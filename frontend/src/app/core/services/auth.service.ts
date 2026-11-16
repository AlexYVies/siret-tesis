import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CambiarContrasenaRequest,
  CompletarRegistroDocenteRequest,
  InvitarDocenteRequest,
  LoginRequest,
  LoginResponse,
  MiPerfilResponse,
  RecuperarContrasenaRequest,
  RecuperarContrasenaResponse,
  ReenviarConfirmacionRequest,
  RegistroCuentaRequest,
  RegistroCuentaResponse,
  RestablecerContrasenaRequest,
  ValidarCuentaRequest,
} from '../models/cuentas.model';
import { ApiService } from './api.service';
import { TokenStorageService } from './token-storage.service';

const CLAVE_NOMBRE_CORTO = 'tesis_nombre_corto';
const CLAVE_INICIAL_APELLIDO = 'tesis_inicial_apellido';
const CLAVE_NOMBRE_COMPLETO = 'tesis_nombre_completo';

@Injectable({ providedIn: 'root' })
export class AuthService extends ApiService {
  private readonly tokens = inject(TokenStorageService);
  private readonly CLAIM_ROL = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  private readonly rolActual = signal<string[]>(this.rolesDesdeToken());
  private readonly correoActual = signal<string>(this.correoDesdeToken());
  private readonly idUsuarioActual = signal<number | null>(this.idUsuarioDesdeToken());
  private readonly nombreCortoActual = signal<string>(this.nombreCortoInicial());
  private readonly inicialApellidoActual = signal<string>(this.inicialApellidoInicial());
  private readonly nombreCompletoActual = signal<string>(this.nombreCompletoInicial());

  readonly roles = this.rolActual.asReadonly();
  readonly correo = this.correoActual.asReadonly();
  readonly idUsuario = this.idUsuarioActual.asReadonly();
  readonly nombreCorto = this.nombreCortoActual.asReadonly();
  readonly inicialApellido = this.inicialApellidoActual.asReadonly();
  readonly nombreCompleto = this.nombreCompletoActual.asReadonly();

  readonly estaAutenticado = computed(() => this.rolActual().length > 0);
  readonly esInvitadoSinSesion = computed(
    () => !this.estaAutenticado() || this.correoActual().trim().toLowerCase() === 'invitado@upt.edu.mx',
  );
  readonly puedeGuardarFavoritos = computed(
    () => this.estaAutenticado() && !this.esInvitadoSinSesion(),
  );

  login(credenciales: LoginRequest): Observable<LoginResponse> {
    return this.post<LoginResponse>('/usuarios/login', credenciales);
  }

  loginInvitado(correo?: string): Observable<LoginResponse> {
    return this.post<LoginResponse>('/usuarios/login-invitado', correo ? { correo } : {});
  }

  registrar(solicitud: RegistroCuentaRequest): Observable<RegistroCuentaResponse> {
    return this.post<RegistroCuentaResponse>('/usuarios/registro', solicitud);
  }

  invitarDocente(solicitud: InvitarDocenteRequest): Observable<{ idUsuario: number; correo: string; nombre: string; mensaje: string }> {
    return this.post<{ idUsuario: number; correo: string; nombre: string; mensaje: string }>('/usuarios/invitar-docente', solicitud);
  }

  completarRegistroDocente(solicitud: CompletarRegistroDocenteRequest): Observable<RegistroCuentaResponse> {
    return this.post<RegistroCuentaResponse>('/usuarios/completar-registro-docente', solicitud);
  }

  validarCuenta(solicitud: ValidarCuentaRequest): Observable<void> {
    return this.post<void>('/usuarios/validar-cuenta', solicitud);
  }

  reenviarConfirmacion(solicitud: ReenviarConfirmacionRequest): Observable<void> {
    return this.post<void>('/usuarios/reenviar-confirmacion', solicitud);
  }

  recuperarContrasena(solicitud: RecuperarContrasenaRequest): Observable<RecuperarContrasenaResponse> {
    return this.post<RecuperarContrasenaResponse>('/usuarios/recuperar-contrasena', solicitud);
  }

  restablecerContrasena(solicitud: RestablecerContrasenaRequest): Observable<void> {
    return this.post<void>('/usuarios/restablecer-contrasena', solicitud);
  }

  cambiarContrasena(solicitud: CambiarContrasenaRequest): Observable<void> {
    return this.post<void>('/usuarios/cambiar-contrasena', solicitud);
  }

  solicitarRestablecimientoCuenta(): Observable<void> {
    return this.post<void>('/usuarios/solicitar-restablecimiento-correo', {});
  }

