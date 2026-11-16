import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CatalogosService } from '../../core/services/catalogos.service';
import { TesisService } from '../../core/services/tesis.service';
import { Posgrado, Profesor } from '../../core/models/catalogos.model';
import { NivelPosgrado, RegistrarTesisRequest, TesisListado } from '../../core/models/tesis.model';
import { primera } from '../../core/util/async.util';

@Component({
  selector: 'app-registrar-tesis',
  imports: [RouterLink],
  templateUrl: './registrar-tesis.page.html',
})
export class RegistrarTesisPage {
  private readonly catalogos = inject(CatalogosService);
  private readonly tesis = inject(TesisService);
  private readonly router = inject(Router);

  protected readonly cargando = signal(true);
  protected readonly enviando = signal(false);
  protected readonly error = signal('');
  protected readonly profesores = signal<Profesor[]>([]);
  protected readonly posgradosCatalogo = signal<Posgrado[]>([]);
  protected readonly programas = signal<string[]>([]);
  protected readonly asesoresDisponibles = signal<Profesor[]>([]);
  protected readonly tesisEnProceso = signal<TesisListado | null>(null);

  protected readonly titulo = signal('');
  protected readonly nivel = signal<NivelPosgrado>('Maestría');
  protected readonly programa = signal('');
  protected readonly idAsesor = signal<number | null>(null);
  protected readonly conCoAsesor = signal(false);
  protected readonly idCoAsesor = signal<number | null>(null);

  constructor() {
    void this.inicializar();
  }

  protected cambiarNivel(valor: string): void {
    this.nivel.set(valor as NivelPosgrado);
    this.recalcularCatalogos();
  }

  protected cambiarPrograma(valor: string): void {
    this.programa.set(valor);
    this.actualizarAsesores();
  }

  protected asignarAsesor(evento: Event): void {
    const valor = (evento.target as HTMLSelectElement).value;
    this.idAsesor.set(valor ? Number(valor) : null);
  }

  protected asignarCoAsesor(evento: Event): void {
    const valor = (evento.target as HTMLSelectElement).value;
    this.idCoAsesor.set(valor ? Number(valor) : null);
  }

  protected alternarCoAsesor(): void {
    this.conCoAsesor.update((valor) => !valor);
    if (!this.conCoAsesor()) {
      this.idCoAsesor.set(null);
    }
  }

  protected cancelar(): void {
    void this.router.navigate(['/tesis']);
  }

  protected formularioValido(): boolean {
    return (
      this.tesisEnProceso() === null &&
      this.titulo().trim().length > 0 &&
      this.programa().trim().length > 0 &&
      this.idAsesor() !== null &&
      (!this.conCoAsesor() || this.idCoAsesor() !== null)
    );
  }

  protected async registrar(): Promise<void> {
    if (!this.formularioValido() || this.enviando() || this.tesisEnProceso() !== null) {
      return;
    }

    this.enviando.set(true);
    this.error.set('');

    const peticion: RegistrarTesisRequest = {
      titulo: this.titulo().trim(),
      nivel: this.nivel(),
      programa: this.programa().trim(),
      idAsesor: this.idAsesor()!,
      idCoAsesor: this.conCoAsesor() ? this.idCoAsesor() : null,
    };

    try {
      const resultado = await firstValueFrom(this.tesis.registrar(peticion));
      if (resultado) {
        void this.router.navigate(['/tesis', resultado.folio]);
      } else {
        this.error.set('No se pudo registrar la tesis. Revisa tus datos.');
      }
    } catch (err: unknown) {
      this.error.set(this.mensajeError(err));
    } finally {
      this.enviando.set(false);
    }
  }

  private async inicializar(): Promise<void> {
    try {
      const [profesores, posgrados, misTesis] = await Promise.all([
        primera(this.catalogos.profesores()),
        primera(this.catalogos.posgrados(true)).catch(() => [] as Posgrado[]),
        firstValueFrom(this.tesis.listar()).catch(() => [] as TesisListado[]),
      ]);

      const enProceso = misTesis.find(
        (t) => !['Aceptada', 'Rechazada', 'Inactiva'].includes(t.estado),
      );
      if (enProceso) {
        this.tesisEnProceso.set(enProceso);
      }

      this.profesores.set(profesores ?? []);
      this.posgradosCatalogo.set(posgrados ?? []);
      this.recalcularCatalogos(true);
    } finally {
      this.cargando.set(false);
    }
  }

  private recalcularCatalogos(mantenerPrograma = false): void {
    const actual = this.programa();
    const activosCatalogo = this.posgradosCatalogo()
      .filter((p) => p.nivel === this.nivel() && p.activo)
      .map((p) => p.nombre);

    const unicos = (
      activosCatalogo.length > 0
        ? activosCatalogo
        : Array.from(
            new Set(
              this.profesores().flatMap((p) =>
                p.programas
                  .filter((pg) => pg.nivel === this.nivel())
                  .map((pg) => pg.programa),
              ),
            ),
          )
    ).sort();

    this.programas.set(unicos);

    if (!mantenerPrograma || !unicos.includes(actual)) {
      this.programa.set(unicos[0] ?? '');
      this.idAsesor.set(null);
      this.idCoAsesor.set(null);
    }

    this.actualizarAsesores();
  }

  private actualizarAsesores(): void {
    const disponibles = this.profesores().filter((p) =>
      p.programas.some((pg) => pg.nivel === this.nivel() && pg.programa === this.programa()),
    );

    this.asesoresDisponibles.set(disponibles);
    this.actualizarSeleccionados();
  }

  private actualizarSeleccionados(): void {
    if (this.idAsesor() !== null && !this.asesoresDisponibles().some((p) => p.idProfesor === this.idAsesor())) {
      this.idAsesor.set(null);
    }

    if (this.idCoAsesor() !== null && !this.asesoresDisponibles().some((p) => p.idProfesor === this.idCoAsesor())) {
      this.idCoAsesor.set(null);
    }
  }

  private mensajeError(err: unknown): string {
    const cuerpo = err as { message?: string } | null;
    return cuerpo?.message ?? 'Error inesperado. Intenta de nuevo.';
  }
}