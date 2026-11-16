import asyncio
import json
import logging
from typing import Optional, Dict, Any
from pathlib import Path
import httpx

from src.core.config import settings
from src.db.database import get_db_connection
from src.models.schemas import (
    EstadoAuditoria, SemaforoDictamen, ResumenEjecutivo, AuditoriaStatusResponse, EstadoCita
)
from src.services.extractor import DocumentExtractor
from src.services.citation_parser import CitationParser
from src.services.citation_verifier import CitationVerifier
from src.services.llm_auditor import ThesisAuditor
from src.services.apa7_document_auditor import APA7DocumentAuditor

logger = logging.getLogger("TesisIA.Queue")

class QueueManager:
    """Gestiona la cola de auditorías de forma asíncrona garantizando 1 trabajo LLM concurrente."""

    _queue: asyncio.Queue = asyncio.Queue()
    _worker_task: Optional[asyncio.Task] = None
    _llm_semaphore: asyncio.Semaphore = asyncio.Semaphore(settings.MAX_CONCURRENT_LLM_JOBS)

    @classmethod
    async def start_worker(cls):
        """Inicia el worker en segundo plano al arrancar FastAPI."""
        if cls._worker_task is None or cls._worker_task.done():
            cls._worker_task = asyncio.create_task(cls._process_queue())
            logger.info("Worker de cola de tesis iniciado exitosamente.")

    @classmethod
    async def enqueue_audit(cls, ticket_id: str):
        """Agrega un ticket a la cola de procesamiento."""
        await cls._queue.put(ticket_id)
        logger.info(f"Ticket {ticket_id} agregado a la cola.")

    @classmethod
    async def get_queue_position(cls, ticket_id: str) -> int:
        """Devuelve la posición aproximada en la cola."""
        return cls._queue.qsize()

    @classmethod
    async def _process_queue(cls):
        while True:
            ticket_id = await cls._queue.get()
            try:
                await cls._execute_audit(ticket_id)
            except Exception as e:
                logger.error(f"Error procesando ticket {ticket_id}: {str(e)}", exc_info=True)
                await cls._update_status(ticket_id, EstadoAuditoria.ERROR, 0, f"Error interno: {str(e)}", error=str(e))
            finally:
                cls._queue.task_done()

    @classmethod
    async def _update_status(
        cls,
        ticket_id: str,
        estado: EstadoAuditoria,
        progreso: int,
        mensaje: str,
        reporte_json: Optional[Dict[str, Any]] = None,
        error: Optional[str] = None
    ):
        async with get_db_connection() as db:
            reporte_str = json.dumps(reporte_json) if reporte_json else None
            await db.execute("""
                UPDATE auditorias
                SET estado = ?, progreso_porcentaje = ?, mensaje = ?, reporte_json = COALESCE(?, reporte_json), error = ?, actualizado_en = CURRENT_TIMESTAMP
                WHERE ticket_id = ?
            """, (estado.value, progreso, mensaje, reporte_str, error, ticket_id))
            await db.commit()

    @classmethod
    async def _execute_audit(cls, ticket_id: str):
        # 1. Obtener datos de la auditoría de SQLite
        async with get_db_connection() as db:
            async with db.execute(
                "SELECT archivo_path, tesis_id, webhook_url FROM auditorias WHERE ticket_id = ?",
                (ticket_id,)
            ) as cursor:
                row = await cursor.fetchone()
                if not row:
                    return
                archivo_path, tesis_id, webhook_url = row

        # Paso 1: Extracción del documento y auditoría de maquetación APA 7
        await cls._update_status(ticket_id, EstadoAuditoria.EXTRAYENDO, 15, "Extrayendo texto y analizando maquetación APA 7ma edición...")
        doc_data = DocumentExtractor.extract(archivo_path)
        cuerpo = doc_data["cuerpo_texto"]
        bib_text = doc_data["bibliografia_texto"]
        formato_apa7 = APA7DocumentAuditor.audit_document(archivo_path, cuerpo_texto=cuerpo)

        # Paso 2: Verificación de Citas (I/O Red en paralelo) y Cumplimiento APA 7
        await cls._update_status(ticket_id, EstadoAuditoria.VALIDANDO_CITAS, 40, "Verificando citas y formato APA 7ma edición...")
        raw_citations = CitationParser.parse_references_block(bib_text)
        citas_auditadas = await CitationVerifier.verify_citations(raw_citations)
        for c in citas_auditadas:
            cumple_apa, obs_apa = CitationParser.audit_apa7_reference(c.cita_original)
            c.cumple_apa7 = cumple_apa
            c.observacion_apa7 = obs_apa

        # Paso 3: Auditoría de Texto con LLM Local (Protegido por semáforo para GPU)
        await cls._update_status(ticket_id, EstadoAuditoria.AUDITANDO_TEXTO, 70, "Analizando coherencia, ortografía y normas APA 7 con modelo local...")
        async with cls._llm_semaphore:
            llm_result = await ThesisAuditor.audit_text_chunks(cuerpo, bib_text)
            
            # Paso 3.1: Verificar fidelidad de citas (¿dice el paper lo que el alumno afirma?)
            for cita in citas_auditadas:
                if cita.estado_verificacion == EstadoCita.VERIFICADA and cita.abstract_encontrado:
                    # Buscar autor principal, o autor detectado, o año
                    ref_clave = cita.autor_detectado or (str(cita.anio_encontrado) if cita.anio_encontrado else (cita.titulo_encontrado[:20] if cita.titulo_encontrado else ""))
                    claim_eval = await ThesisAuditor.verify_citation_claim(cuerpo, ref_clave, cita.abstract_encontrado)
                    cita.validacion_contenido = claim_eval

        # Paso 4: Consolidación y Dictamen Final
        await cls._update_status(ticket_id, EstadoAuditoria.COMPLETADO, 90, "Generando dictamen final con evaluación APA 7...")
        
        total_citas = len(citas_auditadas)
        citas_ok = sum(1 for c in citas_auditadas if c.estado_verificacion in [EstadoCita.VERIFICADA, EstadoCita.VERIFICADA_SIN_DOI])
        citas_distorsionadas = sum(
            1 for c in citas_auditadas 
            if c.validacion_contenido and not c.validacion_contenido.get("coincide", True)
        )
        citas_sospechosas = (total_citas - citas_ok) + citas_distorsionadas
        p_apa7 = float(llm_result.get("puntaje_apa7", 8.0))

        p_formato_apa7 = float(formato_apa7.get("puntaje_formato", 8.0))

        # Determinar Semáforo
        if citas_distorsionadas > 0 or (total_citas > 0 and (citas_ok / total_citas) < 0.7):
            semaforo = SemaforoDictamen.ROJO
            if citas_distorsionadas > 0:
                dictamen = "REQUIERE_REVISION_MAYOR_CITAS_DISTORSIONADAS"
            else:
                dictamen = "REQUIERE_REVISION_MAYOR_CITAS_SOSPECHOSAS"
        elif (
            llm_result["puntaje_ortografia"] < 7.0 
            or llm_result["puntaje_coherencia"] < 7.0 
            or p_apa7 < 7.0 
            or p_formato_apa7 < 6.5
            or citas_sospechosas > 3
        ):
            semaforo = SemaforoDictamen.AMARILLO
            dictamen = "REQUIERE_CORRECCIONES"
        else:
            semaforo = SemaforoDictamen.VERDE
            dictamen = "APROBABLE"

        resumen = ResumenEjecutivo(
            dictamen_sugerido=dictamen,
            semaforo=semaforo,
            total_citas=total_citas,
            citas_verificadas=citas_ok,
            citas_sospechosas=citas_sospechosas,
            puntaje_coherencia=llm_result["puntaje_coherencia"],
            puntaje_ortografia=llm_result["puntaje_ortografia"],
            puntaje_apa7=p_apa7,
            puntaje_formato_apa7=p_formato_apa7
        )

        consejos_alumno = list(llm_result.get("consejos_alumno", []))
        for item in formato_apa7.get("items", []):
            if not item.get("cumple", True) and item.get("sugerencia"):
                consejos_alumno.append(f"[Formato {item['criterio']}] {item['sugerencia']}")

        reporte_final = {
            "ticket_id": ticket_id,
            "tesis_id": tesis_id,
            "estado": EstadoAuditoria.COMPLETADO.value,
            "progreso_porcentaje": 100,
            "mensaje": "Auditoría completada exitosamente.",
            "resumen_ejecutivo": resumen.model_dump(),
            "auditoria_citas": [c.model_dump() for c in citas_auditadas],
            "auditoria_redaccion": [o.model_dump() for o in llm_result["observaciones"]],
            "auditoria_formato_apa7": formato_apa7,
            "observaciones_para_alumno": consejos_alumno
        }

        # Guardar en BD
        await cls._update_status(ticket_id, EstadoAuditoria.COMPLETADO, 100, "Auditoría completada exitosamente.", reporte_json=reporte_final)

        # Disparar Webhook a .NET si fue proporcionado
        if webhook_url:
            await cls._send_webhook(webhook_url, reporte_final)

    @classmethod
    async def _send_webhook(cls, webhook_url: str, payload: Dict[str, Any]):
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                await client.post(webhook_url, json=payload)
        except Exception as e:
            logger.warning(f"No se pudo entregar webhook a {webhook_url}: {e}")
