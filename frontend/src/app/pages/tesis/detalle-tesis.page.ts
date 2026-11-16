import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { firstValueFrom, timeout } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { CatalogosService } from '../../core/services/catalogos.service';
import { RepositorioService } from '../../core/services/repositorio.service';
import { TesisService } from '../../core/services/tesis.service';
import { VisorPdfService } from '../../core/services/visor-pdf.service';
import { Rol } from '../../core/models/constantes.model';
import { TipoArticulo } from '../../core/models/catalogos.model';
import {
  AgregarArticuloRequest,
  ArticuloDto,
  EmitirDictamenRequest,
  EstadoConfirmacion,
  ProcesarConfirmacionRequest,
  ProcesarRevisionAsesorRequest,
  ReferenciaDetectada,
  RevisionAsesorDto,
  RevisionIADto,
  TesisDetalle,
  TipoConfirmacion,
} from '../../core/models/tesis.model';
import { clasesEstado, clasesResultado } from '../../core/util/estado.util';
import { primera } from '../../core/util/async.util';

const ESTADOS_MODIFICABLES = [
  'Registrada',
  'Confirmada',
  'En revisión IA',
  'En revisión del asesor',
  'Cambios solicitados',
];

interface ConfirmacionVisible {
  tipo: TipoConfirmacion;
  estado: EstadoConfirmacion;
  fecha: string | null;
}

@Component({
  selector: 'app-detalle-tesis',
  imports: [RouterLink, DatePipe],
  templateUrl: './detalle-tesis.page.html',
})
export class DetalleTesisPage implements OnDestroy {
  private readonly ruta = inject(ActivatedRoute);
  protected readonly auth = inject(AuthService);
  private readonly catalogos = inject(CatalogosService);
  private readonly repositorio = inject(RepositorioService);
  private readonly tesis = inject(TesisService);
  private readonly visorPdf = inject(VisorPdfService);
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly rol = Rol;
  protected readonly cargando = signal(true);
  protected readonly noExiste = signal(false);
  protected readonly error = signal('');
  protected readonly detalle = signal<TesisDetalle | null>(null);

  protected readonly tiposArticulo = signal<TipoArticulo[]>([]);
  protected readonly formularioArticuloAbierto = signal(false);
  protected readonly subiendo = signal(false);

  // Favoritos e Invitado
  protected readonly esInvitado = computed(() => this.auth.tieneRol(Rol.Invitado));
  protected readonly esFavorito = signal(false);

  // Visor PDF Inline para Invitados
  protected readonly cargandoPdf = signal(false);
  protected readonly errorPdf = signal('');
  protected readonly pdfUrlSeguro = signal<SafeResourceUrl | null>(null);
  protected readonly pdfRawUrl = signal<string | null>(null);

  // Estados para extracción de artículos y referencias
  protected readonly modalSugerenciasAbierto = signal(false);
  protected readonly cargandoSugerencias = signal(false);
  protected readonly importandoSugerencias = signal(false);
  protected readonly sugerenciasPdf = signal<ReferenciaDetectada[]>([]);
  protected readonly indicesSeleccionados = signal<Set<number>>(new Set());
  protected readonly mensajeExitoArticulo = signal<string>('');
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  protected readonly modalReporteAbierto = signal<any | null>(null);

  // Validación de artículos manuales
  protected readonly articuloNombreTocado = signal(false);
  protected readonly articuloUrlTocado = signal(false);
  protected readonly articuloDoiTocado = signal(false);
  protected readonly articuloTipoTocado = signal(false);

  protected readonly errorNombre = computed(() => {
    const nombre = this.nuevoArticulo().nombre.trim();
    if (!nombre) return 'El título formal del artículo es obligatorio.';
    if (nombre.length < 10) return `El título debe tener al menos 10 caracteres (actual: ${nombre.length}).`;
    if (/^(articulo|prueba|test|asdf|qwerty|1234|sin titulo|nada|aaa)/i.test(nombre)) {
      return 'Ingrese el título académico formal de la publicación, no use textos genéricos ni de prueba.';
    }
    return '';
  });

