export const ROLES_AUTOREGISTRABLES = ['Alumno', 'Invitado'] as const;

export type RolAutoregistrable = (typeof ROLES_AUTOREGISTRABLES)[number];

export interface RegistroCuentaRequest {
  correo: string;
  contrasena?: string;
  rol: RolAutoregistrable;
  nombre?: string;
  nombres?: string;
  primerApellido?: string;
  segundoApellido?: string;
  matricula?: string;
}

export interface RegistroCuentaResponse {
  idUsuario: number;
  tokenValidacion?: string | null;
  token?: string | null;
  expiraEn?: string | null;
  correo?: string | null;
  roles?: string[] | null;
}

export interface InvitarDocenteRequest {
  correo: string;
  nombre: string;
  idProfesor?: number;
}

export interface CompletarRegistroDocenteRequest {
  idUsuario: number;
  correo: string;
  nombre: string;
  nombres?: string;
  primerApellido?: string;
  segundoApellido?: string;
  contrasena: string;
  token: string;
}

export interface LoginRequest {
  correo: string;
  contrasena: string;
  rolEsperado?: string;
}

export interface LoginResponse {
  token: string;
  expiraEn: string;
  idUsuario: number;
  correo: string;
  roles: string[];
  nombre?: string;
  nombreCorto?: string;
  inicialApellido?: string;
}

export interface CambiarContrasenaRequest {
  contrasenaActual: string;
  nuevaContrasena: string;
}

export interface MiPerfilResponse {
  idUsuario: number;
  correo: string;
  nombre: string;
  nombreCorto: string;
  inicialApellido: string;
  roles: string[];
}

export interface ValidarCuentaRequest {
  idUsuario: number;
  token: string;
}

export interface ReenviarConfirmacionRequest {
  correo: string;
}

export interface RecuperarContrasenaRequest {
  correo: string;
}

export interface RecuperarContrasenaResponse {
  idUsuario: number;
  tokenRestablecimiento?: string | null;
}

export interface RestablecerContrasenaRequest {
  idUsuario: number;
  token: string;
  nuevaContrasena: string;
}

export function esCorreoInstitucional(correo: string): boolean {
  return /@upt\.edu\.mx$/i.test(correo.trim());
}