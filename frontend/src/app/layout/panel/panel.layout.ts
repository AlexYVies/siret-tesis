import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { Rol, ROLES_ADMINISTRACION, ROLES_PERSONAL } from '../../core/models/constantes.model';
import { TokenStorageService } from '../../core/services/token-storage.service';

interface ItemNavigation {
  ruta: string;
  etiqueta: string;
  icono: string;
  roles?: string[];
}

const ICONO_CUENTA = 'M5.121 17.804A13.937 13.937 0 0112 16c2.5 0 4.847.655 6.879 1.804M15 10a3 3 0 11-6 0 3 3 0 016 0zm6 2a9 9 0 11-18 0 9 9 0 0118 0z';

@Component({
  selector: 'app-panel',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './panel.layout.html',
})
export class PanelLayout implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tokens = inject(TokenStorageService);
  protected readonly menuAbierto = signal(false);
  protected readonly apareciendo = signal(true);

  ngOnInit(): void {
    if (this.auth.estaAutenticado() && !this.auth.esInvitadoSinSesion()) {
      this.auth.obtenerMiPerfil().subscribe({
        next: (perfil) => this.auth.actualizarDatosPerfil(perfil),
        error: () => {},
      });
    }
  }

  protected reiniciarAnimacion(): void {
    this.apareciendo.set(false);
    setTimeout(() => this.apareciendo.set(true), 20);
  }

  protected readonly menu = computed<ItemNavigation[]>(() => {
    if (this.auth.tieneRol(Rol.Alumno)) {
      return [
        {
          ruta: '/mis-tesis',
          etiqueta: 'Mis tesis',
          icono: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
        },
        {
          ruta: '/repositorio',
          etiqueta: 'Repositorio',
          icono: 'M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25',
        },
        {
          ruta: '/tesis/registrar',
          etiqueta: 'Registrar tesis',
          icono: 'M12 4v16m8-8H4',
        },
        {
          ruta: '/favoritos',
          etiqueta: 'Mis favoritos',
          icono: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.196-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
        },
        {
          ruta: '/cuenta',
          etiqueta: 'Mi cuenta',
          icono: ICONO_CUENTA,
        },
      ];
    }

    if (this.auth.tieneRol(Rol.Docente)) {
      return [
        {
          ruta: '/asesorias',
          etiqueta: 'Tesis que asesoro',
          icono: 'M12 14l9-5-9-5-9 5 9 5z M12 14l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14z',
        },
        {
          ruta: '/repositorio',
          etiqueta: 'Repositorio',
          icono: 'M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25',
        },
        {
          ruta: '/favoritos',
          etiqueta: 'Mis favoritos',
          icono: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.196-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
        },
        {
          ruta: '/cuenta',
          etiqueta: 'Mi cuenta',
          icono: ICONO_CUENTA,
        },
      ];
    }

    if (this.auth.tieneRol(Rol.Invitado)) {
      const items: ItemNavigation[] = [
        {
          ruta: '/repositorio',
          etiqueta: 'Repositorio',
          icono: 'M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25',
        },
      ];
      if (this.auth.puedeGuardarFavoritos()) {
        items.push({
          ruta: '/favoritos',
          etiqueta: 'Mis favoritos',
          icono: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.196-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
        });
      }
      if (!this.auth.esInvitadoSinSesion()) {
        items.push({
          ruta: '/cuenta',
          etiqueta: 'Mi cuenta',
          icono: ICONO_CUENTA,
        });
      }
      return items;
    }

    // Personal institucional (Director, Administrador, TI, Asistente)
    const itemsPersonal: ItemNavigation[] = [
      {
        ruta: '/tesis',
        etiqueta: 'Todas las tesis',
        icono: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
        roles: [Rol.Director, Rol.AdministradorTI],
      },
      {
        ruta: '/repositorio',
        etiqueta: 'Repositorio',
        icono: 'M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25',
      },
      {
        ruta: '/favoritos',
        etiqueta: 'Mis favoritos',
        icono: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.196-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
      },
      {
        ruta: '/administracion',
        etiqueta: 'Administración',
        icono: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z',
        roles: ROLES_ADMINISTRACION,
      },
      {
        ruta: '/indicadores',
        etiqueta: 'Indicadores',
        icono: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
        roles: [Rol.Director],
      },
      {
        ruta: '/cuenta',
        etiqueta: 'Mi cuenta',
        icono: ICONO_CUENTA,
      },
    ];

    return itemsPersonal.filter((it) => !it.roles || it.roles.some((r) => this.auth.tieneRol(r)));
  });

  protected alternarMenu(): void {
    this.menuAbierto.update((valor) => !valor);
  }

  protected salir(): void {
    this.tokens.eliminarToken();
    this.auth.cerrarSesion();
    void this.router.navigate(['/inicio']);
  }
}