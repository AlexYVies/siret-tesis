import { NivelPosgrado } from './tesis.model';

export interface ProfesorPrograma {
  nivel: NivelPosgrado;
  programa: string;
}

export interface Profesor {
  idProfesor: number;
  nombre: string;
  activo: boolean;
  programas: ProfesorPrograma[];
  correo?: string | null;
  cuentaActiva?: boolean;
}

export interface TipoArticulo {
  idTipoArticulo: number;
  nombre: string;
  descripcion?: string | null;
}

export interface TipoArticuloRequest {
  nombre: string;
  descripcion?: string | null;
}

export interface RegistrarProfesorRequest {
  nombre: string;
  posgrados: ProfesorPrograma[];
  correo?: string | null;
}

export interface Posgrado {
  idPosgrado: number;
  nivel: NivelPosgrado;
  nombre: string;
  activo: boolean;
}

export interface SolicitudPosgradoRequest {
  nivel: NivelPosgrado;
  nombre: string;
}

export interface ActualizarPosgradoRequest {
  nivel: NivelPosgrado;
  nombre: string;
  activo?: boolean;
}

export interface ActualizarPosgradosProfesorRequest {
  posgrados: ProfesorPrograma[];
}

export interface ResultadoPosgradoAccion {
  mensaje: string;
  posgrado?: Posgrado | null;
}

export interface DirectorPosgrado {
  idUsuario: number;
  correo: string;
  nombre: string;
  emailConfirmed: boolean;
  fechaRegistro?: string | null;
}

export interface SolicitudDirectorRequest {
  correo: string;
  nombre?: string;
  contrasena?: string;
}

export function programasProfesor(profesor: Profesor, nivel: NivelPosgrado): string[] {
  return profesor.programas
    .filter((p) => p.nivel === nivel)
    .map((p) => p.programa);
}