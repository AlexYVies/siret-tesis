import uuid
import shutil
import json
from pathlib import Path
from datetime import datetime
from fastapi import APIRouter, UploadFile, File, Form, HTTPException, status
from src.core.config import settings
from src.db.database import get_db_connection
from src.models.schemas import AuditoriaTicketResponse, AuditoriaStatusResponse, EstadoAuditoria
from src.services.queue_manager import QueueManager

router = APIRouter(prefix="/auditorias", tags=["Auditorías de Tesis"])

@router.post("", response_model=AuditoriaTicketResponse, status_code=status.HTTP_202_ACCEPTED)
async def encolar_auditoria(
    archivo: UploadFile = File(...),
    tesis_id: str = Form(...),
    titulo: str = Form("Tesis sin título"),
    alumno_id: str = Form(""),
    webhook_url: str = Form("")
):
    """Encola un archivo de tesis para ser procesado en segundo plano."""
    ext = Path(archivo.filename).suffix.lower()
    if ext not in [".pdf", ".docx", ".doc"]:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Formato inválido. Solo se admiten archivos .pdf o .docx."
        )

    ticket_id = f"aud_{uuid.uuid4().hex[:12]}"
    saved_filename = f"{ticket_id}{ext}"
    saved_path = settings.UPLOAD_DIR / saved_filename

    # Guardar archivo de forma segura en disco
    with open(saved_path, "wb") as buffer:
        shutil.copyfileobj(archivo.file, buffer)

    # Registrar en base de datos SQLite
    now = datetime.utcnow()
    async with get_db_connection() as db:
        await db.execute("""
            INSERT INTO auditorias (ticket_id, tesis_id, titulo, alumno_id, archivo_path, estado, progreso_porcentaje, mensaje, webhook_url, creado_en)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (ticket_id, tesis_id, titulo, alumno_id, str(saved_path), EstadoAuditoria.EN_COLA.value, 0, "En cola para procesamiento.", webhook_url, now.isoformat()))
        await db.commit()

    # Encolar en la cola asíncrona
    await QueueManager.enqueue_audit(ticket_id)
    posicion = await QueueManager.get_queue_position(ticket_id)

    return AuditoriaTicketResponse(
        ticket_id=ticket_id,
        tesis_id=tesis_id,
        estado=EstadoAuditoria.EN_COLA,
        posicion_en_cola=posicion,
        mensaje="Tesis recibida y encolada para auditoría en segundo plano.",
        creado_en=now
    )

@router.get("/{ticket_id}", response_model=AuditoriaStatusResponse)
async def obtener_estado_auditoria(ticket_id: str):
    """Obtiene el estado o el reporte consolidado de la auditoría."""
    async with get_db_connection() as db:
        async with db.execute("""
            SELECT ticket_id, tesis_id, estado, progreso_porcentaje, mensaje, reporte_json, error
            FROM auditorias
            WHERE ticket_id = ?
        """, (ticket_id,)) as cursor:
            row = await cursor.fetchone()
            if not row:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Ticket de auditoría no encontrado.")

            t_id, tesis_id, estado, progreso, mensaje, reporte_json, error = row

            if reporte_json:
                data = json.loads(reporte_json)
                return AuditoriaStatusResponse(**data)

            return AuditoriaStatusResponse(
                ticket_id=t_id,
                tesis_id=tesis_id,
                estado=EstadoAuditoria(estado),
                progreso_porcentaje=progreso,
                mensaje=mensaje,
                error=error
            )
