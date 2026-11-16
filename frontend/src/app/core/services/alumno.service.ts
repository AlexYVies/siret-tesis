import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';
import { AlumnoPerfil, SolicitudAlumno } from '../models/alumno.model';

@Injectable({ providedIn: 'root' })
export class AlumnoService extends ApiService {
  miPerfil(): Observable<AlumnoPerfil | null> {
    return this.get<AlumnoPerfil | null>('/alumnos/yo');
  }

  crearPerfil(solicitud: SolicitudAlumno): Observable<AlumnoPerfil> {
    return this.post<AlumnoPerfil>('/alumnos', solicitud);
  }
}