import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { RepositorioService } from '../../core/services/repositorio.service';
import { VisorPdfService } from '../../core/services/visor-pdf.service';
import { RepositorioEntrada } from '../../core/models/repositorio.model';
import { primera } from '../../core/util/async.util';

@Component({
  selector: 'app-favoritos',
  imports: [DatePipe],
  templateUrl: './favoritos.page.html',
})
export class FavoritosPage {
  private readonly auth = inject(AuthService);
  private readonly repositorio = inject(RepositorioService);
  private readonly visorPdf = inject(VisorPdfService);

  protected readonly cargando = signal(true);
  protected readonly error = signal('');
  protected readonly favoritos = signal<RepositorioEntrada[]>([]);
  protected readonly quitarAdiccion = signal('');
  protected readonly quitando = signal(false);
  protected readonly abriendoPdf = this.visorPdf.folioActivo;

  constructor() {
    void this.cargar();
  }

  protected abrirPdf(folio: string, titulo?: string): void {
    void this.visorPdf.abrirConPeticion(
      titulo ?? 'Tesis de Posgrado',
      () => this.repositorio.descargarDocumento(folio),
      `Folio: ${folio}`,
      folio,
    );
  }

  protected async cargar(): Promise<void> {
    this.cargando.set(true);
    this.error.set('');

    const idUsuario = this.auth.idUsuario();
    if (idUsuario === null) {
      this.cargando.set(false);
      return;
    }

    const lista = await primera(this.repositorio.misFavoritos(idUsuario));
    if (lista) {
      this.favoritos.set(lista);
    }

    this.cargando.set(false);
  }

  protected async quitar(folio: string): Promise<void> {
    this.quitando.set(true);
    this.quitarAdiccion.set(folio);

    const confirmado = await primera(this.repositorio.eliminarFavorito(folio));
    this.quitando.set(false);
    this.quitarAdiccion.set('');

    if (confirmado) {
      this.favoritos.set(this.favoritos().filter((t) => t.folio !== folio));
    }
  }
}