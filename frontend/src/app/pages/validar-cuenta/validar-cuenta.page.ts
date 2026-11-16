import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { ApiError } from '../../core/models/api-error';

@Component({
  selector: 'app-validar-cuenta',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12 sm:px-6">
      <div class="tarjeta animar-entrada w-full max-w-md p-8 sm:p-10 shadow-xl border border-slate-100 rounded-3xl bg-white">
        <!-- Logo UPT -->
        <a routerLink="/inicio" class="mb-8 flex items-center justify-center gap-3">
          <span class="degradado-marca flex h-12 w-12 items-center justify-center rounded-2xl text-lg font-black text-white shadow-md">
            U
          </span>
          <div class="flex flex-col">
            <span class="text-base font-extrabold text-[#001e61]">UPT</span>
            <span class="text-xs font-semibold text-slate-500">SIRET · Sistema Integral de Repositorio y Evaluación de Tesis</span>
          </div>
        </a>

        <!-- ESTADO 1: Validando automáticamente desde enlace de correo -->
        @if (validandoAuto()) {
          <div class="flex flex-col items-center py-6 text-center animar-entrada">
            <div class="relative flex items-center justify-center my-4">
              <div class="h-16 w-16 animate-spin rounded-full border-4 border-slate-200 border-t-[#001e61]"></div>
              <span class="absolute text-xs font-black text-[#001e61]">UPT</span>
            </div>
            <h1 class="text-xl font-bold text-slate-900">Validando tu cuenta...</h1>
            <p class="mt-2 text-sm text-slate-500 max-w-xs">
              Por favor espera un momento mientras confirmamos tu correo institucional.
            </p>
          </div>
        }

        <!-- ESTADO 2: Cuenta validada exitosamente -->
        @else if (mensajeExito() && !modoReenvio()) {
          <div class="flex flex-col items-center py-4 text-center animar-pop">
            <span class="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500 text-white shadow-lg">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-9 w-9" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
              </svg>
            </span>
            <h1 class="mt-5 text-xl font-bold text-slate-900">¡Cuenta validada exitosamente!</h1>
            <p class="mt-2 text-sm text-slate-600">
              {{ mensajeExito() }}
            </p>
            <div class="mt-4 flex items-center justify-center gap-2 text-xs text-slate-500">
              <svg class="h-4 w-4 animate-spin text-[#001e61]" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
              </svg>
              Redirigiendo al inicio de sesión…
            </div>
            <a routerLink="/login" class="boton-primario mt-6 w-full">
              Iniciar sesión ahora
            </a>
          </div>
        }

        <!-- ESTADO 3: Error en validación automática -->
        @else if (error() && !modoReenvio() && enlaceDetectado()) {
          <div class="flex flex-col items-center py-4 text-center animar-entrada">
            <span class="flex h-14 w-14 items-center justify-center rounded-full bg-rose-100 text-rose-600 shadow-sm">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </span>
            <h1 class="mt-4 text-lg font-bold text-slate-900">No se pudo validar la cuenta</h1>
            <div class="aviso-error mt-3 w-full text-center"> {{ error() }} </div>
            <p class="mt-3 text-xs text-slate-500">
              El enlace de validación pudo haber expirado o la cuenta ya fue activada anteriormente.
            </p>
            <div class="mt-6 flex w-full flex-col gap-3">
              <button type="button" (click)="abrirReenvio()" class="boton-primario w-full">
                Solicitar nuevo enlace de validación
              </button>
              <a routerLink="/login" class="boton-secundario w-full text-center">
                Ir a iniciar sesión
              </a>
            </div>
          </div>
        }

        <!-- ESTADO 4: Solicitar reenvío de correo (o entrada directa sin parámetros) -->
        @else {
          <h1 class="text-2xl font-bold text-slate-900">Reenviar correo de validación</h1>
          <p class="mt-1 text-sm text-slate-500">
            ¿No recibiste el correo o expiró tu enlace? Ingresa tu correo institucional registrado para enviarte un nuevo enlace de activación directa.
          </p>

          @if (error()) {
            <div class="aviso-error mt-4"> {{ error() }} </div>
          }

          @if (mensajeReenvio()) {
            <div class="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-center">
              <span class="inline-flex h-9 w-9 items-center justify-center rounded-full bg-emerald-500 text-white shadow-sm mb-2">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                </svg>
              </span>
              <p class="text-sm font-semibold text-emerald-800">{{ mensajeReenvio() }}</p>
              <p class="mt-1 text-xs text-emerald-600">Revisa tu bandeja de entrada o spam.</p>
              <a
                href="https://mail.google.com"
                target="_blank"
                rel="noopener noreferrer"
                class="boton-primario mt-4 flex w-full items-center justify-center gap-2 text-xs"
              >
                Abrir Gmail
              </a>
            </div>
          } @else {
            <form [formGroup]="formularioReenvio" (ngSubmit)="solicitarReenvio()" class="mt-6 space-y-4">
              <div>
                <label for="correoReenvio" class="campo-label">Correo institucional</label>
                <input
                  id="correoReenvio"
                  type="email"
                  formControlName="correo"
                  required
                  placeholder="ejemplo@upt.edu.mx"
                  class="campo"
                />
              </div>

              <button type="submit" [disabled]="formularioReenvio.invalid || enviando()" class="boton-primario w-full">
                @if (enviando()) {
                  <svg class="h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                  </svg>
                  Enviando enlace…
                } @else {
                  Enviar enlace de activación
                }
              </button>
            </form>
          }

          <!-- Opción secundaria para desarrolladores / token manual -->
          <div class="mt-6 border-t border-slate-100 pt-4 text-center">
            <button
              type="button"
              (click)="mostrarManual.set(!mostrarManual())"
              class="text-xs text-slate-400 hover:text-slate-600 underline"
            >
              {{ mostrarManual() ? 'Ocultar validación manual' : '¿Cuentas con un token manual? Validar aquí' }}
            </button>
          </div>

          @if (mostrarManual()) {
            <form [formGroup]="formulario" (ngSubmit)="validarManual()" class="mt-4 space-y-4 rounded-xl bg-slate-50 p-4 border border-slate-200">
              <div>
                <label for="idUsuario" class="campo-label text-xs">Identificador de usuario</label>
                <input
                  id="idUsuario"
                  type="number"
                  formControlName="idUsuario"
                  required
                  placeholder="Ej. 10"
                  class="campo text-xs py-1.5"
                />
              </div>

              <div>
                <label for="token" class="campo-label text-xs">Token de validación</label>
                <textarea
                  id="token"
                  formControlName="token"
                  required
                  rows="2"
                  placeholder="Pegue aquí el token recibido"
                  class="campo font-mono text-xs"
                ></textarea>
              </div>

              <button type="submit" [disabled]="formulario.invalid || enviando()" class="boton-secundario w-full text-xs py-2">
                Validar token manual
              </button>
            </form>
          }
        }

        <p class="mt-6 text-center text-sm">
          <a routerLink="/login" class="enlace">Ir a iniciar sesión</a>
        </p>
      </div>
    </section>
  `,
})
export class ValidarCuentaPage implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ruta = inject(ActivatedRoute);

  private subscripcion?: Subscription;
  private yaIntentoAutoValidar = false;

  protected readonly enviando = signal(false);
  protected readonly validandoAuto = signal(false);
  protected readonly enlaceDetectado = signal(false);
  protected readonly error = signal('');
  protected readonly mensajeExito = signal('');
  protected readonly mensajeReenvio = signal('');
  protected readonly modoReenvio = signal(false);
  protected readonly mostrarManual = signal(false);

  protected readonly formulario = this.fb.nonNullable.group({
    idUsuario: [0, [Validators.required, Validators.min(1)]],
    token: ['', Validators.required],
  });

  protected readonly formularioReenvio = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
  });

  ngOnInit(): void {
    this.subscripcion = this.ruta.queryParamMap.subscribe((params) => {
      const correoParam = params.get('correo');
      if (correoParam) {
        this.formularioReenvio.patchValue({ correo: correoParam.trim() });
      }

      const idParam = params.get('id') ?? params.get('idUsuario');
      const tokenParam = params.get('token');

      if (idParam && tokenParam && !this.yaIntentoAutoValidar) {
        this.yaIntentoAutoValidar = true;
        this.enlaceDetectado.set(true);

        const idNumero = parseInt(idParam, 10);
        if (!isNaN(idNumero) && idNumero > 0) {
          this.formulario.patchValue({
            idUsuario: idNumero,
            token: tokenParam.trim(),
          });
          this.ejecutarValidacion(idNumero, tokenParam.trim());
        } else {
          this.error.set('El enlace de validación no contiene un identificador válido.');
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscripcion?.unsubscribe();
  }

  ejecutarValidacion(idUsuario: number, token: string): void {
    this.validandoAuto.set(true);
    this.error.set('');
    this.mensajeExito.set('');

    this.auth.validarCuenta({ idUsuario, token }).subscribe({
      next: () => {
        this.validandoAuto.set(false);
        this.mensajeExito.set('Tu cuenta ha sido activada correctamente. Ya puedes acceder al sistema SIRET.');
        setTimeout(() => {
          void this.router.navigate(['/login']);
        }, 2200);
      },
      error: (err: ApiError) => {
        this.validandoAuto.set(false);
        const msg = (err.message || '').toLowerCase();
        if (msg.includes('ya') && (msg.includes('valida') || msg.includes('confirma'))) {
          this.mensajeExito.set('Esta cuenta ya se encuentra validada. Puedes iniciar sesión directamente.');
          setTimeout(() => {
            void this.router.navigate(['/login']);
          }, 2000);
          return;
        }
        this.error.set(
          'El enlace de validación no es válido o ha expirado. Por favor solicita uno nuevo con el botón de abajo.'
        );
      },
    });
  }

  validarManual(): void {
    if (this.formulario.invalid) {
      return;
    }

    const { idUsuario, token } = this.formulario.getRawValue();
    this.ejecutarValidacion(idUsuario, token);
  }

  abrirReenvio(): void {
    this.error.set('');
    this.mensajeExito.set('');
    this.mensajeReenvio.set('');
    this.modoReenvio.set(true);
    this.enlaceDetectado.set(false);
  }

  solicitarReenvio(): void {
    if (this.formularioReenvio.invalid) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');
    this.mensajeReenvio.set('');

    this.auth.reenviarConfirmacion({ correo: this.formularioReenvio.value.correo! }).subscribe({
      next: () => {
        this.enviando.set(false);
        this.mensajeReenvio.set('Se ha enviado un nuevo enlace de activación a tu correo institucional.');
      },
      error: (err: ApiError) => {
        this.enviando.set(false);
        this.error.set(err.message || 'No se pudo enviar el correo de validación.');
      },
    });
  }
}