  protected readonly errorUrl = computed(() => {
    const url = this.nuevoArticulo().url.trim();
    if (!url) return 'La URL de la publicación o repositorio es obligatoria.';
    if (!/^https?:\/\/.+/i.test(url)) {
      return 'La URL debe ser un enlace web completo que comience con https:// o http:// (ej. https://doi.org/...).';
    }
    if (/^(https?:\/\/)?(www\.)?(google|facebook|youtube|instagram|twitter|tiktok)\./i.test(url)) {
      return 'La URL debe apuntar al artículo académico o repositorio científico, no a buscadores ni redes sociales.';
    }
    return '';
  });

  protected readonly errorDoi = computed(() => {
    const doi = this.nuevoArticulo().doi?.trim();
    if (!doi) return '';
    if (!/^10\.\d{4,9}\/[-._;()/:A-Za-z0-9]+$/i.test(doi)) {
      return 'El formato del DOI es incorrecto. Debe iniciar con "10." seguido del prefijo editorial y sufijo (ej. 10.1016/j.optom.2023.01.005).';
    }
    return '';
  });

  protected readonly errorTipo = computed(() => {
    if (!this.nuevoArticulo().idTipoArticulo) {
      return 'Debe seleccionar el tipo de publicación académica para clasificar el artículo.';
    }
    return '';
  });

  protected readonly formularioArticuloValido = computed(() => {
    return !this.errorNombre() && !this.errorUrl() && !this.errorDoi() && !this.errorTipo();
  });

  // Filtros de referencias IA (Sospechosas / Validadas)
  protected readonly referenciasDudosas = computed(() => {
    return this.sugerenciasPdf().filter((s) => s.esSospechosa && !s.yaRegistrado);
  });

  protected readonly referenciasVerificadas = computed(() => {
    return this.sugerenciasPdf().filter((s) => !s.esSospechosa);
  });

  protected readonly nuevoArticulo = signal<AgregarArticuloRequest>({ nombre: '', url: '', doi: null, idTipoArticulo: null });
  protected readonly esUltimaVersionIA = signal(true);
  protected readonly esUltimaVersionAsesor = signal(true);
  protected readonly decisionAsesor = signal<'Aceptada' | 'Rechazada para cambios'>('Aceptada');
  protected readonly observacionesAsesor = signal('');
  protected readonly resultadoDictamen = signal<'Aceptada' | 'Rechazada'>('Aceptada');
  protected readonly observacionesDictamen = signal('');

  protected readonly folio = signal('');
  protected readonly archivoSeleccionado = signal<File | null>(null);
  protected readonly idProfesorActual = signal<number | null>(null);

  constructor() {
    this.folio.set(this.ruta.snapshot.params['folio']);
    void this.cargarDetalle();
    void this.cargarPerfilDocente();
    void this.cargarFavorito();
    void this.cargarPdfInline();
  }

  ngOnDestroy(): void {
    const raw = this.pdfRawUrl();
    if (raw) {
      URL.revokeObjectURL(raw);
    }
  }

  protected estadoClases(estado: string): string {
    return clasesEstado(estado as never);
  }

  protected resultadoClases(resultado: string): string {
    return clasesResultado(resultado);
  }

  protected esAlumno(): boolean {
    return this.auth.tieneRol(Rol.Alumno);
  }

  protected esDocente(): boolean {
    return this.auth.tieneRol(Rol.Docente);
  }

  protected esPersonal(): boolean {
    return this.auth.tieneRol(Rol.Director) || this.auth.tieneRol(Rol.Administrador) || this.auth.tieneRol(Rol.AdministradorTI);
  }

  protected permiteDocumento(): boolean {
    return this.esAlumno() && ESTADOS_MODIFICABLES.includes(this.estado());
  }

