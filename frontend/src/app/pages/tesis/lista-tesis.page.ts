import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { Rol } from '../../core/models/constantes.model';
import { TesisService } from '../../core/services/tesis.service';
import { RepositorioService } from '../../core/services/repositorio.service';
import { VisorPdfService } from '../../core/services/visor-pdf.service';
import { TesisListado } from '../../core/models/tesis.model';
import { clasesEstado } from '../../core/util/estado.util';
import { primera } from '../../core/util/async.util';

@Component({
  selector: 'app-lista-tesis',
  imports: [RouterLink],
  templateUrl: './lista-tesis.page.html',
})
export class ListaTesisPage {
  private readonly tesis = inject(TesisService);
  private readonly repositorio = inject(RepositorioService);
  private readonly visorPdf = inject(VisorPdfService);
  protected readonly auth = inject(AuthService);
  protected readonly Rol = Rol;

  protected readonly titulo = computed(() => {
    if (this.auth.tieneRol(Rol.Alumno)) return 'Mis tesis';
    if (this.auth.tieneRol(Rol.Docente)) return 'Tesis que asesoro';
    if (this.auth.tieneRol(Rol.Invitado)) return 'Repositorio de Tesis Aprobadas';
    return 'Todas las tesis';
  });

  protected readonly subtitulo = computed(() => {
    if (this.auth.tieneRol(Rol.Alumno)) {
      return 'Consulta el desarrollo, avance, dictámenes y versiones de tus proyectos de tesis.';
    }
    if (this.auth.tieneRol(Rol.Docente)) {
      return 'Proyectos de posgrado donde participas como asesor o co-asesor para dictamen y revisión.';
    }
    if (this.auth.tieneRol(Rol.Invitado)) {
      return 'Modo Invitado: Explorando las tesis institucionales que han completado su dictamen y han sido aceptadas.';
    }
    return 'Listado institucional y seguimiento general de proyectos de posgrado.';
  });

  protected readonly tieneTesisEnProceso = computed(() => {
    return (
      this.auth.tieneRol(Rol.Alumno) &&
      this.todos().some((t) => !['Aceptada', 'Rechazada', 'Inactiva'].includes(t.estado))
    );
  });

  protected readonly cargando = signal(true);
  protected readonly error = signal('');
  protected readonly filtro = signal('');
  protected readonly listado = signal<TesisListado[]>([]);
  protected readonly todos = signal<TesisListado[]>([]);
  protected readonly favoritosFolios = signal<Set<string>>(new Set());
  protected readonly abriendoPdf = this.visorPdf.folioActivo;

  constructor() {
    void this.cargar();
    void this.cargarFavoritos();
  }

  protected abrirPdf(folio: string, titulo?: string): void {
    void this.visorPdf.abrirConPeticion(
      titulo ?? 'Tesis de Posgrado',
      () => this.tesis.descargarDocumento(folio),
      `Folio: ${folio}`,
      folio,
    );
  }

  protected estadoClases(estado: string): string {
    return clasesEstado(estado as never);
  }

  protected cambiarFiltro(valor: string): void {
    this.filtro.set(valor);
    const texto = valor.trim().toLowerCase();

    this.listado.set(
      texto ? this.todos().filter((t) => `${t.titulo} ${t.alumno} ${t.asesor} ${t.programa}`.toLowerCase().includes(texto)) : [...this.todos()],
    );
  }

  protected esFavorito(folio: string): boolean {
    return this.favoritosFolios().has(folio);
  }

  protected async alternarFavorito(folio: string): Promise<void> {
    if (!this.auth.puedeGuardarFavoritos()) {
      return;
    }

    const estabaFavorito = this.esFavorito(folio);
    const actualizar = (activo: boolean) => {
      const siguiente = new Set(this.favoritosFolios());
      if (activo) {
        siguiente.add(folio);
      } else {
        siguiente.delete(folio);
      }
      this.favoritosFolios.set(siguiente);
    };

    // Actualización visual inmediata (optimista)
    actualizar(!estabaFavorito);

    const confirmado = estabaFavorito
      ? await primera(this.repositorio.eliminarFavorito(folio))
      : await primera(this.repositorio.agregarFavorito(folio));

    if (!confirmado) {
      actualizar(estabaFavorito);
    }
  }

  private async cargar(): Promise<void> {
    this.cargando.set(true);
    this.error.set('');

    const listado = await primera(this.tesis.listar());

    this.todos.set(listado ?? []);
    this.listado.set(listado ?? []);
    this.cargando.set(false);
  }

  private async cargarFavoritos(): Promise<void> {
    const idUsuario = this.auth.idUsuario();
    if (idUsuario === null || !this.auth.puedeGuardarFavoritos()) {
      return;
    }

    const favoritos = await primera(this.repositorio.misFavoritos(idUsuario));
    this.favoritosFolios.set(new Set((favoritos ?? []).map((f) => f.folio)));
  }
}