export interface AlumnoPerfil {
  idAlumno: number;
  nombre: string;
  numeroControl: string;
  correo: string;
}

export interface SolicitudAlumno {
  nombre: string;
  numeroControl: string;
  correo?: string | null;
}