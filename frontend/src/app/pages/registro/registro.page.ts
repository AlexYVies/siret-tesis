import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  esCorreoInstitucional,
  ROLES_AUTOREGISTRABLES,
  RolAutoregistrable,
} from '../../core/models/cuentas.model';
import { AuthService } from '../../core/services/auth.service';
import { ApiError } from '../../core/models/api-error';

@Component({
  selector: 'app-registro',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="relative flex min-h-screen items-center justify-center bg-slate-900 px-4 py-12 sm:px-6 overflow-hidden">
      <!-- Fondo decorativo institucional con el patrón geométrico oficial UPT -->
      <div class="absolute inset-0 z-0">
        <img
          src="/assets/img_BannerInicio.svg"
          alt="UPT Fondo Geométrico"
          class="h-full w-full object-cover object-center opacity-30"
        />
        <div class="absolute inset-0 bg-slate-950/80 backdrop-blur-xs"></div>
      </div>

      <div class="relative z-10 w-full max-w-lg">
        <!-- Cabecera institucional con Logo Escudo UPT -->
        <div class="animar-entrada mb-6 text-center">
          <a routerLink="/inicio" class="inline-flex flex-col items-center gap-2">
            <img
              src="/assets/img_LogoUPT.svg"
              alt="UPT Logo Escudo"
              class="h-24 w-auto object-contain drop-shadow-xl transition hover:scale-105 duration-200"
            />
            <span class="rounded-full bg-violet-700/80 px-3.5 py-0.5 text-[11px] font-bold uppercase tracking-wider text-white ring-1 ring-white/20 mt-1">
              SIRET · Sistema Integral de Repositorio y Evaluación de Tesis
            </span>
          </a>
        </div>

        <div class="rounded-3xl border border-slate-200 bg-white p-7 shadow-2xl sm:p-9 animar-entrada">
          <div class="mb-6 flex items-center justify-between border-b border-slate-100 pb-4">
            <div>
              <span class="inline-block rounded-full bg-violet-700 px-3 py-0.5 text-[10px] font-bold uppercase tracking-wider text-white">
                @if (modoInvitacionDocente()) {
                  Invitación Institucional
                } @else if (esInvitado()) {
                  Acceso Invitado
                } @else {
                  Nuevo Alumno
                }
              </span>
              <h1 class="mt-1.5 text-xl font-black uppercase text-slate-900 tracking-tight">
                @if (modoInvitacionDocente()) {
                  Registro de Docente
                } @else if (esInvitado()) {
                  Registro de Invitado
                } @else {
                  Crear Cuenta
                }
              </h1>
              <p class="mt-1 text-xs text-slate-500">
                @if (modoInvitacionDocente()) {
                  Completa tu registro con la invitación enviada por el Administrador
                } @else if (esInvitado()) {
                  Ingresa cualquier correo electrónico para consultar las tesis aprobadas
                } @else {
                  Regístrate como alumno o invitado para acceder a SIRET
                }
              </p>
            </div>
            <a
              [routerLink]="['/login']"
              [queryParams]="modoInvitacionDocente() ? { area: 'docente' } : esInvitado() ? { area: 'invitado' } : { area: 'estudiante' }"
              class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-bold text-slate-600 transition hover:bg-slate-100"
            >
              Volver →
            </a>
          </div>

          <!-- Bloque de error amigable -->
          @if (error()) {
            <div class="mb-5 rounded-2xl border border-rose-200 bg-rose-50 p-4 text-left">
              <div class="flex items-start gap-2.5">
                <span class="flex h-5 w-5 items-center justify-center rounded-full bg-rose-200 text-rose-800 shrink-0 text-xs font-bold mt-0.5">!</span>
                <div class="flex-1">
                  <p class="text-xs font-semibold text-rose-900 leading-relaxed">{{ error() }}</p>

                  @if (errorEsDuplicado()) {
                    <div class="mt-2.5 pt-2.5 border-t border-rose-200">
                      @if (esInvitado()) {
                        <p class="text-[11px] text-rose-700">
                          Ya tienes una cuenta de invitado creada con este correo.
                        </p>
                        <a
                          [routerLink]="['/login']"
                          [queryParams]="{ area: 'invitado' }"
                          class="mt-2 inline-flex items-center gap-1.5 rounded-xl bg-violet-700 px-3.5 py-1.5 text-xs font-bold text-white shadow-xs hover:bg-violet-800 transition"
                        >
                          Ir a iniciar sesión como Invitado →
                        </a>
                      } @else {
                        <p class="text-[11px] text-rose-700">
                          ¿Aún no has activado tu cuenta o no recibiste el correo de validación?
                        </p>

                        @if (reenvioExitoso()) {
                          <div class="mt-2 rounded-xl bg-emerald-100 p-2 text-xs font-semibold text-emerald-900 flex items-center gap-1.5">
                            <svg class="h-4 w-4 text-emerald-700 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                              <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                            </svg>
                            <span>¡Correo enviado a {{ correoDuplicado() }}! Revisa tu bandeja o spam.</span>
                          </div>
                          <a
                            href="https://mail.google.com"
                            target="_blank"
                            rel="noopener noreferrer"
                            class="mt-2 inline-flex items-center gap-1 text-xs font-bold text-violet-700 hover:underline"
                          >
                            Abrir Gmail →
                          </a>
                        } @else {
                          <button
                            type="button"
                            (click)="reenviarValidacionDesdeError()"
                            [disabled]="reenviando()"
                            class="mt-2 inline-flex items-center gap-1.5 rounded-xl bg-violet-700 px-3.5 py-1.5 text-xs font-bold text-white shadow-xs hover:bg-violet-800 transition cursor-pointer"
                          >
                            @if (reenviando()) {
                              Reenviando correo…
                            } @else {
                              Reenviar correo de activación
                            }
                          </button>
                        }
                      }
                    </div>
                  }
                </div>
              </div>
            </div>
          }

          <!-- Pantalla de confirmación para Docente o Invitado con acceso inmediato -->
          @if (accesoDirectoExitoso()) {
            <div class="flex flex-col items-center rounded-2xl border border-emerald-200 bg-emerald-50 px-6 py-8 text-center">
              <span class="animar-pop flex h-14 w-14 items-center justify-center rounded-full bg-emerald-600 text-white shadow-lg">
                <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                </svg>
              </span>
              <h2 class="mt-3 text-base font-black uppercase text-emerald-950 tracking-tight">
                @if (modoInvitacionDocente()) {
                  ¡Cuenta de Docente activada!
                } @else {
                  ¡Acceso de Invitado concedido!
                }
              </h2>
              <p class="mt-1 text-xs text-emerald-700">Entrando a SIRET…</p>
            </div>
          }

          <!-- Pantalla de confirmación de registro de Alumno por correo -->
          @else if (registrado()) {
            <div class="flex flex-col items-center rounded-2xl border border-emerald-200 bg-emerald-50 px-6 py-8 text-center">
              <span class="animar-pop flex h-16 w-16 items-center justify-center rounded-full bg-emerald-600 text-white shadow-lg">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </span>
              <h2 class="mt-4 text-lg font-black uppercase text-slate-900 tracking-tight">¡Cuenta registrada con éxito!</h2>
              <p class="mt-2 text-xs text-slate-600 leading-relaxed">
                Hemos enviado un correo a tu cuenta institucional con el enlace de activación.
              </p>

              <div class="mt-4 rounded-xl bg-emerald-100/80 p-3 text-xs text-emerald-950 border border-emerald-200 text-left w-full">
                <p class="font-bold flex items-center gap-1.5 mb-1 text-emerald-900">
                  <svg class="h-4 w-4 text-emerald-700 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Activación automática con un clic:
                </p>
                <p class="text-[11px] leading-relaxed text-emerald-800">
                  Abre tu bandeja y haz clic en el botón <strong>«Validar mi cuenta»</strong>. Tu perfil se activará al instante sin pasos complicados.
                </p>
              </div>

              <a
                href="https://mail.google.com"
                target="_blank"
                rel="noopener noreferrer"
                class="boton-primario mt-5 flex w-full items-center justify-center gap-2 !py-3 !text-xs !font-bold uppercase tracking-wider shadow-md bg-[#ea4335] hover:bg-[#d93025] text-white"
              >
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M24 5.457v13.909c0 .904-.732 1.636-1.636 1.636h-3.819V11.73L12 16.64l-6.545-4.91v9.262H1.636A1.636 1.636 0 0 1 0 19.366V5.457c0-2.023 2.309-3.178 3.927-1.964L5.455 4.64 12 9.548l6.545-4.91 1.528-1.145C21.69 2.28 24 3.434 24 5.457z"/>
                </svg>
                Abrir mi correo (Gmail)
              </a>

              <div class="mt-4 flex flex-col items-center gap-2 text-xs">
                <a routerLink="/validar-cuenta" class="text-slate-500 hover:text-violet-700 underline">
                  ¿No te llegó el correo? Reenviar enlace
                </a>
                <a [routerLink]="['/login']" [queryParams]="{ area: 'estudiante' }" class="text-violet-700 font-bold hover:underline">
                  Ir a iniciar sesión →
                </a>
              </div>
            </div>
          }

          <!-- MODO INVITACIÓN DE DOCENTE (vía enlace enviado por el Admin) -->
          @else if (modoInvitacionDocente()) {
            <form [formGroup]="formularioDocente" (ngSubmit)="completarRegistroDocente()" class="space-y-4">
              <div class="rounded-2xl border border-violet-200 bg-violet-50 p-3.5 text-xs text-violet-950 flex items-start gap-2.5">
                <svg class="h-4 w-4 text-violet-700 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div>
                  <p class="font-bold">Invitación verificada para Docente</p>
                  <p class="text-[11px] text-violet-800 mt-0.5">
                    Correo institucional asignado: <strong>{{ formularioDocente.value.correo }}</strong>
                  </p>
                </div>
              </div>

              <div class="space-y-3">
                <div>
                  <label for="nombresDocente" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                    Nombre(s)
                  </label>
                  <div class="relative">
                    <input
                      id="nombresDocente"
                      type="text"
                      formControlName="nombres"
                      required
                      placeholder="Ej. Alexis"
                      class="campo !rounded-xl pl-10"
                    />
                    <svg class="h-4.5 w-4.5 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                  </div>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label for="primerApellidoDocente" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                      Primer apellido
                    </label>
                    <input
                      id="primerApellidoDocente"
                      type="text"
                      formControlName="primerApellido"
                      required
                      placeholder="Ej. Villar"
                      class="campo !rounded-xl"
                    />
                  </div>
                  <div>
                    <label for="segundoApellidoDocente" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                      Segundo apellido (opcional)
                    </label>
                    <input
                      id="segundoApellidoDocente"
                      type="text"
                      formControlName="segundoApellido"
                      placeholder="Ej. Escorcia"
                      class="campo !rounded-xl"
                    />
                  </div>
                </div>
              </div>

              <div>
                <label for="contrasenaDocente" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                  Crear contraseña de acceso
                </label>
                <div class="relative">
                  <input
                    id="contrasenaDocente"
                    [type]="mostrarContrasena() ? 'text' : 'password'"
                    formControlName="contrasena"
                    required
                    minlength="8"
                    placeholder="Mínimo 8 caracteres (1 mayúscula y 1 número)"
                    class="campo !rounded-xl pl-10 pr-12"
                  />
                  <svg class="h-4.5 w-4.5 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                  </svg>
                  <button
                    type="button"
                    (click)="mostrarContrasena.update((v) => !v)"
                    class="absolute inset-y-0 right-0 flex w-12 items-center justify-center text-slate-400 hover:text-slate-600 cursor-pointer"
                    [attr.aria-label]="mostrarContrasena() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" class="h-4.5 w-4.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      @if (mostrarContrasena()) {
                        <path stroke-linecap="round" stroke-linejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                      } @else {
                        <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                        <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      }
                    </svg>
                  </button>
                </div>
                <p class="mt-1 text-[11px] text-slate-400">
                  Debe contener al menos 8 caracteres, una mayúscula y un número.
                </p>
              </div>

              <button
                type="submit"
                [disabled]="formularioDocente.invalid || enviando()"
                class="boton-primario w-full !py-3 !text-sm !font-bold uppercase tracking-wider shadow-md bg-violet-700 hover:bg-violet-800 transition"
              >
                @if (enviando()) {
                  Activando cuenta de docente…
                } @else {
                  Activar cuenta de Docente
                }
              </button>
            </form>
          }

          <!-- MODO NORMAL: Selección únicamente entre Alumno o Invitado -->
          @else {
            <form [formGroup]="formulario" (ngSubmit)="registrar()" class="space-y-4">
              <!-- Selección de Rol (Solo Alumno o Invitado) -->
              <div class="space-y-1.5">
                <label class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">Tipo de registro</label>
                <div class="grid grid-cols-2 gap-2.5">
                  @for (rol of ROLES_AUTOREGISTRABLES; track rol) {
                    <button
                      type="button"
                      (click)="seleccionarRol(rol)"
                      class="flex flex-col items-center justify-center rounded-2xl border p-3 text-center transition cursor-pointer"
                      [class.border-violet-600]="rolSeleccionado() === rol"
                      [class.bg-violet-50]="rolSeleccionado() === rol"
                      [class.text-violet-900]="rolSeleccionado() === rol"
                      [class.shadow-xs]="rolSeleccionado() === rol"
                      [class.border-slate-200]="rolSeleccionado() !== rol"
                      [class.bg-slate-50]="rolSeleccionado() !== rol"
                      [class.text-slate-600]="rolSeleccionado() !== rol"
                      [class.hover:bg-slate-100]="rolSeleccionado() !== rol"
                    >
                      <span class="text-xs font-extrabold uppercase tracking-tight">{{ rol }}</span>
                    </button>
                  }
                </div>
              </div>

              <!-- Nombre(s) y Apellidos (Solo cuando es Alumno) -->
              @if (!esInvitado()) {
                <div class="space-y-3">
                  <div>
                    <label for="nombres" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                      Nombre(s)
                    </label>
                    <div class="relative">
                      <input
                        id="nombres"
                        type="text"
                        formControlName="nombres"
                        required
                        placeholder="Ej. Alexis Yahir"
                        class="campo !rounded-xl pl-10"
                      />
                      <svg class="h-4.5 w-4.5 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                      </svg>
                    </div>
                  </div>

                  <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div>
                      <label for="primerApellido" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                        Primer apellido
                      </label>
                      <input
                        id="primerApellido"
                        type="text"
                        formControlName="primerApellido"
                        required
                        placeholder="Ej. Villar"
                        class="campo !rounded-xl"
                      />
                    </div>
                    <div>
                      <label for="segundoApellido" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                        Segundo apellido (opcional)
                      </label>
                      <input
                        id="segundoApellido"
                        type="text"
                        formControlName="segundoApellido"
                        placeholder="Ej. Escorcia"
                        class="campo !rounded-xl"
                      />
                    </div>
                  </div>
                </div>
              }

              <!-- Correo (Institucional para Alumno, cualquier correo para Invitado) -->
              <div>
                <label for="correo" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                  {{ esInvitado() ? 'Correo electrónico (cualquier correo)' : 'Correo institucional' }}
                </label>
                <div class="relative">
                  <input
                    id="correo"
                    type="email"
                    formControlName="correo"
                    (input)="onCorreoInput($event)"
                    required
                    [placeholder]="esInvitado() ? 'ejemplo@gmail.com' : '2230100@upt.edu.mx'"
                    class="campo !rounded-xl pl-10"
                  />
                  <svg class="h-4.5 w-4.5 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                </div>
                @if (esInvitado()) {
                  <p class="mt-1.5 text-[11px] text-slate-500">
                    Como invitado solo necesitas poner tu correo electrónico (no es necesario que sea de la UPT).
                  </p>
                }
              </div>

              <!-- Detección automática de matrícula desde el correo (Solo Alumno) -->
              @if (!esInvitado() && matriculaDetectada()) {
                <div class="rounded-2xl border border-emerald-200 bg-emerald-50/80 p-3 flex items-center justify-between text-xs text-emerald-950 shadow-xs animar-pop">
                  <div class="flex items-center gap-2 font-semibold">
                    <span class="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-200 text-emerald-800 shrink-0">
                      <svg class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                      </svg>
                    </span>
                    <span>Matrícula detectada:</span>
                  </div>
                  <span class="font-mono font-extrabold text-xs bg-white px-2.5 py-1 rounded-xl border border-emerald-300 shadow-xs text-emerald-800">
                    {{ matriculaDetectada() }}
                  </span>
                </div>
              }

              <!-- Contraseña (Solo cuando es Alumno) -->
              @if (!esInvitado()) {
                <div>
                  <label for="contrasena" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">Contraseña</label>
                  <div class="relative">
                    <input
                      id="contrasena"
                      [type]="mostrarContrasena() ? 'text' : 'password'"
                      formControlName="contrasena"
                      required
                      minlength="8"
                      placeholder="Mínimo 8 caracteres (1 mayúscula y 1 número)"
                      class="campo !rounded-xl pl-10 pr-12"
                    />
                    <svg class="h-4.5 w-4.5 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                    <button
                      type="button"
                      (click)="mostrarContrasena.update((v) => !v)"
                      class="absolute inset-y-0 right-0 flex w-12 items-center justify-center text-slate-400 hover:text-slate-600 cursor-pointer"
                      [attr.aria-label]="mostrarContrasena() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" class="h-4.5 w-4.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        @if (mostrarContrasena()) {
                          <path stroke-linecap="round" stroke-linejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                        } @else {
                          <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                          <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        }
                      </svg>
                    </button>
                  </div>
                  <p class="mt-1 text-[11px] text-slate-400">
                    Debe contener al menos 8 caracteres, una mayúscula y un número.
                  </p>
                </div>
              }

              @if (correoInstitucionalIncorrecto) {
                <div class="rounded-xl border border-amber-200 bg-amber-50 p-2.5 text-xs text-amber-900 flex items-center gap-2">
                  <svg class="h-4 w-4 text-amber-600 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                  </svg>
                  <span>Para registrarte como Alumno debes usar tu correo institucional terminado en <strong>@upt.edu.mx</strong>.</span>
                </div>
              }

              <button
                type="submit"
                [disabled]="!formularioValido() || enviando()"
                class="boton-primario w-full !py-3 !text-sm !font-bold uppercase tracking-wider shadow-md bg-violet-700 hover:bg-violet-800 transition"
              >
                @if (enviando()) {
                  <svg class="h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                  </svg>
                  {{ esInvitado() ? 'Creando cuenta de invitado…' : 'Registrando alumno…' }}
                } @else {
                  {{ esInvitado() ? 'Crear cuenta de Invitado' : 'Crear cuenta de Alumno' }}
                }
              </button>
            </form>

            @if (esInvitado()) {
              <div class="mt-5 space-y-3 border-t border-slate-100 pt-4 text-center">
                <a
                  [routerLink]="['/login']"
                  [queryParams]="{ area: 'invitado' }"
                  class="flex w-full items-center justify-center rounded-xl border border-violet-300 bg-violet-50 px-4 py-2.5 text-xs font-extrabold uppercase tracking-wider text-violet-800 shadow-xs transition hover:bg-violet-100"
                >
                  Iniciar sesión con mi cuenta de Invitado
                </a>

                <div class="rounded-2xl border border-slate-200 bg-slate-50 p-3.5">
                  <button
                    type="button"
                    (click)="entrarSinIniciarSesion()"
                    [disabled]="enviando()"
                    class="w-full rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-xs font-extrabold uppercase tracking-wider text-slate-700 shadow-xs transition hover:bg-slate-100 cursor-pointer"
                  >
                    Entrar sin iniciar sesión
                  </button>
                  <p class="mt-2 text-[11px] text-amber-800 font-medium">
                    Nota: Si entras sin iniciar sesión podrás consultar las tesis, pero <strong>no podrás guardar favoritos</strong>.
                  </p>
                </div>
              </div>
            }

            <!-- Enlaces de ayuda -->
            @if (!esInvitado()) {
              <div class="mt-6 border-t border-slate-100 pt-4 text-center space-y-2 text-xs">
                <p class="text-slate-500">
                  ¿Ya te registraste y no recibiste tu correo?
                  <a
                    [routerLink]="['/validar-cuenta']"
                    [queryParams]="formulario.value.correo ? { correo: formulario.value.correo } : null"
                    class="font-bold text-violet-700 hover:underline ml-1"
                  >
                    Reenviar correo de validación
                  </a>
                </p>
                <p class="text-slate-500">
                  ¿Ya tienes cuenta activa?
                  <a [routerLink]="['/login']" [queryParams]="{ area: 'estudiante' }" class="font-bold text-violet-700 hover:underline ml-1">
                    Iniciar sesión
                  </a>
                </p>
              </div>
            }
          }
        </div>
      </div>
    </div>
  `,
})
export class RegistroPage implements OnInit {
  protected readonly ROLES_AUTOREGISTRABLES = ROLES_AUTOREGISTRABLES;

  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly ruta = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly enviando = signal(false);
  protected readonly reenviando = signal(false);
  protected readonly reenvioExitoso = signal(false);
  protected readonly error = signal('');
  protected readonly errorEsDuplicado = signal(false);
  protected readonly correoDuplicado = signal('');
  protected readonly registrado = signal(false);
  protected readonly accesoDirectoExitoso = signal(false);
  protected readonly token = signal<string | null>(null);
  protected readonly correoTexto = signal('');
  protected readonly mostrarContrasena = signal(false);

  protected readonly modoInvitacionDocente = signal(false);
  protected readonly rolSeleccionado = signal<RolAutoregistrable>('Alumno');
  protected readonly esInvitado = computed(() => this.rolSeleccionado() === 'Invitado');

  protected readonly matriculaDetectada = computed(() => {
    const correo = this.correoTexto().trim();
    if (this.rolSeleccionado() === 'Alumno' && correo.includes('@')) {
      const partePrefijo = correo.split('@')[0];
      if (/^\d+$/.test(partePrefijo)) {
        return partePrefijo;
      }
    }
    return '';
  });

  protected readonly formulario = this.fb.nonNullable.group({
    nombres: ['', [Validators.required, Validators.minLength(2)]],
    primerApellido: ['', [Validators.required, Validators.minLength(2)]],
    segundoApellido: [''],
    correo: ['', [Validators.required, Validators.email]],
    contrasena: ['', [Validators.required, Validators.minLength(8)]],
    rol: ['Alumno' as RolAutoregistrable, Validators.required],
  });

  protected readonly formularioDocente = this.fb.nonNullable.group({
    idUsuario: [0, [Validators.required, Validators.min(1)]],
    correo: ['', [Validators.required, Validators.email]],
    nombres: ['', [Validators.required, Validators.minLength(2)]],
    primerApellido: ['', [Validators.required, Validators.minLength(2)]],
    segundoApellido: [''],
    contrasena: ['', [Validators.required, Validators.minLength(8)]],
    token: ['', Validators.required],
  });

  ngOnInit(): void {
    this.ruta.queryParamMap.subscribe((params) => {
      const tokenInvitacion = params.get('invitacion');
      const idParam = params.get('id');
      const correoParam = params.get('correo');
      const nombreParam = params.get('nombre');

      if (tokenInvitacion && idParam && correoParam) {
        const idNum = parseInt(idParam, 10);
        if (!isNaN(idNum) && idNum > 0) {
          this.modoInvitacionDocente.set(true);
          const desglosado = this.desglosarNombre(nombreParam ?? '', correoParam);
          this.formularioDocente.patchValue({
            idUsuario: idNum,
            correo: correoParam.trim(),
            nombres: desglosado.nombres,
            primerApellido: desglosado.primerApellido,
            segundoApellido: desglosado.segundoApellido,
            token: tokenInvitacion.trim(),
          });
          return;
        }
      }

      this.modoInvitacionDocente.set(false);
      const rolParam = params.get('rol');
      if (rolParam === 'Invitado' || rolParam === 'Alumno') {
        this.seleccionarRol(rolParam);
      }
    });
  }

  private desglosarNombre(nombreCompleto: string, correo?: string): { nombres: string; primerApellido: string; segundoApellido: string } {
    const texto = (nombreCompleto || '').trim();
    if (texto) {
      const tokens = texto.split(/\s+/).filter(Boolean);
      if (tokens.length >= 4) {
        return {
          nombres: tokens.slice(0, tokens.length - 2).join(' '),
          primerApellido: tokens[tokens.length - 2],
          segundoApellido: tokens[tokens.length - 1],
        };
      }
      if (tokens.length === 3) {
        return {
          nombres: tokens[0],
          primerApellido: tokens[1],
          segundoApellido: tokens[2],
        };
      }
      if (tokens.length === 2) {
        return {
          nombres: tokens[0],
          primerApellido: tokens[1],
          segundoApellido: '',
        };
      }
      if (tokens.length === 1) {
        return {
          nombres: tokens[0],
          primerApellido: '',
          segundoApellido: '',
        };
      }
    }

    if (correo && correo.includes('@')) {
      const prefijo = correo.split('@')[0];
      if (prefijo.includes('.')) {
        const partes = prefijo.split('.').filter(Boolean);
        if (partes.length >= 2) {
          const cap = (s: string) => s.charAt(0).toUpperCase() + s.slice(1).toLowerCase();
          return {
            nombres: cap(partes[0]),
            primerApellido: cap(partes[1]),
            segundoApellido: '',
          };
        }
      }
    }

    return { nombres: '', primerApellido: '', segundoApellido: '' };
  }

  protected get correoInstitucionalIncorrecto(): boolean {
    const correo = (this.formulario.value.correo ?? '').trim();
    return (
      !this.esInvitado() &&
      correo.length > 0 &&
      !esCorreoInstitucional(correo)
    );
  }

  protected formularioValido(): boolean {
    const correoControl = this.formulario.controls.correo;
    if (this.esInvitado()) {
      return correoControl.valid && (correoControl.value?.trim().length ?? 0) > 0;
    }
    return (
      this.formulario.controls.nombres.valid &&
      this.formulario.controls.primerApellido.valid &&
      this.formulario.controls.contrasena.valid &&
      this.formulario.controls.correo.valid &&
      !this.correoInstitucionalIncorrecto
    );
  }

  seleccionarRol(rol: RolAutoregistrable): void {
    this.rolSeleccionado.set(rol);
    this.formulario.patchValue({ rol });
    this.error.set('');
    this.errorEsDuplicado.set(false);
  }

  onCorreoInput(event: Event): void {
    const valor = (event.target as HTMLInputElement)?.value ?? '';
    this.correoTexto.set(valor);
  }

  registrar(): void {
    if (!this.formularioValido()) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');
    this.errorEsDuplicado.set(false);
    this.reenvioExitoso.set(false);

    const correo = this.formulario.value.correo!.trim();
    const rol = this.rolSeleccionado();

    if (rol === 'Invitado') {
      this.auth
        .registrar({
          correo,
          rol: 'Invitado',
        })
        .subscribe({
          next: (respuesta) => {
            this.enviando.set(false);
            if (respuesta.token) {
              this.auth.guardarSesion({
                token: respuesta.token,
                expiraEn: respuesta.expiraEn ?? new Date(Date.now() + 3600000).toISOString(),
                idUsuario: respuesta.idUsuario,
                correo: respuesta.correo ?? correo,
                roles: respuesta.roles ?? ['Invitado'],
              });
            }
            this.accesoDirectoExitoso.set(true);
            setTimeout(() => void this.router.navigate(['/repositorio']), 800);
          },
          error: (err: ApiError) => {
            this.enviando.set(false);
            this.procesarError(err, correo);
          },
        });
      return;
    }

    const val = this.formulario.getRawValue();
    const nombres = val.nombres.trim();
    const primerApellido = val.primerApellido.trim();
    const segundoApellido = val.segundoApellido.trim();
    const nombreCompleto = [nombres, primerApellido, segundoApellido].filter(Boolean).join(' ');
    const matricula = this.matriculaDetectada() || undefined;

    this.auth
      .registrar({
        correo,
        nombre: nombreCompleto,
        nombres,
        primerApellido,
        segundoApellido,
        matricula,
        contrasena: val.contrasena,
        rol: 'Alumno',
      })
      .subscribe({
        next: (respuesta) => {
          this.enviando.set(false);
          this.token.set(respuesta.tokenValidacion ?? null);
          this.registrado.set(true);
        },
        error: (err: ApiError) => {
          this.enviando.set(false);
          this.procesarError(err, correo);
        },
      });
  }

  entrarSinIniciarSesion(): void {
    this.enviando.set(true);
    this.error.set('');
    this.errorEsDuplicado.set(false);

    this.auth.loginInvitado().subscribe({
      next: (respuesta) => {
        this.auth.guardarSesion(respuesta);
        this.enviando.set(false);
        this.accesoDirectoExitoso.set(true);
        setTimeout(() => void this.router.navigate(['/repositorio']), 800);
      },
      error: (err: ApiError) => {
        this.enviando.set(false);
        this.error.set(err.message || 'No fue posible entrar como invitado.');
      },
    });
  }

  completarRegistroDocente(): void {
    if (this.formularioDocente.invalid) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');

    const datos = this.formularioDocente.getRawValue();
    const nombres = datos.nombres.trim();
    const primerApellido = datos.primerApellido.trim();
    const segundoApellido = datos.segundoApellido.trim();
    const nombreCompleto = [nombres, primerApellido, segundoApellido].filter(Boolean).join(' ');

    this.auth
      .completarRegistroDocente({
        idUsuario: datos.idUsuario,
        correo: datos.correo.trim(),
        nombre: nombreCompleto,
        nombres,
        primerApellido,
        segundoApellido,
        contrasena: datos.contrasena,
        token: datos.token,
      })
      .subscribe({
        next: (respuesta) => {
          this.enviando.set(false);
          if (respuesta.token) {
            this.auth.guardarSesion({
              token: respuesta.token,
              expiraEn: respuesta.expiraEn ?? new Date(Date.now() + 3600000).toISOString(),
              idUsuario: respuesta.idUsuario,
              correo: respuesta.correo ?? datos.correo,
              roles: respuesta.roles ?? ['Docente'],
            });
          }
          this.accesoDirectoExitoso.set(true);
          setTimeout(() => void this.router.navigate(['/asesorias']), 900);
        },
        error: (err: ApiError) => {
          this.enviando.set(false);
          this.procesarError(err, datos.correo);
        },
      });
  }

  reenviarValidacionDesdeError(): void {
    const correo = this.correoDuplicado() || this.formulario.value.correo?.trim();
    if (!correo) return;

    this.reenviando.set(true);
    this.auth.reenviarConfirmacion({ correo }).subscribe({
      next: () => {
        this.reenviando.set(false);
        this.reenvioExitoso.set(true);
      },
      error: (err: ApiError) => {
        this.reenviando.set(false);
        if (err.message && err.message.toLowerCase().includes('ya está validada')) {
          this.error.set('Esta cuenta ya está validada. Puedes iniciar sesión directamente.');
        } else {
          this.reenvioExitoso.set(true);
        }
      },
    });
  }

  private procesarError(err: ApiError, correo: string): void {
    const msg = err.message || '';
    const codigo = err.code || '';

    if (
      codigo === 'CORREO_REGISTRADO_PENDIENTE_VALIDACION' ||
      codigo === 'CORREO_YA_REGISTRADO' ||
      msg.toLowerCase().includes('already taken') ||
      msg.toLowerCase().includes('ya se encuentra registrado') ||
      msg.toLowerCase().includes('ya está registrado')
    ) {
      this.errorEsDuplicado.set(true);
      this.correoDuplicado.set(correo);
      this.error.set(
        this.esInvitado()
          ? 'Este correo electrónico ya está registrado como invitado.'
          : 'Este correo institucional ya está registrado en el sistema.',
      );
      return;
    }

    if (
      msg.toLowerCase().includes('password') ||
      msg.toLowerCase().includes('contraseña')
    ) {
      this.error.set(
        'La contraseña debe tener mínimo 8 caracteres, al menos un número y una letra mayúscula.'
      );
      return;
    }

    if (codigo === 'CORREO_NO_INSTITUCIONAL') {
      this.error.set(
        'El correo debe ser institucional y pertenecer al dominio @upt.edu.mx.'
      );
      return;
    }

    this.error.set(msg || 'No fue posible completar el registro. Por favor verifica tus datos.');
  }
}