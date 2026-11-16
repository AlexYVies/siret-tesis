import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { ApiError } from '../../core/models/api-error';

@Component({
  selector: 'app-restablecer-contrasena',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12 sm:px-6">
      <div class="tarjeta animar-entrada w-full max-w-md p-8 sm:p-10">
        <a routerLink="/inicio" class="mb-8 flex items-center gap-3">
          <span class="degradado-marca flex h-11 w-11 items-center justify-center rounded-2xl text-lg font-black text-white shadow-md">
            U
          </span>
          <span class="text-sm font-bold text-slate-900">SIRET · Sistema Integral de Repositorio y Evaluación de Tesis</span>
        </a>

        <h1 class="text-2xl font-bold text-slate-900">Restablecer contraseña</h1>
        <p class="mt-1 text-sm text-slate-500">
          @if (tokenDesdeUrl()) {
            Ingrese su nueva contraseña de acceso.
          } @else {
            Ingrese el identificador, el token de restablecimiento y su nueva contraseña.
          }
        </p>

        @if (tokenDesdeUrl()) {
          <div class="mt-3 flex items-center gap-2 rounded-xl border border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-800">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 shrink-0 text-sky-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>Enlace de recuperación verificado para el usuario <strong>#{{ formulario.value.idUsuario }}</strong>.</span>
          </div>
        }

        @if (error()) {
          <div class="aviso-error mt-4"> {{ error() }} </div>
        }

        @if (exito()) {
          <div class="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-center">
            <span class="inline-flex h-10 w-10 items-center justify-center rounded-full bg-emerald-500 text-white shadow-sm mb-2">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
              </svg>
            </span>
            <p class="text-sm font-semibold text-emerald-800">¡Contraseña restablecida con éxito!</p>
            <p class="mt-1 text-xs text-emerald-600">Redirigiendo a inicio de sesión…</p>
          </div>
        } @else {
          <form [formGroup]="formulario" (ngSubmit)="restablecer()" class="mt-6 space-y-4">
            @if (!tokenDesdeUrl()) {
              <div>
                <label for="idUsuario" class="campo-label">Identificador de usuario</label>
                <input
                  id="idUsuario"
                  type="number"
                  formControlName="idUsuario"
                  required
                  placeholder="Ej. 12"
                  class="campo"
                />
              </div>

              <div>
                <label for="token" class="campo-label">Token de restablecimiento</label>
                <textarea
                  id="token"
                  formControlName="token"
                  required
                  rows="3"
                  placeholder="Pegue aquí el token recibido por correo"
                  class="campo font-mono text-xs"
                ></textarea>
              </div>
            }

            <div>
              <label for="nuevaContrasena" class="campo-label">Nueva contraseña</label>
              <div class="relative">
                <input
                  id="nuevaContrasena"
                  [type]="mostrarContrasena() ? 'text' : 'password'"
                  formControlName="nuevaContrasena"
                  required
                  minlength="8"
                  placeholder="Mínimo 8 caracteres"
                  class="campo pr-12"
                />
                <button
                  type="button"
                  (click)="mostrarContrasena.update((v) => !v)"
                  class="absolute inset-y-0 right-0 flex w-12 items-center justify-center text-slate-400 hover:text-slate-600"
                  [attr.aria-label]="mostrarContrasena() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    @if (mostrarContrasena()) {
                      <path stroke-linecap="round" stroke-linejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                    } @else {
                      <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                      <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    }
                  </svg>
                </button>
              </div>
            </div>

            <button type="submit" [disabled]="formulario.invalid || enviando()" class="boton-primario w-full">
              @if (enviando()) {
                <svg class="h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                </svg>
                Restableciendo…
              } @else {
                Guardar nueva contraseña
              }
            </button>
          </form>
        }

        <p class="mt-6 text-center text-sm">
          <a routerLink="/login" class="enlace">Ir a iniciar sesión</a>
        </p>
      </div>
    </section>
  `,
})
export class RestablecerContrasenaPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ruta = inject(ActivatedRoute);

  protected readonly enviando = signal(false);
  protected readonly error = signal('');
  protected readonly exito = signal(false);
  protected readonly mostrarContrasena = signal(false);
  protected readonly tokenDesdeUrl = signal(false);

  protected readonly formulario = this.fb.nonNullable.group({
    idUsuario: [0, [Validators.required, Validators.min(1)]],
    token: ['', Validators.required],
    nuevaContrasena: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    const idParam = this.ruta.snapshot.queryParamMap.get('id');
    const tokenParam = this.ruta.snapshot.queryParamMap.get('token');

    if (idParam && tokenParam) {
      const idNumero = parseInt(idParam, 10);
      if (!isNaN(idNumero) && idNumero > 0) {
        this.formulario.patchValue({
          idUsuario: idNumero,
          token: tokenParam.trim(),
        });
        this.tokenDesdeUrl.set(true);
      }
    }
  }

  restablecer(): void {
    if (this.formulario.invalid) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');

    this.auth
      .restablecerContrasena({
        idUsuario: this.formulario.value.idUsuario!,
        token: this.formulario.value.token!,
        nuevaContrasena: this.formulario.value.nuevaContrasena!,
      })
      .subscribe({
        next: () => {
          this.enviando.set(false);
          this.exito.set(true);
          setTimeout(() => {
            void this.router.navigate(['/login']);
          }, 1800);
        },
        error: (err: ApiError) => {
          this.enviando.set(false);
          this.error.set(err.message || 'Error al restablecer la contraseña.');
        },
      });
  }
}