  protected confirmacionesVisibles(): ConfirmacionVisible[] {
    const detalle = this.detalle();
    if (!detalle) {
      return [];
    }

    const tipos: TipoConfirmacion[] = detalle.tieneCoAsesor ? ['Asesor', 'Co-asesor'] : ['Asesor'];

    return tipos.map((tipo) => {
      const existente = detalle.confirmaciones.find((c) => c.tipo === tipo);
      const fecha = existente?.fecha;
      return {
        tipo,
        estado: existente?.estado ?? 'Pendiente',
        fecha: fecha && !fecha.startsWith('0001-01-01') ? fecha : null,
      } satisfies ConfirmacionVisible;
    });
  }

  protected miTipoConfirmacion(): TipoConfirmacion | null {
    const detalle = this.detalle();
    const idProfesor = this.idProfesorActual();
    if (!detalle || idProfesor === null) {
      return null;
    }
    if (detalle.idAsesor === idProfesor) {
      return 'Asesor';
    }
    if (detalle.idCoAsesor === idProfesor) {
      return 'Co-asesor';
    }
    return null;
  }

  protected permiteConfirmar(tipo: TipoConfirmacion): boolean {
    return this.esDocente() && this.estado() === 'Registrada' && this.miTipoConfirmacion() === tipo;
  }

  protected permiteArticulo(): boolean {
    const tieneRol = this.esAlumno() || this.esDocente() || this.esPersonal();
    return tieneRol && this.estado() !== 'Registrada' && !['Inactiva', 'Rechazada'].includes(this.estado());
  }

  protected permiteRevisionIA(): boolean {
    const esAsesor = this.esDocente() && this.idProfesorActual() === this.detalle()?.idAsesor;
    const tieneReporteActual = this.detalle()?.revisionesIA?.some((r) => r.version === this.detalle()?.version && !!r.reporteJson);
    return esAsesor && (this.estado() === 'En revisión IA' || (this.estado() === 'En revisión del asesor' && !tieneReporteActual));
  }

  protected permiteRevisionAsesor(): boolean {
    return this.esDocente() && this.estado() === 'En revisión del asesor' && this.idProfesorActual() === this.detalle()?.idAsesor;
  }

  protected permiteDictamen(): boolean {
    return this.esPersonal() && this.estado() === 'En dictamen';
  }

  protected estado(): string {
    return this.detalle()?.estado ?? '';
  }

  protected seleccionarArchivo(evento: Event): void {
    const archivo = (evento.target as HTMLInputElement).files?.item(0);
    this.archivoSeleccionado.set(archivo ?? null);
  }

