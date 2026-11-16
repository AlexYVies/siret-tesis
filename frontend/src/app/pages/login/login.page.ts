import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { ApiError } from '../../core/models/api-error';
import { Rol, ROLES_PERSONAL, RolValue } from '../../core/models/constantes.model';

type AreaAcceso = 'docente' | 'estudiante' | 'institucion' | 'invitado';
type PasoLogin = 'area' | 'formulario';

interface AreaAccesoInfo {
  valor: AreaAcceso;
  titulo: string;
  badge: string;
  descripcion: string;
  rol: string;
  imagen: string;
}

const AREAS: AreaAccesoInfo[] = [
  {
    valor: 'docente',
    titulo: 'Docente',
    badge: 'DOCENTE',
    descripcion: 'Confirma asesorías de tesis, revisa entregas de alumnos y emite dictámenes.',
    rol: Rol.Docente,
    imagen: '/assets/docente.png',
  },
  {
    valor: 'estudiante',
    titulo: 'Estudiante',
    badge: 'ESTUDIANTE',
    descripcion: 'Registra tu proyecto de tesis, sube avances y consulta el estado de tu titulación.',
    rol: Rol.Alumno,
    imagen: '/assets/estudiante.png',
  },
  {
    valor: 'institucion',
    titulo: 'Administrativo',
    badge: 'ADMINISTRATIVO',
    descripcion: 'Dirección de carrera, administración escolar, gestión institucional y catálogos.',
    rol: ROLES_PERSONAL.join('/'),
    imagen: '/assets/admin.png',
  },
  {
    valor: 'invitado',
    titulo: 'Invitado',
    badge: 'INVITADO',
    descripcion: 'Inicia sesión, crea tu cuenta o entra sin registrarte para consultar el repositorio.',
    rol: Rol.Invitado,
    imagen: '/assets/aspirante.png',
  },
];

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="relative flex min-h-screen flex-col font-sans select-none bg-slate-900 overflow-x-hidden">
      <!-- Fondo decorativo institucional anterior con el patrón geométrico oficial UPT -->
      <div class="absolute inset-0 z-0 pointer-events-none">
        <img
          src="/assets/img_BannerInicio.svg"
          alt="UPT Fondo Geométrico"
          class="h-full w-full object-cover object-center opacity-30"
        />
        <div class="absolute inset-0 bg-slate-950/80 backdrop-blur-xs"></div>
      </div>

      <!-- Banner institucional superior idéntico a SIGAE -->
      <header class="flex w-full items-center justify-between bg-[#001e61] px-0 shadow-md shrink-0 z-20">
        <!-- Bloque amarillo UPT en el extremo izquierdo con el logo oficial -->
        <a
          routerLink="/inicio"
          class="flex items-center bg-[#ffcc33] px-5 py-2 sm:px-8 sm:py-2.5 transition hover:brightness-95"
          title="Ir al inicio"
        >
          <img
            src="/assets/logo_cabecera.png"
            alt="Universidad Politécnica de Tulancingo"
            class="h-9 sm:h-11 w-auto object-contain"
          />
        </a>

        <!-- Texto de SIRET institucional -->
        <div class="flex items-center gap-2 px-2 text-center">
          <span class="rounded-full bg-white/10 px-3 sm:px-3.5 py-1 text-[10px] sm:text-[11px] font-bold uppercase tracking-wider text-amber-300 ring-1 ring-white/15">
            SIRET · Sistema Integral de Repositorio y Evaluación de Tesis
          </span>
        </div>

        <!-- Botón REGRESAR en el extremo derecho estilo SIGAE -->
        <div class="pr-4 sm:pr-8">
          <button
            type="button"
            (click)="regresar()"
            class="inline-flex items-center justify-center rounded-sm bg-[#ffcc33] px-5 sm:px-6 py-2 text-xs sm:text-sm font-black uppercase tracking-wider text-slate-900 shadow-sm transition hover:bg-[#f5c226] hover:shadow-md cursor-pointer"
          >
            Regresar
          </button>
        </div>
      </header>

      <!-- Contenedor central con scroll natural -->
      <main class="flex-1 flex items-center justify-center px-4 py-8 sm:py-12 relative z-10">
        @if (paso() === 'area') {
          <!-- PASO 1: Selección de Área con botones circulares idénticos a SIGAE -->
          <div class="w-full max-w-4xl py-4 sm:py-6 animar-entrada text-center">
            <div class="mb-6 sm:mb-8">
              <span class="inline-block rounded-full bg-[#73002b] px-4 py-1 text-xs font-bold uppercase tracking-wider text-white shadow-md ring-1 ring-white/20">
                Selección de Área
              </span>
              <h1 class="mt-2.5 text-2xl font-black uppercase text-white tracking-tight drop-shadow-md">
                ¿Qué área eres?
              </h1>
              <p class="mt-1 text-xs text-slate-300">
                Selecciona tu perfil universitario para ingresar al sistema
              </p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-y-10 sm:gap-y-12 gap-x-12 sm:gap-x-20 max-w-3xl mx-auto items-center justify-items-center">
              @for (area of areas; track area.valor; let i = $index) {
                <div class="flex flex-col items-center text-center">
                  <button
                    type="button"
                    (click)="elegirArea(area.valor)"
                    class="group flex flex-col items-center focus:outline-none cursor-pointer transition-transform duration-300 hover:scale-105"
                  >
                    <!-- Círculo ilustrado SIGAE con borde guinda 5px y sombra profunda -->
                    <div
                      class="flex h-44 w-44 sm:h-52 sm:w-52 md:h-56 md:w-56 items-center justify-center rounded-full border-[5px] border-[#73002b] bg-white shadow-[0_12px_40px_rgba(0,0,0,0.7)] transition-all duration-300 group-hover:border-[#880f54] group-hover:shadow-[0_15px_45px_rgba(115,0,43,0.6)]"
                      [style.backgroundImage]="'url(' + area.imagen + ')'"
                      style="background-size: 85%; background-repeat: no-repeat; background-position: center;"
                    ></div>

                    <!-- Badge guinda/rojo quemado SIGAE -->
                    <span
                      class="mt-4 inline-block rounded-md bg-[#73002b] ring-1 ring-white/20 px-5 py-1.5 text-sm sm:text-base font-black uppercase tracking-wider text-white shadow-md transition-colors duration-200 group-hover:bg-[#880f54]"
                    >
                      {{ area.badge }}
                    </span>
                  </button>
                </div>
              }
            </div>
          </div>
        } @else {
          <!-- PASO 2: Formulario de Login estilo caja SIGAE -->
          <div class="animar-entrada w-full max-w-lg">
            <div class="rounded-3xl border border-slate-200 bg-white/95 backdrop-blur-xs p-6 shadow-2xl sm:p-9">
              <!-- Encabezado de la caja con botón Regresar interno -->
              <div class="mb-5 flex items-center justify-between border-b border-slate-100 pb-4">
                <div>
                  <span class="inline-block rounded-md bg-[#73002b] px-3.5 py-1 text-[11px] font-black uppercase tracking-wider text-white shadow-xs">
                    {{ areaActual().badge }}
                  </span>
                  <h1 class="mt-2 text-xl font-black uppercase text-slate-900 tracking-tight">
                    {{ areaSeleccionada() === 'invitado' ? 'Acceso Invitado' : 'Acceso Principal' }}
                  </h1>
                </div>
                <button
                  type="button"
                  (click)="volverASeleccionArea()"
                  class="rounded-lg bg-[#880f54] hover:bg-[#73002b] px-3.5 py-1.5 text-xs font-bold uppercase tracking-wider text-white transition shadow-sm cursor-pointer"
                >
                  ← Regresar
                </button>
              </div>

              <p class="mb-5 text-xs text-slate-500">
                {{ areaActual().descripcion }}
              </p>

              @if (error()) {
                <div class="mb-4 rounded-2xl border border-rose-200 bg-rose-50 p-4 text-left">
                  <div class="flex items-start gap-2.5">
                    <span class="flex h-5 w-5 items-center justify-center rounded-full bg-rose-200 text-rose-800 shrink-0 text-xs font-bold mt-0.5">!</span>
                    <div class="flex-1">
                      <p class="text-xs font-semibold text-rose-900 leading-relaxed">{{ error() }}</p>

                      @if (areaSugerida(); as sugerida) {
                        <div class="mt-2.5 pt-2.5 border-t border-rose-200">
                          <p class="text-[11px] text-rose-700">
                            Detectamos que tu perfil corresponde al área de <strong>{{ nombreAreaSugerida() }}</strong>.
                          </p>
                          <button
                            type="button"
                            (click)="cambiarAAreaSugerida()"
                            class="mt-2 inline-flex items-center gap-1.5 rounded-xl bg-[#73002b] px-3.5 py-1.5 text-xs font-bold text-white shadow-xs hover:bg-[#880f54] transition"
                          >
                            Ir a la sección de {{ nombreAreaSugerida() }} →
                          </button>
                        </div>
                      }

                      @if (errorEsCuentaNoValidada()) {
                        <div class="mt-2.5 pt-2.5 border-t border-rose-200">
                          <p class="text-[11px] text-rose-700">
                            ¿Aún no has recibido el correo de activación o expiró tu enlace?
                          </p>

                          @if (reenvioValidacionExitoso()) {
                            <div class="mt-2 rounded-xl bg-emerald-100 p-2 text-xs font-semibold text-emerald-900 flex items-center gap-1.5">
                              <svg class="h-4 w-4 text-emerald-700 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                              </svg>
                              ¡Enlace enviado! Revisa tu bandeja o spam.
                            </div>
                            <a
                              href="https://mail.google.com"
                              target="_blank"
                              rel="noopener noreferrer"
                              class="mt-2 inline-flex items-center gap-1 text-xs font-bold text-[#73002b] hover:underline"
                            >
                              Abrir Gmail →
                            </a>
                          } @else {
                            <button
                              type="button"
                              (click)="reenviarValidacionDesdeLogin()"
                              [disabled]="reenviandoValidacion()"
                              class="mt-2 inline-flex items-center gap-1.5 rounded-xl bg-[#73002b] px-3 py-1.5 text-xs font-bold text-white shadow-xs hover:bg-[#880f54] transition"
                            >
                              @if (reenviandoValidacion()) {
                                Reenviando enlace…
                              } @else {
                                Reenviar enlace de activación
                              }
                            </button>
                          }
                        </div>
                      }
                    </div>
                  </div>
                </div>
              }

              @if (exito()) {
                <div class="flex flex-col items-center rounded-2xl border border-emerald-200 bg-emerald-50 px-6 py-10 text-center">
                  <span class="animar-pop flex h-14 w-14 items-center justify-center rounded-full bg-emerald-600 text-white shadow-lg">
                    <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                  </span>
                  <p class="mt-3 text-sm font-bold text-emerald-900">Sesión iniciada correctamente</p>
                  <p class="text-xs text-emerald-700">Entrando a su portal de posgrado…</p>
                </div>
              } @else if (areaSeleccionada() === 'invitado') {
                <!-- SECCIÓN INVITADO: 1) Iniciar sesión, 2) Crear cuenta, 3) Entrar sin iniciar sesión -->
                <div class="space-y-5">
                  <!-- Opción 1: Iniciar sesión con correo de invitado -->
                  <form [formGroup]="formularioInvitado" (ngSubmit)="iniciarSesionInvitado()" class="space-y-3">
                    <div>
                      <label for="correoInvitado" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">
                        Iniciar sesión como Invitado
                      </label>
                      <input
                        id="correoInvitado"
                        type="email"
                        formControlName="correo"
                        autocomplete="email"
                        placeholder="Ingresa tu correo registrado (ej. correo@gmail.com)"
                        class="campo !rounded-xl"
                      />
                      <p class="mt-1 text-[11px] text-slate-500">
                        Ingresa con tu correo de invitado para consultar tesis y guardar tus favoritos.
                      </p>
                    </div>

                    <button
                      type="submit"
                      [disabled]="formularioInvitado.invalid || enviando()"
                      class="w-full rounded-xl bg-[#73002b] py-2.5 text-xs font-bold uppercase tracking-wider text-white shadow-md hover:bg-[#880f54] active:scale-[0.99] transition cursor-pointer disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      @if (enviando()) {
                        Ingresando…
                      } @else {
                        Iniciar sesión como Invitado
                      }
                    </button>
                  </form>

                  <!-- Opción 2: Crear cuenta de invitado -->
                  <div class="rounded-2xl border border-violet-200 bg-violet-50/70 p-4 text-center">
                    <p class="text-xs font-bold text-violet-950">
                      ¿Aún no tienes cuenta de invitado?
                    </p>
                    <p class="mt-0.5 text-[11px] text-violet-800">
                      Regístrate solo con tu correo electrónico (cualquier correo) para poder guardar favoritos.
                    </p>
                    <a
                      [routerLink]="['/registro']"
                      [queryParams]="{ rol: 'Invitado' }"
                      class="mt-3 inline-flex w-full items-center justify-center gap-1.5 rounded-xl bg-[#73002b] px-4 py-2.5 text-xs font-bold uppercase tracking-wider text-white shadow-xs transition hover:bg-[#880f54]"
                    >
                      Crear cuenta de Invitado
                    </a>
                  </div>

                  <!-- Separador -->
                  <div class="relative flex items-center py-1">
                    <div class="flex-grow border-t border-slate-200"></div>
                    <span class="mx-3 shrink-0 text-[11px] font-bold uppercase tracking-wider text-slate-400">o bien</span>
                    <div class="flex-grow border-t border-slate-200"></div>
                  </div>

                  <!-- Opción 3: Entrar sin iniciar sesión (sin poder guardar favoritos) -->
                  <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-center">
                    <button
                      type="button"
                      (click)="entrarSinIniciarSesion()"
                      [disabled]="enviando()"
                      class="w-full rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-xs font-extrabold uppercase tracking-wider text-slate-700 shadow-xs transition hover:bg-slate-100 cursor-pointer"
                    >
                      Entrar sin iniciar sesión
                    </button>
                    <p class="mt-2 text-[11px] text-amber-800 font-medium">
                      Nota: Si entras sin iniciar sesión podrás consultar el repositorio, pero <strong>no podrás guardar tesis en favoritos</strong>.
                    </p>
                  </div>
                </div>
              } @else {
                <form [formGroup]="formulario" (ngSubmit)="entrar()" class="space-y-4">
                  <div>
                    <label for="correo" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">Correo o Usuario Institucional</label>
                    <input
                      id="correo"
                      type="email"
                      formControlName="correo"
                      autocomplete="username"
                      placeholder="usuario@upt.edu.mx"
                      class="campo !rounded-xl"
                    />
                  </div>

                  <div>
                    <label for="contrasena" class="campo-label text-xs font-bold uppercase tracking-wide text-slate-700">Contraseña</label>
                    <div class="relative">
                      <input
                        id="contrasena"
                        [type]="mostrarContrasena() ? 'text' : 'password'"
                        formControlName="contrasena"
                        autocomplete="current-password"
                        placeholder="••••••••"
                        class="campo !rounded-xl pr-14"
                      />
                      <button
                        type="button"
                        (click)="mostrarContrasena.update((v) => !v)"
                        class="absolute inset-y-0 right-0 flex w-12 items-center justify-center text-slate-400 hover:text-slate-600"
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
                  </div>

                  <button
                    type="submit"
                    [disabled]="formulario.invalid || enviando()"
                    class="w-full rounded-xl bg-[#73002b] py-3 text-sm font-bold uppercase tracking-wider text-white shadow-md hover:bg-[#880f54] active:scale-[0.99] transition cursor-pointer disabled:cursor-not-allowed disabled:opacity-60 flex items-center justify-center gap-2"
                  >
                    @if (enviando()) {
                      <svg width="16" height="16" class="h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                      </svg>
                      Verificando credenciales…
                    } @else {
                      Ingresar al Sistema
                    }
                  </button>
                </form>

                <!-- Opciones exclusivas dentro de la sección de Alumnos (Estudiante) -->
                @if (areaSeleccionada() === 'estudiante') {
                  <div class="mt-6 rounded-2xl border border-violet-100 bg-violet-50/70 p-4 text-center">
                    <p class="text-xs font-bold uppercase tracking-wide text-violet-950">
                      ¿Aún no tienes cuenta? Regístrate como:
                    </p>
                    <div class="mt-3 grid grid-cols-2 gap-2.5">
                      <a
                        [routerLink]="['/registro']"
                        [queryParams]="{ rol: 'Alumno' }"
                        class="flex items-center justify-center gap-1.5 rounded-xl bg-[#73002b] px-3 py-2.5 text-xs font-bold uppercase tracking-wide text-white shadow-xs transition hover:bg-[#880f54]"
                      >
                        Alumno
                      </a>
                      <a
                        [routerLink]="['/registro']"
                        [queryParams]="{ rol: 'Invitado' }"
                        class="flex items-center justify-center gap-1.5 rounded-xl border border-violet-300 bg-white px-3 py-2.5 text-xs font-bold uppercase tracking-wide text-violet-800 shadow-xs transition hover:bg-violet-100"
                      >
                        Invitado
                      </a>
                    </div>
                  </div>
                }

                @if (areaSeleccionada() === 'docente') {
                  <div class="mt-5 rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-center text-[11px] text-slate-600">
                    ¿Eres docente de nuevo ingreso? Solicita al <strong>Administrador</strong> que te envíe una invitación de registro a tu correo institucional.
                  </div>
                }

                <div class="mt-5 space-y-2 text-center text-xs">
                  <div>
                    <a routerLink="/recuperar-contrasena" class="font-semibold text-[#73002b] hover:underline">
                      ¿Olvidaste tu contraseña?
                    </a>
                  </div>
                  @if (areaSeleccionada() === 'estudiante') {
                    <div class="text-slate-500">
                      ¿No te llegó el correo de activación?
                      <a
                        [routerLink]="['/validar-cuenta']"
                        [queryParams]="formulario.value.correo ? { correo: formulario.value.correo } : null"
                        class="font-bold text-[#73002b] hover:underline ml-1"
                      >
                        Reenviar correo de validación
                      </a>
                    </div>
                  }
                </div>
              }
            </div>
          </div>
        }
      </main>
    </div>
  `,
})
export class LoginPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ruta = inject(ActivatedRoute);

  protected readonly areas = AREAS;
  protected readonly paso = signal<PasoLogin>(
    this.areaInicial() === null ? 'area' : 'formulario',
  );
  protected readonly areaSeleccionada = signal<AreaAcceso>(this.areaInicial() ?? 'docente');
  protected readonly areaActual = computed(
    () => AREAS.find((a) => a.valor === this.areaSeleccionada()) ?? AREAS[0],
  );

  protected readonly enviando = signal(false);
  protected readonly reenviandoValidacion = signal(false);
  protected readonly reenvioValidacionExitoso = signal(false);
  protected readonly error = signal('');
  protected readonly errorEsCuentaNoValidada = signal(false);
  protected readonly areaSugerida = signal<AreaAcceso | null>(null);
  protected readonly nombreAreaSugerida = computed(() => {
    const sug = this.areaSugerida();
    if (!sug) return '';
    return AREAS.find((a) => a.valor === sug)?.titulo ?? sug;
  });
  protected readonly exito = signal(false);
  protected readonly mostrarContrasena = signal(false);

  protected readonly formulario = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
    contrasena: ['', Validators.required],
  });

  protected readonly formularioInvitado = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
  });

  ngOnInit(): void {
    if (this.auth.estaAutenticado()) {
      this.auth.cerrarSesion();
    }

    this.ruta.queryParamMap.subscribe((params) => {
      if (params.get('inactividad') === 'true') {
        this.error.set('Tu sesión se ha cerrado automáticamente por inactividad (10 minutos).');
      }

      const consulta = params.get('area');
      if (
        consulta === 'estudiante' ||
        consulta === 'docente' ||
        consulta === 'institucion' ||
        consulta === 'invitado'
      ) {
        this.areaSeleccionada.set(consulta);
        this.paso.set('formulario');
        if (params.get('inactividad') !== 'true') {
          this.error.set('');
        }
        this.areaSugerida.set(null);
      }
    });
  }

  private areaInicial(): AreaAcceso | null {
    const consulta = this.ruta.snapshot.queryParamMap.get('area');
    return consulta === 'estudiante' ||
      consulta === 'docente' ||
      consulta === 'institucion' ||
      consulta === 'invitado'
      ? consulta
      : null;
  }

  protected elegirArea(area: AreaAcceso): void {
    this.areaSeleccionada.set(area);
    this.error.set('');
    this.areaSugerida.set(null);
    this.paso.set('formulario');
  }

  protected volverASeleccionArea(): void {
    this.paso.set('area');
    this.error.set('');
    this.areaSugerida.set(null);
  }

  protected regresar(): void {
    if (this.paso() === 'formulario') {
      this.volverASeleccionArea();
    } else {
      void this.router.navigate(['/inicio']);
    }
  }

  protected cambiarAAreaSugerida(): void {
    const sug = this.areaSugerida();
    if (sug) {
      this.areaSeleccionada.set(sug);
      this.areaSugerida.set(null);
      this.error.set('');
    }
  }

  iniciarSesionInvitado(): void {
    if (this.formularioInvitado.invalid) {
      return;
    }

    const correo = this.formularioInvitado.value.correo!.trim();
    this.enviando.set(true);
    this.error.set('');
    this.areaSugerida.set(null);

    this.auth.loginInvitado(correo).subscribe({
      next: (respuesta) => {
        this.auth.guardarSesion(respuesta);
        this.enviando.set(false);
        this.exito.set(true);
        setTimeout(() => void this.router.navigate(['/repositorio']), 800);
      },
      error: (err: ApiError) => {
        this.enviando.set(false);
        this.exito.set(false);
        this.error.set(
          err.message || 'No encontramos una cuenta de invitado con ese correo. Puedes crear tu cuenta abajo.',
        );
      },
    });
  }

  entrarSinIniciarSesion(): void {
    this.enviando.set(true);
    this.error.set('');
    this.areaSugerida.set(null);

    this.auth.loginInvitado().subscribe({
      next: (respuesta) => {
        this.auth.guardarSesion(respuesta);
        this.enviando.set(false);
        this.exito.set(true);
        setTimeout(() => void this.router.navigate(['/repositorio']), 800);
      },
      error: (err: ApiError) => {
        this.enviando.set(false);
        this.exito.set(false);
        this.error.set(err.message || 'No fue posible entrar como invitado.');
      },
    });
  }

  reenviarValidacionDesdeLogin(): void {
    const correo = this.formulario.value.correo?.trim();
    if (!correo) return;

    this.reenviandoValidacion.set(true);
    this.auth.reenviarConfirmacion({ correo }).subscribe({
      next: () => {
        this.reenviandoValidacion.set(false);
        this.reenvioValidacionExitoso.set(true);
      },
      error: () => {
        this.reenviandoValidacion.set(false);
        this.reenvioValidacionExitoso.set(true);
      },
    });
  }

  entrar(): void {
    if (this.formulario.invalid) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');
    this.areaSugerida.set(null);
    this.errorEsCuentaNoValidada.set(false);
    this.reenvioValidacionExitoso.set(false);

    const areaActual = this.areaSeleccionada();

    this.auth
      .login({
        correo: this.formulario.value.correo!,
        contrasena: this.formulario.value.contrasena!,
        rolEsperado: areaActual,
      })
      .subscribe({
        next: (respuesta) => {
          const esAlumno = respuesta.roles.includes(Rol.Alumno);
          const esDocente = respuesta.roles.includes(Rol.Docente);
          const esPersonal = respuesta.roles.some((r) => ROLES_PERSONAL.includes(r as RolValue));

          let coincide = false;
          if (areaActual === 'estudiante' && esAlumno) coincide = true;
          else if (areaActual === 'docente' && esDocente) coincide = true;
          else if (areaActual === 'institucion' && esPersonal) coincide = true;

          if (!coincide) {
            this.enviando.set(false);
            this.exito.set(false);
            if (esDocente) {
              this.error.set('Esta cuenta pertenece al perfil de Docente. Por favor ingresa desde la sección de Docentes.');
              this.areaSugerida.set('docente');
            } else if (esAlumno) {
              this.error.set('Esta cuenta pertenece al perfil de Estudiante. Por favor ingresa desde la sección de Estudiantes.');
              this.areaSugerida.set('estudiante');
            } else if (esPersonal) {
              this.error.set('Esta cuenta pertenece al personal administrativo. Por favor ingresa desde la sección de Administrativo.');
              this.areaSugerida.set('institucion');
            } else {
              this.error.set('Tu cuenta no tiene permisos para ingresar en esta sección.');
            }
            return;
          }

          this.auth.guardarSesion(respuesta);
          this.enviando.set(false);
          this.exito.set(true);
          const rutaDestino = esAlumno
            ? '/mis-tesis'
            : esDocente
              ? '/asesorias'
              : respuesta.roles.includes(Rol.Administrador)
                ? '/administracion'
                : '/tesis';
          setTimeout(() => void this.router.navigate([rutaDestino]), 950);
        },
        error: (err: ApiError) => {
          this.enviando.set(false);
          this.exito.set(false);

          if (err.code === 'CUENTA_NO_VALIDADA') {
            this.error.set('Tu cuenta aún no ha sido activada. Por favor confirma tu correo institucional.');
            this.errorEsCuentaNoValidada.set(true);
            this.areaSugerida.set(null);
          } else if (err.code === 'ROL_NO_CORRESPONDE') {
            this.error.set(err.message || 'Esta cuenta no pertenece a esta sección.');
            this.errorEsCuentaNoValidada.set(false);
            if (err.message?.includes('Docente')) {
              this.areaSugerida.set('docente');
            } else if (err.message?.includes('Alumno') || err.message?.includes('Estudiante')) {
              this.areaSugerida.set('estudiante');
            } else if (err.message?.includes('Director') || err.message?.includes('Administrador')) {
              this.areaSugerida.set('institucion');
            }
          } else if (err.code === 'CREDENCIALES_INVALIDAS') {
            this.error.set('Correo o contraseña incorrectos. Verifica tus credenciales e intenta de nuevo.');
            this.errorEsCuentaNoValidada.set(false);
            this.areaSugerida.set(null);
          } else {
            this.error.set(err.message || 'No fue posible iniciar sesión.');
            this.errorEsCuentaNoValidada.set(false);
            this.areaSugerida.set(null);
          }
        },
      });
  }
}
