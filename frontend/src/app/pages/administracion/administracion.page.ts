import { Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { CatalogosService } from '../../core/services/catalogos.service';
import { Rol } from '../../core/models/constantes.model';
import { NivelPosgrado } from '../../core/models/tesis.model';
import {
  DirectorPosgrado,
  Posgrado,
  Profesor,
  ProfesorPrograma,
  RegistrarProfesorRequest,
  SolicitudPosgradoRequest,
  TipoArticulo,
  TipoArticuloRequest,
} from '../../core/models/catalogos.model';
import { primera } from '../../core/util/async.util';

export interface PosgradoEditable {
  nivel: NivelPosgrado;
  programa: string;
}

@Component({
  selector: 'app-administracion',
  imports: [],
  templateUrl: './administracion.page.html',
})
export class AdministracionPage {
  private readonly catalogos = inject(CatalogosService);
  private readonly auth = inject(AuthService);

  // Estados generales
  protected readonly cargando = signal(true);
  protected readonly error = signal('');
  protected readonly profesores = signal<Profesor[]>([]);

  // Registro de nuevo docente
  protected readonly nombre = signal('');
  protected readonly correo = signal('');
  protected readonly posgrados = signal<PosgradoEditable[]>([
    { nivel: 'Maestría', programa: '' },
  ]);
  protected readonly guardando = signal(false);
  protected readonly mensaje = signal('');
  protected readonly errorProfesor = signal('');

  // Reenvío de invitaciones
  protected readonly reenviandoId = signal<number | null>(null);
  protected readonly profesorAsignandoCorreo = signal<number | null>(null);
  protected readonly correoAsignar = signal('');
  protected readonly mensajeLista = signal('');

  // Catálogo de Posgrados (Maestrías y Doctorados)
  protected readonly posgradosCatalogo = signal<Posgrado[]>([]);
  protected readonly posgradosActivos = computed(() =>
    this.posgradosCatalogo().filter((p) => p.activo),
  );
  protected readonly cargandoPosgrados = signal(false);
  protected readonly nuevoPosgradoNivel = signal<NivelPosgrado>('Maestría');
  protected readonly nuevoPosgradoNombre = signal('');
  protected readonly guardandoPosgrado = signal(false);
  protected readonly mensajePosgrado = signal('');
  protected readonly errorPosgrado = signal('');

  // Edición de un posgrado
  protected readonly posgradoEnEdicion = signal<Posgrado | null>(null);
  protected readonly edicionNivel = signal<NivelPosgrado>('Maestría');
  protected readonly edicionNombre = signal('');
  protected readonly guardandoEdicion = signal(false);
  protected readonly errorEdicion = signal('');
  protected readonly procesandoBajaId = signal<number | null>(null);

  // Modificación de posgrados de un profesor
  protected readonly profesorGestionandoPosgrados = signal<number | null>(null);
  protected readonly posgradosProfesorEditando = signal<PosgradoEditable[]>([]);
  protected readonly guardandoPosgradosProfesor = signal(false);
  protected readonly mensajeGestionProfesor = signal('');
  protected readonly errorGestionProfesor = signal('');

  // Catálogo de Tipos de Artículo
  protected readonly tiposArticulo = signal<TipoArticulo[]>([]);
  protected readonly tipoNombre = signal('');
  protected readonly tipoDescripcion = signal('');
  protected readonly guardandoTipo = signal(false);
  protected readonly mensajeTipo = signal('');

  // Directores de Posgrado
  protected readonly directores = signal<DirectorPosgrado[]>([]);
  protected readonly cargandoDirectores = signal(false);
  protected readonly directorNombre = signal('');
  protected readonly directorCorreo = signal('');
  protected readonly directorContrasena = signal('Admin.123');
  protected readonly guardandoDirector = signal(false);
  protected readonly mensajeDirector = signal('');
  protected readonly errorDirector = signal('');
  protected readonly removiendoDirectorId = signal<number | null>(null);

  constructor() {
    void this.cargarDatos();
  }

  protected esAdministrador(): boolean {
    return this.auth.tieneRol(Rol.Administrador);
  }

  private async cargarDatos(): Promise<void> {
    this.cargando.set(true);
    await Promise.all([
      this.cargarPosgrados(),
      this.cargarProfesores(),
      this.cargarDirectores(),
      this.cargarTiposArticulo(),
    ]);
    this.cargando.set(false);
  }

  protected async cargarPosgrados(): Promise<void> {
    this.cargandoPosgrados.set(true);
    try {
      const lista = await primera(this.catalogos.posgrados(false));
      if (lista) {
        this.posgradosCatalogo.set(lista);
        // Si el registro de docente tiene programa vacío, predeterminar con el primer activo
        const activos = lista.filter((p) => p.activo);
        if (activos.length > 0 && this.posgrados().length === 1 && !this.posgrados()[0].programa) {
          this.posgrados.set([{ nivel: activos[0].nivel, programa: activos[0].nombre }]);
        }
      }
    } catch {
      // Ignorar error de red si aplica
    } finally {
      this.cargandoPosgrados.set(false);
    }
  }

  protected async cargarProfesores(): Promise<void> {
    this.error.set('');
    try {
      const lista = await primera(this.catalogos.profesores());
      if (lista) {
        this.profesores.set(lista);
      }
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.error.set(cuerpo?.message ?? 'No se pudo cargar la lista de profesores.');
    }
  }

  protected async cargarTiposArticulo(): Promise<void> {
    try {
      const lista = await primera(this.catalogos.tiposArticulo());
      if (lista) {
        this.tiposArticulo.set(lista);
      }
    } catch {
      // Ignorar error de carga de tipos
    }
  }

  // --- Gestión de Posgrados ---

  protected async registrarPosgrado(): Promise<void> {
    this.mensajePosgrado.set('');
    this.errorPosgrado.set('');

    const nombre = this.nuevoPosgradoNombre().trim();
    if (!nombre) {
      this.errorPosgrado.set('Indique el nombre del programa de posgrado.');
      return;
    }

    this.guardandoPosgrado.set(true);
    try {
      const solicitud: SolicitudPosgradoRequest = {
        nivel: this.nuevoPosgradoNivel(),
        nombre,
      };
      const creado = await firstValueFrom(this.catalogos.registrarPosgrado(solicitud));
      this.nuevoPosgradoNombre.set('');
      this.mensajePosgrado.set(`Posgrado "${creado.nombre}" (${creado.nivel}) registrado con éxito.`);
      await this.cargarPosgrados();
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.errorPosgrado.set(cuerpo?.message ?? 'No se pudo registrar el posgrado.');
    } finally {
      this.guardandoPosgrado.set(false);
    }
  }

  protected iniciarEdicionPosgrado(posgrado: Posgrado): void {
    this.posgradoEnEdicion.set(posgrado);
    this.edicionNivel.set(posgrado.nivel);
    this.edicionNombre.set(posgrado.nombre);
    this.errorEdicion.set('');
  }

  protected cancelarEdicionPosgrado(): void {
    this.posgradoEnEdicion.set(null);
    this.errorEdicion.set('');
  }

  protected async guardarEdicionPosgrado(): Promise<void> {
    const pos = this.posgradoEnEdicion();
    if (!pos) return;

    this.errorEdicion.set('');
    const nombre = this.edicionNombre().trim();
    if (!nombre) {
      this.errorEdicion.set('El nombre del posgrado no puede estar vacío.');
      return;
    }

    this.guardandoEdicion.set(true);
    try {
      await firstValueFrom(
        this.catalogos.actualizarPosgrado(pos.idPosgrado, {
          nivel: this.edicionNivel(),
          nombre,
        }),
      );
      this.mensajePosgrado.set(`Posgrado "${nombre}" modificado correctamente.`);
      this.posgradoEnEdicion.set(null);
      await Promise.all([this.cargarPosgrados(), this.cargarProfesores()]);
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.errorEdicion.set(cuerpo?.message ?? 'Error al actualizar el posgrado.');
    } finally {
      this.guardandoEdicion.set(false);
    }
  }

  protected async eliminarODarDeBajaPosgrado(posgrado: Posgrado): Promise<void> {
    const confirmar = window.confirm(
      `¿Está seguro de dar de baja o eliminar el posgrado "${posgrado.nombre}"?\n\n` +
      `Si tiene tesis registradas o publicadas, se dará de baja y no se borrará para preservar el historial.\n` +
      `Si tiene tesis en curso, no se permitirá darlo de baja.\n` +
      `Se desasignará automáticamente de todos los profesores que lo impartan.`,
    );
    if (!confirmar) return;

    this.mensajePosgrado.set('');
    this.errorPosgrado.set('');
    this.procesandoBajaId.set(posgrado.idPosgrado);

    try {
      const res = await firstValueFrom(
        this.catalogos.eliminarODarDeBajaPosgrado(posgrado.idPosgrado),
      );
      this.mensajePosgrado.set(res.mensaje);
      await Promise.all([this.cargarPosgrados(), this.cargarProfesores()]);
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.errorPosgrado.set(
        cuerpo?.message ?? 'No se pudo dar de baja el posgrado. Verifique que no tenga tesis en curso.',
      );
    } finally {
      this.procesandoBajaId.set(null);
    }
  }

  protected async reactivarPosgrado(posgrado: Posgrado): Promise<void> {
    this.mensajePosgrado.set('');
    this.errorPosgrado.set('');
    this.procesandoBajaId.set(posgrado.idPosgrado);

    try {
      await firstValueFrom(
        this.catalogos.actualizarPosgrado(posgrado.idPosgrado, {
          nivel: posgrado.nivel,
          nombre: posgrado.nombre,
          activo: true,
        }),
      );
      this.mensajePosgrado.set(`Posgrado "${posgrado.nombre}" reactivado exitosamente.`);
      await this.cargarPosgrados();
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.errorPosgrado.set(cuerpo?.message ?? 'No se pudo reactivar el posgrado.');
    } finally {
      this.procesandoBajaId.set(null);
    }
  }

  // --- Modificar Posgrados de un Profesor ---

  protected abrirGestionPosgrados(profesor: Profesor): void {
    this.profesorGestionandoPosgrados.set(profesor.idProfesor);
    this.mensajeGestionProfesor.set('');
    this.errorGestionProfesor.set('');

    const programasActuales = profesor.programas.map((p) => ({
      nivel: p.nivel,
      programa: p.programa,
    }));

    if (programasActuales.length === 0) {
      const primerActivo = this.posgradosActivos()[0];
      if (primerActivo) {
        programasActuales.push({ nivel: primerActivo.nivel, programa: primerActivo.nombre });
      } else {
        programasActuales.push({ nivel: 'Maestría', programa: '' });
      }
    }

    this.posgradosProfesorEditando.set(programasActuales);
  }

  protected cerrarGestionPosgrados(): void {
    this.profesorGestionandoPosgrados.set(null);
    this.posgradosProfesorEditando.set([]);
    this.mensajeGestionProfesor.set('');
    this.errorGestionProfesor.set('');
  }

  protected agregarPosgradoAProfesor(): void {
    const primerActivo = this.posgradosActivos()[0];
    const item: PosgradoEditable = primerActivo
      ? { nivel: primerActivo.nivel, programa: primerActivo.nombre }
      : { nivel: 'Maestría', programa: '' };

    this.posgradosProfesorEditando.update((actual) => [...actual, item]);
  }

  protected quitarPosgradoDeProfesor(indice: number): void {
    this.posgradosProfesorEditando.update((actual) => actual.filter((_, i) => i !== indice));
  }

  protected cambiarNivelProfesorEditando(indice: number, valor: string): void {
    const nivel = valor as NivelPosgrado;
    // Seleccionar el primer posgrado activo de ese nivel si existe
    const opciones = this.posgradosActivos().filter((p) => p.nivel === nivel);
    const programaNuevo = opciones[0]?.nombre ?? '';

    this.posgradosProfesorEditando.update((actual) =>
      actual.map((pos, i) => (i === indice ? { nivel, programa: programaNuevo } : pos)),
    );
  }

  protected cambiarProgramaProfesorEditando(indice: number, valor: string): void {
    this.posgradosProfesorEditando.update((actual) =>
      actual.map((pos, i) => (i === indice ? { ...pos, programa: valor } : pos)),
    );
  }

  protected async guardarPosgradosProfesor(idProfesor: number): Promise<void> {
    this.mensajeGestionProfesor.set('');
    this.errorGestionProfesor.set('');

    const posgradosValidos = this.posgradosProfesorEditando()
      .map((p) => ({ nivel: p.nivel, programa: p.programa.trim() }))
      .filter((p) => p.programa.length > 0);

    if (posgradosValidos.length === 0) {
      this.errorGestionProfesor.set('Debe indicar al menos un programa de posgrado para el profesor.');
      return;
    }

    // Verificar duplicados
    const claves = new Set<string>();
    for (const p of posgradosValidos) {
      const clave = `${p.nivel}_${p.programa.toLowerCase()}`;
      if (claves.has(clave)) {
        this.errorGestionProfesor.set(`El posgrado "${p.programa}" (${p.nivel}) está duplicado.`);
        return;
      }
      claves.add(clave);
    }

    this.guardandoPosgradosProfesor.set(true);
    try {
      await firstValueFrom(
        this.catalogos.actualizarPosgradosProfesor(idProfesor, posgradosValidos),
      );
      this.mensajeGestionProfesor.set('Posgrados del docente actualizados correctamente.');
      await this.cargarProfesores();
      setTimeout(() => {
        if (this.profesorGestionandoPosgrados() === idProfesor) {
          this.cerrarGestionPosgrados();
        }
      }, 1200);
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.errorGestionProfesor.set(
        cuerpo?.message ?? 'No se pudieron actualizar los posgrados del profesor.',
      );
    } finally {
      this.guardandoPosgradosProfesor.set(false);
    }
  }

  // --- Registro de nuevo docente ---

  protected agregarPosgrado(): void {
    const primerActivo = this.posgradosActivos()[0];
    const item: PosgradoEditable = primerActivo
      ? { nivel: primerActivo.nivel, programa: primerActivo.nombre }
      : { nivel: 'Maestría', programa: '' };
    this.posgrados.update((actual) => [...actual, item]);
  }

  protected quitarPosgrado(indice: number): void {
    this.posgrados.update((actual) => actual.filter((_, i) => i !== indice));
  }

  protected cambiarNivel(indice: number, valor: string): void {
    const nivel = valor as NivelPosgrado;
    const opciones = this.posgradosActivos().filter((p) => p.nivel === nivel);
    const programaNuevo = opciones[0]?.nombre ?? '';

    this.posgrados.update((actual) =>
      actual.map((pos, i) => (i === indice ? { nivel, programa: programaNuevo } : pos)),
    );
  }

  protected cambiarPrograma(indice: number, valor: string): void {
    this.posgrados.update((actual) =>
      actual.map((pos, i) => (i === indice ? { ...pos, programa: valor } : pos)),
    );
  }

  protected async registrar(): Promise<void> {
    this.mensaje.set('');
    this.errorProfesor.set('');

    const correoLimpio = this.correo().trim();
    const nombreLimpio = this.nombre().trim();

    if (!nombreLimpio) {
      this.errorProfesor.set('Indique el nombre completo del docente.');
      return;
    }

    if (!correoLimpio || !/@upt\.edu\.mx$/i.test(correoLimpio)) {
      this.errorProfesor.set(
        'Indique el correo institucional del docente terminado en @upt.edu.mx para enviarle la invitación.',
      );
      return;
    }

    const solicitud: RegistrarProfesorRequest = {
      nombre: nombreLimpio,
      correo: correoLimpio,
      posgrados: this.posgrados()
        .filter((p) => p.programa.trim().length > 0)
        .map((p): ProfesorPrograma => ({ nivel: p.nivel, programa: p.programa.trim() })),
    };

    if (solicitud.posgrados.length === 0) {
      this.errorProfesor.set('Indique al menos un programa de posgrado.');
      return;
    }

    this.guardando.set(true);
    try {
      const creado = await firstValueFrom(this.catalogos.registrarProfesor(solicitud));
      this.nombre.set('');
      this.correo.set('');
      const primerActivo = this.posgradosActivos()[0];
      this.posgrados.set([
        primerActivo
          ? { nivel: primerActivo.nivel, programa: primerActivo.nombre }
          : { nivel: 'Maestría', programa: '' },
      ]);
      this.mensaje.set(
        `Docente "${creado.nombre}" registrado y correo de invitación enviado a ${correoLimpio}.`,
      );
      await this.cargarProfesores();
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.errorProfesor.set(
        cuerpo?.message ??
          'No se pudo registrar ni enviar la invitación al docente. Verifique los datos.',
      );
    } finally {
      this.guardando.set(false);
    }
  }

  protected async reenviarInvitacion(profesor: Profesor): Promise<void> {
    const correo = profesor.correo?.trim();
    if (!correo) {
      this.profesorAsignandoCorreo.set(profesor.idProfesor);
      this.correoAsignar.set('');
      return;
    }

    this.reenviandoId.set(profesor.idProfesor);
    this.mensajeLista.set('');
    this.auth
      .invitarDocente({
        correo,
        nombre: profesor.nombre,
        idProfesor: profesor.idProfesor,
      })
      .subscribe({
        next: () => {
          this.reenviandoId.set(null);
          this.mensajeLista.set(`Invitación de registro enviada por correo a ${correo}.`);
          void this.cargarProfesores();
        },
        error: () => {
          this.reenviandoId.set(null);
          this.mensajeLista.set(`No fue posible enviar el correo de invitación a ${correo}.`);
        },
      });
  }

  protected enviarInvitacionConCorreoNuevo(profesor: Profesor): void {
    const correo = this.correoAsignar().trim();
    if (!correo || !/@upt\.edu\.mx$/i.test(correo)) {
      this.mensajeLista.set('El correo del docente debe terminar en @upt.edu.mx.');
      return;
    }

    this.reenviandoId.set(profesor.idProfesor);
    this.mensajeLista.set('');
    this.auth
      .invitarDocente({
        correo,
        nombre: profesor.nombre,
        idProfesor: profesor.idProfesor,
      })
      .subscribe({
        next: () => {
          this.reenviandoId.set(null);
          this.profesorAsignandoCorreo.set(null);
          this.correoAsignar.set('');
          this.mensajeLista.set(`Invitación enviada correctamente a ${correo}.`);
          void this.cargarProfesores();
        },
        error: () => {
          this.reenviandoId.set(null);
          this.mensajeLista.set(`No se pudo enviar la invitación a ${correo}.`);
        },
      });
  }

  // --- Tipos de Artículo ---

  protected async registrarTipoArticulo(): Promise<void> {
    this.mensajeTipo.set('');

    const nombre = this.tipoNombre().trim();
    if (!nombre) {
      this.mensajeTipo.set('Indique el nombre del tipo de artículo.');
      return;
    }

    const solicitud: TipoArticuloRequest = {
      nombre,
      descripcion: this.tipoDescripcion().trim() || null,
    };

    this.guardandoTipo.set(true);
    try {
      const creado = await firstValueFrom(this.catalogos.registrarTipoArticulo(solicitud));
      this.tipoNombre.set('');
      this.tipoDescripcion.set('');
      this.mensajeTipo.set(`Tipo "${creado.nombre}" registrado correctamente.`);
      await this.cargarTiposArticulo();
    } catch (err: unknown) {
      const cuerpo = err as { message?: string } | null;
      this.mensajeTipo.set(cuerpo?.message ?? 'No se pudo registrar el tipo de artículo.');
    } finally {
      this.guardandoTipo.set(false);
    }
  }

  // --- Directores de Posgrado ---

  protected async cargarDirectores(): Promise<void> {
    this.cargandoDirectores.set(true);
    try {
      const lista = await primera(this.catalogos.directores());
      if (lista) {
        this.directores.set(lista);
      }
    } catch {
      // Ignorar error si aplica
    } finally {
      this.cargandoDirectores.set(false);
    }
  }

  protected async registrarDirector(): Promise<void> {
    const correo = this.directorCorreo().trim();
    const nombre = this.directorNombre().trim();
    const contrasena = this.directorContrasena().trim();

    this.mensajeDirector.set('');
    this.errorDirector.set('');

    if (!correo) {
      this.errorDirector.set('Debe indicar un correo electrónico institucional.');
      return;
    }

    if (!correo.toLowerCase().endsWith('@upt.edu.mx')) {
      this.errorDirector.set('El correo del director debe pertenecer al dominio institucional @upt.edu.mx.');
      return;
    }

    if (contrasena && contrasena.length < 8) {
      this.errorDirector.set('La contraseña debe tener al menos 8 caracteres.');
      return;
    }

    this.guardandoDirector.set(true);
    try {
      const resp = await firstValueFrom(
        this.catalogos.registrarDirector({
          correo,
          nombre: nombre || 'Director de Posgrado',
          contrasena: contrasena || 'Admin.123',
        }),
      );
      this.mensajeDirector.set(`Usuario Director '${resp.correo}' registrado y configurado con éxito.`);
      this.directorCorreo.set('');
      this.directorNombre.set('');
      this.directorContrasena.set('Admin.123');
      await this.cargarDirectores();
    } catch (err: unknown) {
      const e = err as { error?: { message?: string }; message?: string };
      this.errorDirector.set(e.error?.message || e.message || 'Error al registrar el director de posgrado.');
    } finally {
      this.guardandoDirector.set(false);
    }
  }

  protected async removerDirector(idUsuario: number, correo: string): Promise<void> {
    if (!confirm(`¿Desea revocar el rol de Director de posgrado al usuario '${correo}'?`)) {
      return;
    }

    this.removiendoDirectorId.set(idUsuario);
    this.mensajeDirector.set('');
    this.errorDirector.set('');
    try {
      await firstValueFrom(this.catalogos.removerDirector(idUsuario));
      this.mensajeDirector.set(`Rol de Director de posgrado revocado para '${correo}'.`);
      await this.cargarDirectores();
    } catch (err: unknown) {
      const e = err as { error?: { message?: string }; message?: string };
      this.errorDirector.set(e.error?.message || e.message || 'Error al remover el rol de director.');
    } finally {
      this.removiendoDirectorId.set(null);
    }
  }
}
