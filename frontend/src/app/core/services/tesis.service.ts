import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';
import {
  AgregarArticuloRequest,
  EmitirDictamenRequest,
  ProcesarConfirmacionRequest,
  ProcesarRevisionAsesorRequest,
  RegistrarTesisRequest,
  TesisDetalle,
  TesisListado,
  TesisRegistrada,
  RevisionIADto,
  ArticuloDto,
  ReferenciaDetectada,
  DocumentoDto,
  DictamenDto,
  RevisionAsesorDto,
  ConfirmacionDto,
} from '../models/tesis.model';

@Injectable({ providedIn: 'root' })
export class TesisService extends ApiService {
  listar(): Observable<TesisListado[]> {
    return this.get<TesisListado[]>('/tesis');
  }

  obtenerDetalle(folio: string): Observable<TesisDetalle> {
    return this.get<TesisDetalle>(`/tesis/${folio}`);
  }

  registrar(peticion: RegistrarTesisRequest): Observable<TesisRegistrada> {
    return this.post<TesisRegistrada>('/tesis', peticion);
  }

  subirDocumento(folio: string, archivo: File): Observable<DocumentoDto> {
    const formulario = new FormData();
    formulario.append('archivo', archivo, archivo.name);
    return this.postForm<DocumentoDto>(`/tesis/${folio}/documento`, formulario);
  }

  descargarDocumento(folio: string): Observable<Blob> {
    return this.getBlob(`/tesis/${folio}/documento`);
  }

  agregarArticulo(folio: string, peticion: AgregarArticuloRequest): Observable<ArticuloDto> {
    return this.post<ArticuloDto>(`/tesis/${folio}/articulos`, peticion);
  }

  obtenerSugerenciasPdf(folio: string): Observable<ReferenciaDetectada[]> {
    return this.get<ReferenciaDetectada[]>(`/tesis/${folio}/articulos/sugerencias-pdf`);
  }

  importarReferencias(folio: string, referencias: Array<{ nombre: string; url: string; doi?: string | null; idTipoArticulo?: number | null }>): Observable<ArticuloDto[]> {
    return this.post<ArticuloDto[]>(`/tesis/${folio}/articulos/importar-referencias`, { referencias });
  }

  confirmar(folio: string, peticion: ProcesarConfirmacionRequest): Observable<ConfirmacionDto> {
    return this.post<ConfirmacionDto>(`/tesis/${folio}/confirmaciones`, peticion);
  }

  revisionIA(folio: string, esUltimaVersion: boolean): Observable<RevisionIADto> {
    return this.post<RevisionIADto>(`/tesis/${folio}/revision-ia`, { esUltimaVersion });
  }

  revisionAsesor(folio: string, peticion: ProcesarRevisionAsesorRequest): Observable<RevisionAsesorDto> {
    return this.post<RevisionAsesorDto>(`/tesis/${folio}/revision-asesor`, peticion);
  }

  dictamen(folio: string, peticion: EmitirDictamenRequest): Observable<DictamenDto> {
    return this.post<DictamenDto>(`/tesis/${folio}/dictamen`, peticion);
  }
}