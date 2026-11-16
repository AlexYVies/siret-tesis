import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { ApiError } from '../../core/models/api-error';

@Component({
  selector: 'app-recuperar-contrasena',
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

        <h1 class="text-2xl font-bold text-slate-900">Recuperar contraseña</h1>
        <p class="mt-1 text-sm text-slate-500">Le enviaremos las instrucciones a su correo institucional.</p>

        @if (error()) {
          <div class="aviso-error"> {{ error() }} </div>
        }

        @if (instrucciones()) {
          <div class="mt-6 flex flex-col items-center rounded-2xl border border-emerald-200 bg-emerald-50 px-6 py-8 text-center shadow-sm">
            <span class="animar-pop flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500 text-white shadow-lg">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </span>
            <h2 class="mt-4 text-lg font-bold text-slate-900">Enlace de recuperación enviado</h2>
            <p class="mt-2 text-sm text-slate-600">
              Hemos enviado las instrucciones para restablecer tu contraseña a tu correo institucional.
            </p>
            <p class="mt-2 text-xs text-slate-500">
              Abre el correo recibido y haz clic en <strong>«Restablecer mi contraseña»</strong> para definir tu nueva contraseña directamente.
            </p>

            <a
              href="https://mail.google.com"
              target="_blank"
              rel="noopener noreferrer"
              class="boton-primario mt-5 flex w-full items-center justify-center gap-2 shadow-md hover:shadow-lg transition-all"
            >
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M24 5.457v13.909c0 .904-.732 1.636-1.636 1.636h-3.819V11.73L12 16.64l-6.545-4.91v9.262H1.636A1.636 1.636 0 0 1 0 19.366V5.457c0-2.023 2.309-3.178 3.927-1.964L5.455 4.64 12 9.548l6.545-4.91 1.528-1.145C21.69 2.28 24 3.434 24 5.457z"/>
              </svg>
              Abrir mi correo (Gmail)
            </a>

            <div class="mt-4 flex flex-col items-center gap-2 text-xs">
              <a routerLink="/login" class="text-slate-600 font-medium hover:text-upt-azul">
                Volver a inicio de sesión
              </a>
            </div>
          </div>
        } @else {
          <form [formGroup]="formulario" (ngSubmit)="recuperar()" class="mt-6 space-y-4">
            <div>
              <label for="correo" class="campo-label">Correo institucional</label>
              <input
                id="correo"
                type="email"
                formControlName="correo"
                required
                placeholder="usuario@@upt.edu.mx"
                class="campo"
              />
            </div>

            <button type="submit" [disabled]="formulario.invalid || enviando()" class="boton-primario w-full">
              @if (enviando()) {
                <svg class="h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                </svg>
                Enviando…
              } @else {
                Enviar instrucciones
              }
            </button>
          </form>

          <p class="mt-6 text-center text-sm">
            <a routerLink="/login" class="enlace">Volver a iniciar sesión</a>
          </p>
        }
      </div>
    </section>
  `,
})
export class RecuperarContrasenaPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  protected readonly enviando = signal(false);
  protected readonly error = signal('');
  protected readonly instrucciones = signal(false);
  protected readonly token = signal<string | null>(null);

  protected readonly formulario = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
  });

  recuperar(): void {
    if (this.formulario.invalid) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');

    this.auth
      .recuperarContrasena({ correo: this.formulario.value.correo! })
      .subscribe({
        next: (respuesta) => {
          this.enviando.set(false);
          this.token.set(respuesta.tokenRestablecimiento ?? null);
          this.instrucciones.set(true);
        },
        error: (err: ApiError) => {
          this.enviando.set(false);
          this.error.set(err.message);
        },
      });
  }
}