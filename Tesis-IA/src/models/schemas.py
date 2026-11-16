from enum import Enum
from typing import List, Optional, Dict, Any
from pydantic import BaseModel, Field
from datetime import datetime

class EstadoAuditoria(str, Enum):
    EN_COLA = "EN_COLA"
    EXTRAYENDO = "EXTRAYENDO"
    VALIDANDO_CITAS = "VALIDANDO_CITAS"
    AUDITANDO_TEXTO = "AUDITANDO_TEXTO"
    COMPLETADO = "COMPLETADO"
    ERROR = "ERROR"

class SemaforoDictamen(str, Enum):
    VERDE = "VERDE"        # Tesis aprobable sin observaciones graves
    AMARILLO = "AMARILLO"  # Requiere correcciones menores
    ROJO = "ROJO"          # Requiere revisión mayor / citas falsas o sospechosas

class EstadoCita(str, Enum):
    VERIFICADA = "VERIFICADA"
    VERIFICADA_SIN_DOI = "VERIFICADA_SIN_DOI"  # Libro o fuente real encontrada en OpenLibrary / Catálogo
    SOSPECHOSA_NO_ENCONTRADA = "SOSPECHOSA_NO_ENCONTRADA"
    ERROR_FORMATO = "ERROR_FORMATO"

class TipoObservacionRedaccion(str, Enum):
    ORTOGRAFIA = "ORTOGRAFIA"
    COHERENCIA = "COHERENCIA"
    FORMATO_ACADEMICO = "FORMATO_ACADEMICO"
    FORMATO_APA7 = "FORMATO_APA7"

class ValidacionContenidoCita(BaseModel):
    coincide: bool
    explicacion: str

class CitaAuditada(BaseModel):
    cita_original: str
    estado_verificacion: EstadoCita
    doi: Optional[str] = None
    titulo_encontrado: Optional[str] = None
    autor_detectado: Optional[str] = None
    fuente_verificacion: Optional[str] = None  # ej. "Crossref", "OpenAlex", "OpenLibrary (Libro Físico)"
    coincidencia_autores: bool = False
    anio_encontrado: Optional[int] = None
    motivo: Optional[str] = None
    abstract_encontrado: Optional[str] = None
    validacion_contenido: Optional[ValidacionContenidoCita] = None
    cumple_apa7: Optional[bool] = None
    observacion_apa7: Optional[str] = None

class ObservacionRedaccion(BaseModel):
    seccion: str
    tipo: TipoObservacionRedaccion
    texto_afectado: str
    observacion: str
    sugerencia: str

class CumplimientoItemAPA7(BaseModel):
    criterio: str
    cumple: bool
    detalle_detectado: str
    norma_requerida: str
    observacion: Optional[str] = None
    sugerencia: Optional[str] = None

class AuditoriaFormatoDocumentoAPA7(BaseModel):
    puntaje_formato: float = Field(..., ge=0, le=10)
    cumple_general: bool
    resumen: str
    items: List[CumplimientoItemAPA7] = []
    detalles_tecnicos: Dict[str, Any] = {}

class ResumenEjecutivo(BaseModel):
    dictamen_sugerido: str
    semaforo: SemaforoDictamen
    total_citas: int = 0
    citas_verificadas: int = 0
    citas_sospechosas: int = 0
    puntaje_coherencia: float = Field(..., ge=0, le=10)
    puntaje_ortografia: float = Field(..., ge=0, le=10)
    puntaje_apa7: Optional[float] = Field(default=8.0, ge=0, le=10)
    puntaje_formato_apa7: Optional[float] = Field(default=None, ge=0, le=10)

class AuditoriaTicketResponse(BaseModel):
    ticket_id: str
    tesis_id: str
    estado: EstadoAuditoria
    posicion_en_cola: int
    mensaje: str
    creado_en: datetime

class AuditoriaStatusResponse(BaseModel):
    ticket_id: str
    tesis_id: str
    estado: EstadoAuditoria
    progreso_porcentaje: int
    mensaje: str
    resumen_ejecutivo: Optional[ResumenEjecutivo] = None
    auditoria_citas: Optional[List[CitaAuditada]] = None
    auditoria_redaccion: Optional[List[ObservacionRedaccion]] = None
    auditoria_formato_apa7: Optional[AuditoriaFormatoDocumentoAPA7] = None
    observaciones_para_alumno: Optional[List[str]] = None
    error: Optional[str] = None
