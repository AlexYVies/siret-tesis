import { Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AlumnoService } from '../../core/services/alumno.service';
import { AuthService } from '../../core/services/auth.service';
import { AlumnoPerfil } from '../../core/models/alumno.model';
import { primera } from '../../core/util/async.util';

@Component({
  selector: 'app-perfil',
  templateUrl: './perfil.page.html',
})
export class PerfilPage {
  private readonly alumnos = inject(AlumnoService);
  private readonly auth = inject(AuthService);

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal('');
  protected readonly perfil = signal<AlumnoPerfil | null>(null);
  protected readonly crear = signal(false);

  protected readonly nombre = signal('');
  protected readonly numeroControl = signal('');

  constructor() {
    void this.cargar();
  }

  protected correo(): string {
    return this.auth.correo();
  }

  protected formularioValido(): boolean {
    return this.nombre().trim().length > 0 && this.numeroControl().trim().length > 0;
  }

  protected async guardarPerfil(): Promise<void> {
    if (!this.formularioValido() || this.guardando()) {
      return;
    }

    this.guardando.set(true);
    this.error.set('');

    try {
      const resultado = await firstValueFrom(
        this.alumnos.crearPerfil({
          nombre: this.nombre().trim(),
          numeroControl: this.numeroControl().trim(),
          correo: this.correo(),
        }),
      );

      if (resultado) {
        this.perfil.set(resultado);
        this.crear.set(false);
      } else {
        this.error.set('No se pudo crear el perfil.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.guardando.set(false);
    }
  }

  private async cargar(): Promise<void> {
    this.cargando.set(true);

    const perfil = await primera(this.alumnos.miPerfil());
    this.perfil.set(perfil);
    this.crear.set(!perfil);
    this.cargando.set(false);
  }

  private mensajeError(err: unknown): string {
    const cuerpo = err as { message?: string } | null;
    return cuerpo?.message ?? 'Error inesperado. Intenta de nuevo.';
  }
}