  obtenerMiPerfil(): Observable<MiPerfilResponse> {
    return this.get<MiPerfilResponse>('/usuarios/mi-perfil');
  }

  actualizarDatosPerfil(perfil: { nombre?: string; nombreCorto?: string; inicialApellido?: string }): void {
    if (perfil.nombre) {
      this.nombreCompletoActual.set(perfil.nombre);
      if (typeof sessionStorage !== 'undefined') sessionStorage.setItem(CLAVE_NOMBRE_COMPLETO, perfil.nombre);
    }
    if (perfil.nombreCorto) {
      this.nombreCortoActual.set(perfil.nombreCorto);
      if (typeof sessionStorage !== 'undefined') sessionStorage.setItem(CLAVE_NOMBRE_CORTO, perfil.nombreCorto);
    }
    if (perfil.inicialApellido) {
      this.inicialApellidoActual.set(perfil.inicialApellido);
      if (typeof sessionStorage !== 'undefined') sessionStorage.setItem(CLAVE_INICIAL_APELLIDO, perfil.inicialApellido);
    }
  }

  guardarSesion(respuesta: LoginResponse): void {
    this.tokens.guardarToken(respuesta.token);
    this.rolActual.set(this.rolesDesdeToken());
    this.correoActual.set(respuesta.correo);
    this.idUsuarioActual.set(this.idUsuarioDesdeToken());

    const nombre = respuesta.nombre || this.nombreDesdeToken() || '';
    const nombreCorto = respuesta.nombreCorto || this.calcularNombreCorto(nombre, respuesta.correo);
    const inicial = respuesta.inicialApellido || this.calcularInicialApellido(nombre, respuesta.correo);

    this.nombreCompletoActual.set(nombre);
    this.nombreCortoActual.set(nombreCorto);
    this.inicialApellidoActual.set(inicial);

    if (typeof sessionStorage !== 'undefined') {
      if (nombre) sessionStorage.setItem(CLAVE_NOMBRE_COMPLETO, nombre);
      if (nombreCorto) sessionStorage.setItem(CLAVE_NOMBRE_CORTO, nombreCorto);
      if (inicial) sessionStorage.setItem(CLAVE_INICIAL_APELLIDO, inicial);
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(CLAVE_NOMBRE_COMPLETO);
      localStorage.removeItem(CLAVE_NOMBRE_CORTO);
      localStorage.removeItem(CLAVE_INICIAL_APELLIDO);
    }
  }

