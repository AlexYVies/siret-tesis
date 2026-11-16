import { inject, Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from './auth.service';

const TIEMPO_INACTIVIDAD_MS = 10 * 60 * 1000; // 10 minutos exactos

@Injectable({ providedIn: 'root' })
export class InactividadService {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  private temporizador: ReturnType<typeof setTimeout> | null = null;
  private readonly eventos = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'click'];
  private readonly manejadorActividad = () => this.registrarActividad();
  private escuchando = false;

  iniciar(): void {
    if (this.escuchando || typeof window === 'undefined') {
      return;
    }

    this.escuchando = true;
    this.ngZone.runOutsideAngular(() => {
      for (const ev of this.eventos) {
        window.addEventListener(ev, this.manejadorActividad, { passive: true });
      }
    });

    this.reiniciarTemporizador();
  }

  detener(): void {
    if (!this.escuchando || typeof window === 'undefined') {
      return;
    }

    this.escuchando = false;
    for (const ev of this.eventos) {
      window.removeEventListener(ev, this.manejadorActividad);
    }

    if (this.temporizador) {
      clearTimeout(this.temporizador);
      this.temporizador = null;
    }
  }

  private registrarActividad(): void {
    if (!this.auth.estaAutenticado()) {
      return;
    }

    this.reiniciarTemporizador();
  }

  private reiniciarTemporizador(): void {
    if (this.temporizador) {
      clearTimeout(this.temporizador);
      this.temporizador = null;
    }

    if (!this.auth.estaAutenticado()) {
      return;
    }

    this.ngZone.runOutsideAngular(() => {
      this.temporizador = setTimeout(() => {
        this.ngZone.run(() => {
          this.cerrarPorInactividad();
        });
      }, TIEMPO_INACTIVIDAD_MS);
    });
  }

  private cerrarPorInactividad(): void {
    if (!this.auth.estaAutenticado()) {
      return;
    }

    this.auth.cerrarSesion();
    this.detener();
    void this.router.navigate(['/login'], {
      queryParams: { inactividad: 'true' },
    });
  }
}
