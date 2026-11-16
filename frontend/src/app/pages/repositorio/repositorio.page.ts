import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { RepositorioService } from '../../core/services/repositorio.service';
import { VisorPdfService } from '../../core/services/visor-pdf.service';
import { NivelPosgrado } from '../../core/models/tesis.model';
import { RepositorioEntrada } from '../../core/models/repositorio.model';
import { primera } from '../../core/util/async.util';

const TAMANO = 20;

@Component({
  selector: 'app-repositorio',
  imports: [DatePipe],
  templateUrl: './repositorio.page.html',
})
export class RepositorioPage {
  private readonly repositorio = inject(RepositorioService);
  private readonly visorPdf = inject(VisorPdfService);
  protected readonly auth = inject(AuthService);

  protected readonly cargando = signal(true);
  protected readonly error = signal('');

  protected readonly termino = signal('');
  protected readonly nivel = signal<NivelPosgrado | ''>('');
  protected readonly programa = signal('');
  protected readonly pagina = signal(1);
  protected readonly tamano = TAMANO;

  protected readonly total = signal(0);
  protected readonly resultados = signal<RepositorioEntrada[]>([]);
  protected readonly favoritosFolios = signal<Set<string>>(new Set());
  protected readonly abriendoPdf = this.visorPdf.folioActivo;

  constructor() {
    void this.buscar();
    void this.cargarFavoritos();
  }

  protected abrirPdf(folio: string, titulo?: string): void {
    void this.visorPdf.abrirConPeticion(
      titulo ?? 'Tesis de Posgrado',
      () => this.repositorio.descargarDocumento(folio),
      `Folio: ${folio}`,
      folio,
    );
  }

  protected get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.total() / this.tamano));
  }

  protected cambiarTermino(valor: string): void {
    this.termino.set(valor);
    this.irAPagina(1);
    void this.buscar();
  }

  protected cambiarNivel(valor: string): void {
    this.nivel.set(valor as NivelPosgrado | '');
    this.irAPagina(1);
    void this.buscar();
  }

  protected cambiarPrograma(valor: string): void {
    this.programa.set(valor);
    this.irAPagina(1);
    void this.buscar();
  }

  protected limpiarFiltros(): void {
    this.termino.set('');
    this.nivel.set('');
    this.programa.set('');
    this.irAPagina(1);
    void this.buscar();
  }

  protected anterior(): void {
    if (this.pagina() > 1) {
      this.irAPagina(this.pagina() - 1);
      void this.buscar();
    }
  }

  protected siguiente(): void {
    if (this.pagina() < this.totalPaginas) {
      this.irAPagina(this.pagina() + 1);
      void this.buscar();
    }
  }

  protected async buscar(): Promise<void> {
    this.cargando.set(true);
    this.error.set('');

    const resultado = await primera(
      this.repositorio.consultar({
        termino: this.termino() || undefined,
        nivel: this.nivel() || undefined,
        programa: this.programa() || undefined,
        pagina: this.pagina(),
        tamano: this.tamano,
      }),
    );

    if (resultado) {
      this.resultados.set(resultado.data ?? []);
      this.total.set(resultado.total);
    } else {
      this.resultados.set([]);
      this.total.set(0);
    }

    this.cargando.set(false);
  }

  protected esFavorito(folio: string): boolean {
    return this.favoritosFolios().has(folio);
  }

  protected async alternarFavorito(tesis: RepositorioEntrada): Promise<void> {
    if (!this.auth.puedeGuardarFavoritos()) {
      return;
    }

    const folio = tesis.folio;
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

  private async cargarFavoritos(): Promise<void> {
    const idUsuario = this.auth.idUsuario();
    if (idUsuario === null || !this.auth.puedeGuardarFavoritos()) {
      return;
    }

    const favoritos = await primera(this.repositorio.misFavoritos(idUsuario));
    this.favoritosFolios.set(new Set((favoritos ?? []).map((f) => f.folio)));
  }

  private irAPagina(nueva: number): void {
    this.pagina.set(Math.max(1, nueva));
  }
}