  cerrarSesion(): void {
    this.tokens.eliminarToken();
    this.rolActual.set([]);
    this.correoActual.set('');
    this.idUsuarioActual.set(null);
    this.nombreCompletoActual.set('');
    this.nombreCortoActual.set('');
    this.inicialApellidoActual.set('U');
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.removeItem(CLAVE_NOMBRE_COMPLETO);
      sessionStorage.removeItem(CLAVE_NOMBRE_CORTO);
      sessionStorage.removeItem(CLAVE_INICIAL_APELLIDO);
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(CLAVE_NOMBRE_COMPLETO);
      localStorage.removeItem(CLAVE_NOMBRE_CORTO);
      localStorage.removeItem(CLAVE_INICIAL_APELLIDO);
    }
  }

  tieneRol(rol: string): boolean {
    return this.rolActual().includes(rol);
  }

  private nombreCompletoInicial(): string {
    const enLocal = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(CLAVE_NOMBRE_COMPLETO) : null;
    if (enLocal) return enLocal;
    return this.nombreDesdeToken();
  }

  private nombreCortoInicial(): string {
    const enLocal = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(CLAVE_NOMBRE_CORTO) : null;
    if (enLocal) return enLocal;
    const nombre = this.nombreDesdeToken();
    const correo = this.correoDesdeToken();
    return this.calcularNombreCorto(nombre, correo);
  }

  private inicialApellidoInicial(): string {
    const enLocal = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(CLAVE_INICIAL_APELLIDO) : null;
    if (enLocal) return enLocal;
    const inicialToken = this.inicialApellidoDesdeToken();
    if (inicialToken) return inicialToken;
    const nombre = this.nombreDesdeToken();
    const correo = this.correoDesdeToken();
    return this.calcularInicialApellido(nombre, correo);
  }

  private calcularNombreCorto(nombreCompleto: string, correo: string): string {
    const correoMin = (correo || '').trim().toLowerCase();
    if (correoMin.includes('2230100')) {
      return 'Alexis Villar';
    }

    const nombre = (nombreCompleto || '').trim();
    if (nombre) {
      const tokens = nombre.split(/\s+/).filter(Boolean);
      if (tokens.length >= 4) {
        return `${tokens[0]} ${tokens[2]}`;
      }
      if (tokens.length === 3) {
        return `${tokens[0]} ${tokens[1]}`;
      }
      if (tokens.length === 2) {
        return `${tokens[0]} ${tokens[1]}`;
      }
      if (tokens.length === 1) {
        return tokens[0];
      }
    }

    if (correo && correo.includes('@')) {
      const prefijo = correo.split('@')[0];
      if (prefijo.includes('.')) {
        const partes = prefijo.split('.').filter(Boolean);
        if (partes.length >= 2) {
          const pNombre = partes[0].charAt(0).toUpperCase() + partes[0].slice(1).toLowerCase();
          const pApellido = partes[1].charAt(0).toUpperCase() + partes[1].slice(1).toLowerCase();
          return `${pNombre} ${pApellido}`;
        }
      }
      if (/^\d+$/.test(prefijo)) {
        return 'Alumno';
      }
      return prefijo;
    }

    return correo || 'Usuario';
  }

  private calcularInicialApellido(nombreCompleto: string, correo: string): string {
    const correoMin = (correo || '').trim().toLowerCase();
    if (correoMin.includes('2230100')) {
      return 'V';
    }

    const nombre = (nombreCompleto || '').trim();
    if (nombre) {
      const tokens = nombre.split(/\s+/).filter(Boolean);
      if (tokens.length >= 4) {
        return tokens[2].charAt(0).toUpperCase();
      }
      if (tokens.length === 3 || tokens.length === 2) {
        return tokens[1].charAt(0).toUpperCase();
      }
      if (tokens.length === 1) {
        return tokens[0].charAt(0).toUpperCase();
      }
    }

    if (correo && correo.includes('@')) {
      const prefijo = correo.split('@')[0];
      if (prefijo.includes('.')) {
        const partes = prefijo.split('.').filter(Boolean);
        if (partes.length >= 2 && partes[1].length > 0) {
          return partes[1].charAt(0).toUpperCase();
        }
      }
      if (!/^\d+$/.test(prefijo) && prefijo.length > 0) {
        return prefijo.charAt(0).toUpperCase();
      }
    }

    return 'U';
  }

  private nombreDesdeToken(): string {
    const token = this.tokens.obtenerToken();
    if (!token) {
      return '';
    }

    try {
      const cargaTexto = token.split('.')[1];
      const carga = JSON.parse(
        atob(cargaTexto.replace(/-/g, '+').replace(/_/g, '/')),
      );
      return (
        carga?.['nombre'] ??
        carga?.['given_name'] ??
        carga?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ??
        ''
      );
    } catch {
      return '';
    }
  }

  private inicialApellidoDesdeToken(): string {
    const token = this.tokens.obtenerToken();
    if (!token) {
      return '';
    }

    try {
      const cargaTexto = token.split('.')[1];
      const carga = JSON.parse(
        atob(cargaTexto.replace(/-/g, '+').replace(/_/g, '/')),
      );
      const inicial = (carga?.['inicial_apellido'] ?? '').toString().trim().toUpperCase();
      return inicial.length > 0 ? inicial.charAt(0) : '';
    } catch {
      return '';
    }
  }

  private rolesDesdeToken(): string[] {
    const token = this.tokens.obtenerToken();
    if (!token) {
      return [];
    }

    try {
      const cargaTexto = token.split('.')[1];
      const carga = JSON.parse(
        atob(cargaTexto.replace(/-/g, '+').replace(/_/g, '/')),
      );
      const rolClaim = carga?.[this.CLAIM_ROL] ?? carga?.['role'];

      if (Array.isArray(rolClaim)) {
        return Array.from(new Set<string>(rolClaim));
      }

      return rolClaim ? [rolClaim] : [];
    } catch {
      return [];
    }
  }

  private correoDesdeToken(): string {
    const token = this.tokens.obtenerToken();
    if (!token) {
      return '';
    }

    try {
      const cargaTexto = token.split('.')[1];
      const carga = JSON.parse(
        atob(cargaTexto.replace(/-/g, '+').replace(/_/g, '/')),
      );
      return carga?.['email'] ?? '';
    } catch {
      return '';
    }
  }

  private idUsuarioDesdeToken(): number | null {
    const token = this.tokens.obtenerToken();
    if (!token) {
      return null;
    }

    try {
      const cargaTexto = token.split('.')[1];
      const carga = JSON.parse(
        atob(cargaTexto.replace(/-/g, '+').replace(/_/g, '/')),
      );
      const valor = carga?.['nameid'] ?? carga?.['sub'];
      const numero = Number(valor);
      return Number.isInteger(numero) && numero > 0 ? numero : null;
    } catch {
      return null;
    }
  }
}