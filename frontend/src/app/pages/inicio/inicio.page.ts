import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { RepositorioService } from '../../core/services/repositorio.service';
import { TesisService } from '../../core/services/tesis.service';
import { Indicadores } from '../../core/models/repositorio.model';
import { TesisListado } from '../../core/models/tesis.model';
import { Rol, ROLES_PERSONAL } from '../../core/models/constantes.model';
import { clasesEstado } from '../../core/util/estado.util';
import { primera } from '../../core/util/async.util';

interface AreaLanding {
  valor: string;
  titulo: string;
  descripcion: string;
  accion: string;
  ruta: string;
  icono: string;
}

@Component({
  selector: 'app-inicio',
  imports: [RouterLink],
  templateUrl: './inicio.page.html',
  host: {
    class: 'flex-1 flex flex-col',
  },
})
export class InicioPage {
  private readonly repositorio = inject(RepositorioService);
  private readonly tesis = inject(TesisService);

  protected readonly auth = inject(AuthService);

  protected readonly rol = Rol;
  protected readonly cargando = signal(true);
  protected readonly error = signal('');
  protected readonly indicadores = signal<Indicadores | null>(null);
  protected readonly recientes = signal<TesisListado[]>([]);

  protected readonly areas: AreaLanding[] = [
    {
      valor: 'estudiante',
      titulo: 'Estudiantes',
      descripcion: 'Registra tu tesis, carga documentos y sigue tu trámite.',
      accion: 'Crear cuenta',
      ruta: '/registro',
      icono: 'M15 9a3 3 0 11-6 0 3 3 0 016 0z M12 21a9 9 0 100-18 9 9 0 000 18z',
    },
    {
      valor: 'docente',
      titulo: 'Docentes y asesores',
      descripcion: 'Confirma asesorías, revisa versiones y emite dictámenes.',
      accion: 'Entrar al portal',
      ruta: '/login?area=docente',
      icono: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253',
    },
    {
      valor: 'institucion',
      titulo: 'Dirección y administración',
      descripcion: 'Catálogos, usuarios e indicadores de producción académica.',
      accion: 'Entrar al portal',
      ruta: '/login?area=institucion',
      icono: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
    },
  ];

  constructor() {
    if (this.auth.estaAutenticado()) {
      this.auth.cerrarSesion();
    }
    this.cargando.set(false);
  }

  protected correo(): string {
    return this.auth.correo();
  }

  protected esAlumno(): boolean {
    return this.auth.tieneRol(Rol.Alumno);
  }

  protected esDocente(): boolean {
    return this.auth.tieneRol(Rol.Docente);
  }

  protected esPersonal(): boolean {
    return ROLES_PERSONAL.some((rol) => this.auth.tieneRol(rol));
  }

  protected esAdministrador(): boolean {
    return this.auth.tieneRol(Rol.Administrador);
  }

  protected puedeAdministrar(): boolean {
    return this.auth.tieneRol(Rol.Administrador) || this.auth.tieneRol(Rol.AdministradorTI);
  }

  protected esDirector(): boolean {
    return this.auth.tieneRol(Rol.Director);
  }

  protected estadoClases(estado: string): string {
    return clasesEstado(estado as never);
  }

  private async cargar(): Promise<void> {
    this.cargando.set(true);
    this.error.set('');

    const [indicadores, recientes] = await Promise.all([
      this.esDirector() ? primera(this.repositorio.indicadores()) : Promise.resolve(null),
      !this.esAdministrador() ? primera(this.tesis.listar()) : Promise.resolve([]),
    ]);

    this.indicadores.set(indicadores);
    this.recientes.set(recientes ?? []);
    this.cargando.set(false);
  }
}