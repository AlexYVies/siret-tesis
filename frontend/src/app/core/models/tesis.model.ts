export type NivelPosgrado = 'Maestría' | 'Doctorado';
export type TesisEstado =
  | 'Registrada'
  | 'Confirmada'
  | 'En revisión IA'
  | 'En revisión del asesor'
  | 'Cambios solicitados'
  | 'En dictamen'
  | 'Aceptada'
  | 'Rechazada'
  | 'Inactiva';
export type TipoConfirmacion = 'Asesor' | 'Co-asesor';
export type EstadoConfirmacion = 'Pendiente' | 'Confirmada' | 'Rechazada';
export type ResultadoRevisionIA = 'Válido' | 'No válido' | 'Con observaciones';
export type DecisionRevisionAsesor = 'Aceptada' | 'Rechazada para cambios';
export type ResultadoDictamen = 'Aceptada' | 'Rechazada';
export type AccionConfirmacion = EstadoConfirmacion;

export interface ConfirmacionDto {
  idConfirmacion: number;
  tipo: TipoConfirmacion;
  estado: EstadoConfirmacion;
  fecha: string;
}

export interface ArticuloDto {
  idArticulo: number;
  nombre: string;
  url: string;
  doi?: string | null;
  idTipoArticulo?: number | null;
  tipoArticulo?: string | null;
  fechaReg: string;
}

export interface DocumentoDto {
  idDocumento: number;
  version: number;
  nombre: string;
  fechaReg: string;
}

export interface RevisionIADto {
  idRevision: number;
  version: number;
  resultado: ResultadoRevisionIA;
  porcentaje: number;
  fecha: string;
  reporteJson?: string | null;
}

export interface RevisionAsesorDto {
  idRevisionAsesor: number;
  version: number;
  esUltimaVersion: boolean;
  decision: DecisionRevisionAsesor;
  observaciones?: string | null;
  fecha: string;
}

export interface DictamenDto {
  idDictamen: number;
  resultado: ResultadoDictamen;
  observaciones?: string | null;
  fecha: string;
}

export interface TesisListado {
  folio: string;
  titulo: string;
  nivel: NivelPosgrado;
  programa: string;
  version: number;
  estado: TesisEstado;
  fechaReg: string;
  alumno: string;
  asesor: string;
  coAsesor?: string | null;
}

export interface TesisDetalle extends TesisListado {
  fechaExamenGrado?: string | null;
  tieneCoAsesor: boolean;
  idAlumno: number;
  idAsesor: number;
  idCoAsesor?: number | null;
  confirmaciones: ConfirmacionDto[];
  documentos: DocumentoDto[];
  articulos: ArticuloDto[];
  revisionesIA: RevisionIADto[];
  revisionesAsesor: RevisionAsesorDto[];
  dictamenes: DictamenDto[];
}

export interface RegistrarTesisRequest {
  titulo: string;
  nivel: NivelPosgrado;
  programa: string;
  idAsesor: number;
  idCoAsesor?: number | null;
}

export interface TesisRegistrada {
  folio: string;
  estado: TesisEstado;
}

export interface AgregarArticuloRequest {
  nombre: string;
  url: string;
  doi?: string | null;
  idTipoArticulo?: number | null;
}

export interface ReferenciaDetectada {
  nombre: string;
  url: string;
  doi?: string | null;
  idTipoArticulo?: number | null;
  tipoArticulo?: string | null;
  citaOriginal: string;
  yaRegistrado: boolean;
  esSospechosa?: boolean;
  motivoSospecha?: string | null;
  estadoVerificacion?: string | null;
  coincideContenido?: boolean | null;
  cumpleApa7?: boolean | null;
  observacionApa7?: string | null;
}

export interface ImportarReferenciasRequest {
  referencias: Array<{
    nombre: string;
    url: string;
    doi?: string | null;
    idTipoArticulo?: number | null;
  }>;
}

export interface ProcesarConfirmacionRequest {
  tipo: TipoConfirmacion;
  decision: AccionConfirmacion;
}

export interface ProcesarRevisionAsesorRequest {
  esUltimaVersion: boolean;
  decision: DecisionRevisionAsesor;
  observaciones?: string | null;
}

export interface EmitirDictamenRequest {
  resultado: ResultadoDictamen;
  observaciones?: string | null;
}