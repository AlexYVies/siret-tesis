import { Injectable } from '@angular/core';

const CLAVE_TOKEN = 'tesis_token';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  constructor() {
    // Limpieza de cualquier sesión antigua guardada en localStorage
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(CLAVE_TOKEN);
    }
  }

  guardarToken(token: string): void {
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.setItem(CLAVE_TOKEN, token);
    }
    // Asegurar que no quede en localStorage
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(CLAVE_TOKEN);
    }
  }

  obtenerToken(): string | null {
    if (typeof sessionStorage !== 'undefined') {
      return sessionStorage.getItem(CLAVE_TOKEN);
    }
    return null;
  }

  eliminarToken(): void {
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.removeItem(CLAVE_TOKEN);
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(CLAVE_TOKEN);
    }
  }
}