  protected async subirDocumento(): Promise<void> {
    const archivo = this.archivoSeleccionado();
    if (!archivo || this.subiendo()) {
      return;
    }

    this.subiendo.set(true);
    this.error.set('');

    try {
      const resultado = await primera(this.tesis.subirDocumento(this.folio(), archivo));
      if (resultado) {
        this.archivoSeleccionado.set(null);
        await this.cargarDetalle();
      } else {
        this.error.set('No se pudo subir el documento.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  protected async descargarDocumento(): Promise<void> {
    this.error.set('');

    try {
      const blob = await primera(this.tesis.descargarDocumento(this.folio()));
      if (!blob) {
        return;
      }
      const nombre = this.detalle()?.documentos.at(-1)?.nombre ?? 'tesis.pdf';
      const url = URL.createObjectURL(blob);
      const enlace = document.createElement('a');
      enlace.href = url;
      enlace.download = nombre;
      document.body.appendChild(enlace);
      enlace.click();
      enlace.remove();
      URL.revokeObjectURL(url);
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    }
  }

  protected abrirVistaPreviaModal(nombre?: string): void {
    void this.visorPdf.abrirConPeticion(
      nombre ?? this.detalle()?.titulo ?? 'Documento de Tesis',
      () => this.tesis.descargarDocumento(this.folio()),
      `Folio: ${this.folio()}`,
      this.folio(),
    );
  }

  protected abrirPdfEnPestana(): void {
    const url = this.pdfRawUrl();
    if (url) {
      window.open(url, '_blank');
    } else {
      void this.descargarDocumento();
    }
  }

  protected async alternarFavorito(): Promise<void> {
    const actual = this.esFavorito();
    try {
      if (actual) {
        await primera(this.repositorio.eliminarFavorito(this.folio()));
        this.esFavorito.set(false);
      } else {
        await primera(this.repositorio.agregarFavorito(this.folio()));
        this.esFavorito.set(true);
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    }
  }

  private async cargarFavorito(): Promise<void> {
    if (!this.auth.puedeGuardarFavoritos()) {
      return;
    }
    const idUsuario = this.auth.idUsuario();
    if (!idUsuario) {
      return;
    }
    try {
      const favs = await primera(this.repositorio.misFavoritos(idUsuario));
      const es = (favs ?? []).some((f) => f.folio === this.folio());
      this.esFavorito.set(es);
    } catch {
      // Ignorar error de carga de favoritos
    }
  }

  private async cargarPdfInline(): Promise<void> {
    if (!this.esInvitado()) {
      return;
    }
    this.cargandoPdf.set(true);
    this.errorPdf.set('');
    try {
      const blob = await primera(this.repositorio.descargarDocumento(this.folio()));
      if (blob && blob.size > 0) {
        const pdfBlob = blob.type === 'application/pdf' ? blob : new Blob([blob], { type: 'application/pdf' });
        const raw = URL.createObjectURL(pdfBlob);
        this.pdfRawUrl.set(raw);
        this.pdfUrlSeguro.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
      } else {
        this.errorPdf.set('No se encontró el archivo PDF para previsualizar.');
      }
    } catch {
      this.errorPdf.set('No se pudo cargar la vista previa del PDF.');
    } finally {
      this.cargandoPdf.set(false);
    }
  }

  protected alternarArticulo(): void {
    const abierto = !this.formularioArticuloAbierto();
    this.formularioArticuloAbierto.set(abierto);
    if (!abierto) {
      this.articuloNombreTocado.set(false);
      this.articuloUrlTocado.set(false);
      this.articuloDoiTocado.set(false);
      this.articuloTipoTocado.set(false);
    }
  }

  protected cambiarArticuloNombre(evento: Event): void {
    this.articuloNombreTocado.set(true);
    this.nuevoArticulo.update((a) => ({ ...a, nombre: (evento.target as HTMLInputElement).value }));
  }

  protected cambiarArticuloUrl(evento: Event): void {
    this.articuloUrlTocado.set(true);
    this.nuevoArticulo.update((a) => ({ ...a, url: (evento.target as HTMLInputElement).value }));
  }

  protected cambiarArticuloDoi(evento: Event): void {
    this.articuloDoiTocado.set(true);
    this.nuevoArticulo.update((a) => ({ ...a, doi: (evento.target as HTMLInputElement).value || null }));
  }

  protected cambiarArticuloTipo(evento: Event): void {
    this.articuloTipoTocado.set(true);
    const valor = (evento.target as HTMLSelectElement).value;
    this.nuevoArticulo.update((a) => ({ ...a, idTipoArticulo: valor ? Number(valor) : null }));
  }

  protected async agregarArticulo(): Promise<void> {
    this.articuloNombreTocado.set(true);
    this.articuloUrlTocado.set(true);
    this.articuloDoiTocado.set(true);
    this.articuloTipoTocado.set(true);

    if (!this.formularioArticuloValido() || this.subiendo()) {
      return;
    }

    const articulo = this.nuevoArticulo();
    this.subiendo.set(true);
    this.error.set('');

    try {
      const resultado = await primera(this.tesis.agregarArticulo(this.folio(), articulo));
      if (resultado) {
        this.formularioArticuloAbierto.set(false);
        this.nuevoArticulo.set({ nombre: '', url: '', doi: null, idTipoArticulo: null });
        this.articuloNombreTocado.set(false);
        this.articuloUrlTocado.set(false);
        this.articuloDoiTocado.set(false);
        this.articuloTipoTocado.set(false);
        this.mensajeExitoArticulo.set('Artículo académico registrado exitosamente con formato APA 7 validado.');
        setTimeout(() => this.mensajeExitoArticulo.set(''), 4000);
        await this.cargarDetalle();
      } else {
        this.error.set('No se pudo guardar el artículo.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  // --- MÉTODOS PARA REVISIÓN DE REFERENCIAS DUDOSAS / SOSPECHOSAS ---

  protected async abrirModalDudosas(): Promise<void> {
    if (this.sugerenciasPdf().length === 0) {
      await this.cargarEstadoReferencias();
    }
    // Desmarcadas por defecto para que el alumno decida cuáles sí y cuáles no
    this.indicesSeleccionados.set(new Set());
    this.modalSugerenciasAbierto.set(true);
  }

  protected cerrarModalSugerencias(): void {
    this.modalSugerenciasAbierto.set(false);
    this.indicesSeleccionados.set(new Set());
  }

  protected alternarSeleccionDudosa(idx: number): void {
    const actuales = new Set(this.indicesSeleccionados());
    if (actuales.has(idx)) {
      actuales.delete(idx);
    } else {
      actuales.add(idx);
    }
    this.indicesSeleccionados.set(actuales);
  }

  protected alternarSeleccionarTodasDudosas(): void {
    const totalDudosas = this.referenciasDudosas().length;
    if (this.indicesSeleccionados().size === totalDudosas) {
      this.indicesSeleccionados.set(new Set());
    } else {
      this.indicesSeleccionados.set(new Set(Array.from({ length: totalDudosas }, (_, i) => i)));
    }
  }

  protected async importarDudosasSeleccionadas(): Promise<void> {
    const indices = Array.from(this.indicesSeleccionados());
    if (indices.length === 0 || this.importandoSugerencias()) {
      return;
    }

    const dudosas = this.referenciasDudosas();
    const items = indices
      .map((i) => dudosas[i])
      .filter((s) => !!s && !s.yaRegistrado)
      .map((s) => ({
        nombre: s.nombre,
        url: s.url,
        doi: s.doi,
        idTipoArticulo: s.idTipoArticulo ?? 2,
      }));

    if (items.length === 0) {
      this.cerrarModalSugerencias();
      return;
    }

    this.importandoSugerencias.set(true);
    this.error.set('');

    try {
      await firstValueFrom(this.tesis.importarReferencias(this.folio(), items));
      this.cerrarModalSugerencias();
      this.mensajeExitoArticulo.set(`Se incorporaron ${items.length} referencia(s) autorizada(s) por el alumno a los artículos.`);
      setTimeout(() => this.mensajeExitoArticulo.set(''), 4500);
      await this.cargarDetalle();
      await this.cargarEstadoReferencias();
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.importandoSugerencias.set(false);
    }
  }

  protected async cargarEstadoReferencias(): Promise<void> {
    this.cargandoSugerencias.set(true);
    try {
      const sugerencias = await firstValueFrom(
        this.tesis.obtenerSugerenciasPdf(this.folio()).pipe(timeout(10000))
      );
      this.sugerenciasPdf.set(sugerencias ?? []);

      const detalleActual = this.detalle();
      if (detalleActual && detalleActual.articulos.length === 0) {
        const detalleActualizado = await primera(this.tesis.obtenerDetalle(this.folio()));
        if (detalleActualizado && detalleActualizado.articulos.length > 0) {
          this.detalle.set(detalleActualizado);
        }
      }
    } catch {
      // Ignorar fallo de sondeo
    } finally {
      this.cargandoSugerencias.set(false);
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  protected async importarCitaDesdeInforme(cita: any): Promise<void> {
    if (!cita || this.subiendo()) return;

    const nombre = cita.cita_original ? this.limpiarTituloCita(cita.cita_original) : 'Publicación de investigación';
    const doi = cita.doi || null;
    const url = doi ? `https://doi.org/${doi}` : `https://scholar.google.com/scholar?q=${encodeURIComponent(nombre)}`;

    let idTipo: number | null = 2; // Artículo de revista por defecto
    const lower = (cita.cita_original || '').toLowerCase();
    if (lower.includes('proceedings') || lower.includes('conference') || lower.includes('congreso')) {
      idTipo = 3;
    } else if (lower.includes('chapter') || lower.includes('capítulo')) {
      idTipo = 4;
    } else if (lower.includes('press') || lower.includes('book') || lower.includes('libro')) {
      idTipo = 5;
    }

    this.subiendo.set(true);
    try {
      await firstValueFrom(this.tesis.importarReferencias(this.folio(), [{ nombre, url, doi, idTipoArticulo: idTipo }]));
      this.mensajeExitoArticulo.set(`Artículo "${nombre}" registrado exitosamente.`);
      setTimeout(() => this.mensajeExitoArticulo.set(''), 4000);
      await this.cargarDetalle();
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  protected async importarTodasCitasDesdeInforme(citas: any[]): Promise<void> {
    if (!citas || citas.length === 0 || this.subiendo()) return;

    const lista = citas.map((cita) => {
      const nombre = cita.cita_original ? this.limpiarTituloCita(cita.cita_original) : 'Publicación de investigación';
      const doi = cita.doi || null;
      const url = doi ? `https://doi.org/${doi}` : `https://scholar.google.com/scholar?q=${encodeURIComponent(nombre)}`;
      let idTipo: number | null = 2;
      const lower = (cita.cita_original || '').toLowerCase();
      if (lower.includes('proceedings') || lower.includes('conference') || lower.includes('congreso')) {
        idTipo = 3;
      } else if (lower.includes('chapter') || lower.includes('capítulo')) {
        idTipo = 4;
      } else if (lower.includes('press') || lower.includes('book') || lower.includes('libro')) {
        idTipo = 5;
      }
      return { nombre, url, doi, idTipoArticulo: idTipo };
    });

    this.subiendo.set(true);
    try {
      await firstValueFrom(this.tesis.importarReferencias(this.folio(), lista));
      this.mensajeExitoArticulo.set(`Se importaron ${lista.length} citas como artículos de la tesis.`);
      setTimeout(() => this.mensajeExitoArticulo.set(''), 4500);
      await this.cargarDetalle();
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  protected estaCitaRegistrada(cita: any): boolean {
    const articulos = this.detalle()?.articulos ?? [];
    if (!cita) return false;
    const doi = (cita.doi || '').trim().toLowerCase();
    const titulo = this.limpiarTituloCita(cita.cita_original || '').trim().toLowerCase();

    return articulos.some((a) => {
      if (doi && a.doi && a.doi.trim().toLowerCase() === doi) return true;
      if (titulo && a.nombre && a.nombre.trim().toLowerCase() === titulo) return true;
      return false;
    });
  }

  private limpiarTituloCita(cita: string): string {
    const matchComillas = cita.match(/["“]([^"”]+)["”]/);
    if (matchComillas && matchComillas[1].length > 8) {
      return matchComillas[1].trim();
    }
    const matchApa = cita.match(/\((?:19|20)\d{2}\)\.\s*([^.]+)\./);
    if (matchApa && matchApa[1].length > 8) {
      return matchApa[1].trim();
    }
    const limpia = cita.replace(/^\[[0-9]+\]\s*/, '');
    const partes = limpia.split('. ');
    if (partes.length >= 2 && partes[1].length > 8) {
      return partes[1].trim();
    }
    return limpia.length > 95 ? limpia.substring(0, 92) + '...' : limpia;
  }

  // --- REVISIÓN IA Y MODAL ---

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  protected reporteIADetallado(revision: RevisionIADto): any {
    if (!revision.reporteJson) {
      return null;
    }
    try {
      return JSON.parse(revision.reporteJson);
    } catch {
      return null;
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  protected abrirModalReporte(reporte: any): void {
    this.modalReporteAbierto.set(reporte);
  }

  protected cerrarModalReporte(): void {
    this.modalReporteAbierto.set(null);
  }

  protected async confirmar(tipo: TipoConfirmacion, decision: 'Confirmada' | 'Rechazada'): Promise<void> {
    if (this.subiendo()) {
      return;
    }

    this.subiendo.set(true);
    this.error.set('');

    const peticion: ProcesarConfirmacionRequest = {
      tipo: tipo as 'Asesor' | 'Co-asesor',
      decision,
    };

    try {
      const resultado = await primera(this.tesis.confirmar(this.folio(), peticion));
      if (resultado) {
        await this.cargarDetalle();
      } else {
        this.error.set('No se pudo procesar la confirmación.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  protected async ejecutarRevisionIA(): Promise<void> {
    if (this.subiendo()) {
      return;
    }

    this.subiendo.set(true);
    this.error.set('');

    try {
      const resultado = await primera(this.tesis.revisionIA(this.folio(), this.esUltimaVersionIA()));
      if (resultado) {
        await this.cargarDetalle();
      } else {
        this.error.set('No se pudo ejecutar la revisión IA.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  protected async emitirRevisionAsesor(): Promise<void> {
    if (this.subiendo()) {
      return;
    }

    this.subiendo.set(true);
    this.error.set('');

    const peticion: ProcesarRevisionAsesorRequest = {
      esUltimaVersion: this.esUltimaVersionAsesor(),
      decision: this.decisionAsesor(),
      observaciones: this.observacionesAsesor() || null,
    };

    try {
      const resultado = await primera(this.tesis.revisionAsesor(this.folio(), peticion));
      if (resultado) {
        this.observacionesAsesor.set('');
        await this.cargarDetalle();
      } else {
        this.error.set('No se pudo emitir la revisión del asesor.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  protected async emitirDictamen(): Promise<void> {
    if (this.subiendo()) {
      return;
    }

    this.subiendo.set(true);
    this.error.set('');

    const peticion: EmitirDictamenRequest = {
      resultado: this.resultadoDictamen(),
      observaciones: this.observacionesDictamen() || null,
    };

    try {
      const resultado = await primera(this.tesis.dictamen(this.folio(), peticion));
      if (resultado) {
        this.observacionesDictamen.set('');
        await this.cargarDetalle();
      } else {
        this.error.set('No se pudo emitir el dictamen.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.subiendo.set(false);
    }
  }

  protected ultimaRevisionIA(): RevisionIADto | null {
    const listado = this.detalle()?.revisionesIA ?? [];
    return listado.length ? listado[listado.length - 1] : null;
  }

  protected ultimaRevisionAsesor(): RevisionAsesorDto | null {
    const listado = this.detalle()?.revisionesAsesor ?? [];
    return listado.length ? listado[listado.length - 1] : null;
  }

  protected verArticulo(articulo: ArticuloDto): void {
    window.open(articulo.url, '_blank', 'noopener');
  }

  private async cargarPerfilDocente(): Promise<void> {
    if (!this.esDocente()) {
      return;
    }

    const profesor = await primera(this.catalogos.profesorActual());
    this.idProfesorActual.set(profesor?.idProfesor ?? null);
  }

  private async cargarDetalle(): Promise<void> {
    this.cargando.set(true);
    this.error.set('');

    const [detalle, tipos] = await Promise.all([
      primera(this.tesis.obtenerDetalle(this.folio())),
      this.tiposArticulo().length ? Promise.resolve(this.tiposArticulo()) : primera(this.catalogos.tiposArticulo()),
    ]);

    if (detalle) {
      this.detalle.set(detalle);
      if (detalle.revisionesIA?.some((r) => !!r.reporteJson)) {
        void this.cargarEstadoReferencias();
      }
    } else {
      this.noExiste.set(true);
    }

    this.tiposArticulo.set(tipos ?? []);
    this.cargando.set(false);
  }

  private mensajeError(err: unknown): string {
    const cuerpo = err as { message?: string } | null;
    return cuerpo?.message ?? 'Error inesperado. Intenta de nuevo.';
  }
}
