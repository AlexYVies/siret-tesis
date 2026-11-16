import { inject, Injectable, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Observable } from 'rxjs';
import { primera } from '../util/async.util';

@Injectable({
  providedIn: 'root',
})
export class VisorPdfService {
  private readonly sanitizer = inject(DomSanitizer);

  readonly visible = signal(false);
  readonly titulo = signal('');
  readonly subtitulo = signal('');
  readonly cargando = signal(false);
  readonly error = signal('');
  readonly rawUrl = signal<string | null>(null);
  readonly urlSegura = signal<SafeResourceUrl | null>(null);
  readonly folioActivo = signal<string | null>(null);

  /**
   * Abre el modal y carga el documento de forma asíncrona mediante un Observable o Promesa.
   */
  async abrirConPeticion(
    titulo: string,
    obtenerBlob: () => Observable<Blob> | Promise<Blob>,
    subtitulo?: string,
    folio?: string,
  ): Promise<void> {
    this.limpiarUrlActiva();

    this.folioActivo.set(folio ?? null);
    this.titulo.set(titulo);
    this.subtitulo.set(subtitulo ?? '');
    this.cargando.set(true);
    this.error.set('');
    this.visible.set(true);

    try {
      const fuente = obtenerBlob();
      const blob = 'subscribe' in fuente ? await primera(fuente) : await fuente;

      if (blob && blob.size > 0) {
        const pdfBlob = blob.type === 'application/pdf' ? blob : new Blob([blob], { type: 'application/pdf' });
        const raw = URL.createObjectURL(pdfBlob);
        this.rawUrl.set(raw);
        this.urlSegura.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
      } else {
        this.error.set('No se pudo cargar el documento PDF o el archivo está vacío.');
      }
    } catch {
      this.error.set('Esta tesis aún no tiene documento PDF disponible para lectura.');
    } finally {
      this.cargando.set(false);
    }
  }

  /**
   * Abre el modal directamente con un Blob que ya se tiene en memoria.
   */
  abrirConBlob(titulo: string, blob: Blob, subtitulo?: string): void {
    this.limpiarUrlActiva();

    this.titulo.set(titulo);
    this.subtitulo.set(subtitulo ?? '');
    this.cargando.set(false);
    this.error.set('');
    this.visible.set(true);

    if (blob && blob.size > 0) {
      const pdfBlob = blob.type === 'application/pdf' ? blob : new Blob([blob], { type: 'application/pdf' });
      const raw = URL.createObjectURL(pdfBlob);
      this.rawUrl.set(raw);
      this.urlSegura.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
    } else {
      this.error.set('El documento no contiene datos legibles.');
    }
  }

  /**
   * Abre el documento PDF en una pestaña nueva del navegador.
   */
  abrirEnNuevaPestana(): void {
    const raw = this.rawUrl();
    if (raw) {
      window.open(raw, '_blank');
    }
  }

  /**
   * Cierra el modal y libera el ObjectURL para evitar fugas de memoria.
   */
  cerrar(): void {
    this.limpiarUrlActiva();
    this.folioActivo.set(null);
    this.cargando.set(false);
    this.error.set('');
    this.visible.set(false);
  }

  private limpiarUrlActiva(): void {
    const raw = this.rawUrl();
    if (raw) {
      URL.revokeObjectURL(raw);
    }
    this.rawUrl.set(null);
    this.urlSegura.set(null);
  }
}
