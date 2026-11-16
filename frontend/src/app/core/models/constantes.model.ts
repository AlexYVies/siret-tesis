export const Rol = {
  Alumno: 'Alumno',
  Docente: 'Docente',
  Director: 'Director de posgrado',
  Administrador: 'Administrador',
  AdministradorTI: 'Administrador de TI',
  Invitado: 'Invitado',
} as const;

export type RolValue = (typeof Rol)[keyof typeof Rol];

export const ROLES_PERSONAL: RolValue[] = [Rol.Director, Rol.Administrador, Rol.AdministradorTI];
export const ROLES_ADMINISTRACION: RolValue[] = [Rol.Administrador, Rol.AdministradorTI];