import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';

import { AuthService } from '../../core/services/auth.service';
import { ApiError } from '../../core/models/api-error';
import { MiPerfilResponse } from '../../core/models/cuentas.model';

@Component({
  selector: 'app-cuenta',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './cuenta.page.html',
})
export class CuentaPage implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  protected readonly cargando = signal(true);
  protected readonly enviandoCambio = signal(false);
  protected readonly mensajeExitoCambio = signal('');
  protected readonly errorCambio = signal('');

  protected readonly mostrarContrasenaActual = signal(false);
  protected readonly mostrarNuevaContrasena = signal(false);
  protected readonly mostrarConfirmarContrasena = signal(false);

  protected readonly perfil = signal<MiPerfilResponse | null>(null);

  protected readonly formularioCambio = this.fb.nonNullable.group({
    contrasenaActual: ['', [Validators.required]],
    nuevaContrasena: ['', [Validators.required, Validators.minLength(8)]],
    confirmarContrasena: ['', [Validators.required]],
  });

  protected readonly contrasenasCoinciden = computed(() => {
    const val = this.formularioCambio.value;
    if (!val.nuevaContrasena || !val.confirmarContrasena) {
      return true;
    }
    return val.nuevaContrasena === val.confirmarContrasena;
  });

  ngOnInit(): void {
    this.cargarDatos();
  }

  protected cargarDatos(): void {
    this.cargando.set(true);
    this.auth.obtenerMiPerfil().subscribe({
      next: (datos) => {
        this.perfil.set(datos);
        this.auth.actualizarDatosPerfil(datos);
        this.cargando.set(false);
      },
      error: () => {
        this.cargando.set(false);
      },
    });
  }

  protected cambiarContrasena(): void {
    if (this.formularioCambio.invalid || !this.contrasenasCoinciden()) {
      return;
    }

    const { contrasenaActual, nuevaContrasena } = this.formularioCambio.getRawValue();

    this.enviandoCambio.set(true);
    this.mensajeExitoCambio.set('');
    this.errorCambio.set('');

    this.auth.cambiarContrasena({ contrasenaActual, nuevaContrasena }).subscribe({
      next: () => {
        this.enviandoCambio.set(false);
        this.mensajeExitoCambio.set('Tu contraseña se ha actualizado correctamente.');
        this.formularioCambio.reset();
      },
      error: (err: ApiError) => {
        this.enviandoCambio.set(false);
        this.errorCambio.set(err.message || 'No fue posible actualizar tu contraseña. Revisa que tu contraseña actual sea correcta.');
      },
    });
  }
}
