import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';
import {
  ActualizarPosgradoRequest,
  DirectorPosgrado,
  Posgrado,
  Profesor,
  ProfesorPrograma,
  RegistrarProfesorRequest,
  ResultadoPosgradoAccion,
  SolicitudDirectorRequest,
  SolicitudPosgradoRequest,
  TipoArticulo,
  TipoArticuloRequest,
} from '../models/catalogos.model';

@Injectable({ providedIn: 'root' })
export class CatalogosService extends ApiService {
  profesores(): Observable<Profesor[]> {
    return this.get<Profesor[]>('/profesores');
  }

  profesorActual(): Observable<Profesor> {
    return this.get<Profesor>('/profesores/yo');
  }

  registrarProfesor(solicitud: RegistrarProfesorRequest): Observable<Profesor> {
    return this.post<Profesor>('/profesores', solicitud);
  }

  actualizarPosgradosProfesor(idProfesor: number, posgrados: ProfesorPrograma[]): Observable<Profesor> {
    return this.put<Profesor>(`/profesores/${idProfesor}/posgrados`, { posgrados });
  }

  posgrados(soloActivos = false): Observable<Posgrado[]> {
    return this.get<Posgrado[]>(`/posgrados${soloActivos ? '?soloActivos=true' : ''}`);
  }

  registrarPosgrado(solicitud: SolicitudPosgradoRequest): Observable<Posgrado> {
    return this.post<Posgrado>('/posgrados', solicitud);
  }

  actualizarPosgrado(idPosgrado: number, solicitud: ActualizarPosgradoRequest): Observable<Posgrado> {
    return this.put<Posgrado>(`/posgrados/${idPosgrado}`, solicitud);
  }

  eliminarODarDeBajaPosgrado(idPosgrado: number): Observable<ResultadoPosgradoAccion> {
    return this.delete<ResultadoPosgradoAccion>(`/posgrados/${idPosgrado}`);
  }

  tiposArticulo(): Observable<TipoArticulo[]> {
    return this.get<TipoArticulo[]>('/tipos-articulo');
  }

  registrarTipoArticulo(solicitud: TipoArticuloRequest): Observable<TipoArticulo> {
    return this.post<TipoArticulo>('/tipos-articulo', solicitud);
  }

  directores(): Observable<DirectorPosgrado[]> {
    return this.get<DirectorPosgrado[]>('/usuarios/directores');
  }

  registrarDirector(solicitud: SolicitudDirectorRequest): Observable<DirectorPosgrado> {
    return this.post<DirectorPosgrado>('/usuarios/directores', solicitud);
  }

  removerDirector(idUsuario: number): Observable<{ message: string }> {
    return this.delete<{ message: string }>(`/usuarios/directores/${idUsuario}`);
  }
}
