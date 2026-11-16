import { inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const autenticado = (): boolean => {
  const auth = inject(AuthService);
  if (!auth.estaAutenticado()) {
    void inject(Router).navigate(['/login']);
    return false;
  }

  return true;
};

export const estaAutenticadoMatch = (): boolean => {
  return inject(AuthService).estaAutenticado();
};

export const iniciadoSesion = (): boolean => {
  const auth = inject(AuthService);
  if (auth.estaAutenticado()) {
    auth.cerrarSesion();
  }

  return true;
};

export const conRol = (...roles: string[]) => (): boolean => {
  const auth = inject(AuthService);
  if (!auth.estaAutenticado()) {
    void inject(Router).navigate(['/login']);
    return false;
  }

  return roles.some((rol) => auth.tieneRol(rol));
};