import { Component, computed, inject, signal } from '@angular/core';

import { RepositorioService } from '../../core/services/repositorio.service';
import { IndicadorNivel, IndicadorPrograma, Indicadores } from '../../core/models/repositorio.model';
import { NivelPosgrado } from '../../core/models/tesis.model';
import { primera } from '../../core/util/async.util';

export interface RebanadaGrafica {
  id: string;
  etiqueta: string;
  subetiqueta?: string;
  valor: number;
  porcentaje: number;
  color: string;
  pathD: string;
  aceptadas?: number;
  enTramite?: number;
  rechazadas?: number;
  nivel?: NivelPosgrado;
  programa?: string;
}

const PALETA_COLORES = [
  '#7c3aed', // violeta
  '#06b6d4', // cian
  '#10b981', // esmeralda
  '#f59e0b', // ambar
  '#ec4899', // rosa
  '#3b82f6', // azul
  '#8b5cf6', // purpura
  '#14b8a6', // teal
  '#f97316', // naranja
  '#6366f1', // indigo
];

function polarToCartesian(cx: number, cy: number, r: number, angleInDegrees: number) {
  const rad = ((angleInDegrees - 90) * Math.PI) / 180.0;
  return {
    x: cx + r * Math.cos(rad),
    y: cy + r * Math.sin(rad),
  };
}

function describirArcoDonut(
  cx: number,
  cy: number,
  rOuter: number,
  rInner: number,
  startAngle: number,
  endAngle: number,
): string {
  const delta = endAngle - startAngle;
  if (delta >= 359.99) {
    return `M ${cx} ${cy - rOuter} A ${rOuter} ${rOuter} 0 1 1 ${cx} ${cy + rOuter} A ${rOuter} ${rOuter} 0 1 1 ${cx} ${cy - rOuter} M ${cx} ${cy - rInner} A ${rInner} ${rInner} 0 1 0 ${cx} ${cy + rInner} A ${rInner} ${rInner} 0 1 0 ${cx} ${cy - rInner} Z`;
  }

  const p1 = polarToCartesian(cx, cy, rOuter, startAngle);
  const p2 = polarToCartesian(cx, cy, rOuter, endAngle);
  const p3 = polarToCartesian(cx, cy, rInner, endAngle);
  const p4 = polarToCartesian(cx, cy, rInner, startAngle);

  const largeArcFlag = delta > 180 ? 1 : 0;

  return [
    `M ${p1.x.toFixed(2)} ${p1.y.toFixed(2)}`,
    `A ${rOuter} ${rOuter} 0 ${largeArcFlag} 1 ${p2.x.toFixed(2)} ${p2.y.toFixed(2)}`,
    `L ${p3.x.toFixed(2)} ${p3.y.toFixed(2)}`,
    `A ${rInner} ${rInner} 0 ${largeArcFlag} 0 ${p4.x.toFixed(2)} ${p4.y.toFixed(2)}`,
    'Z',
  ].join(' ');
}

@Component({
  selector: 'app-indicadores',
  imports: [],
  templateUrl: './indicadores.page.html',
})
export class IndicadoresPage {
  private readonly repositorio = inject(RepositorioService);

  protected readonly cargando = signal(true);
  protected readonly indicadores = signal<Indicadores | null>(null);

  // Filtro de desglose interactivo: 'General', 'Maestría', 'Doctorado'
  protected readonly vistaNivel = signal<'General' | 'Maestría' | 'Doctorado'>('General');
  protected readonly rebanadaActiva = signal<RebanadaGrafica | null>(null);

  // Total relevante según la vista seleccionada
  protected readonly totalVistaActual = computed(() => {
    const datos = this.indicadores();
    if (!datos) return 0;

    const vista = this.vistaNivel();
    if (vista === 'General') {
      return datos.total;
    }

    const nivelObj = datos.porNivel.find((n) => n.nivel === vista);
    return nivelObj?.total ?? 0;
  });

  // Programas detallados del nivel seleccionado
  protected readonly programasNivelActual = computed<IndicadorPrograma[]>(() => {
    const datos = this.indicadores();
    const vista = this.vistaNivel();
    if (!datos || vista === 'General') return [];

    const nivelObj = datos.porNivel.find((n) => n.nivel === vista);
    if (nivelObj?.programas && nivelObj.programas.length > 0) {
      return nivelObj.programas;
    }

    // Si viene en porPrograma global
    return (datos.porPrograma ?? []).filter((p) => p.nivel === vista);
  });

  // Rebanadas del Pastel interactivo
  protected readonly rebanadasPastel = computed<RebanadaGrafica[]>(() => {
    const datos = this.indicadores();
    if (!datos) return [];

    const vista = this.vistaNivel();
    let items: Array<{
      id: string;
      etiqueta: string;
      subetiqueta?: string;
      valor: number;
      color: string;
      aceptadas?: number;
      enTramite?: number;
      rechazadas?: number;
      nivel?: NivelPosgrado;
      programa?: string;
    }> = [];

    if (vista === 'General') {
      // Nivel General: Maestrías vs Doctorados
      items = datos.porNivel.map((n, i) => ({
        id: `nivel-${n.nivel}`,
        etiqueta: n.nivel,
        subetiqueta: `${n.aceptadas} aceptadas`,
        valor: n.total,
        color: n.nivel === 'Maestría' ? '#7c3aed' : '#06b6d4',
        aceptadas: n.aceptadas,
        enTramite: n.enTramite,
        rechazadas: n.rechazadas,
        nivel: n.nivel,
      }));
    } else {
      // Nivel Específico: Cada programa de Maestría o Doctorado
      const programas = this.programasNivelActual();
      items = programas.map((p, i) => ({
        id: `prog-${p.programa}`,
        etiqueta: p.programa,
        subetiqueta: `${p.aceptadas} aceptadas / ${p.enTramite} en trámite`,
        valor: p.total,
        color: PALETA_COLORES[i % PALETA_COLORES.length],
        aceptadas: p.aceptadas,
        enTramite: p.enTramite,
        rechazadas: p.rechazadas,
        nivel: p.nivel,
        programa: p.programa,
      }));
    }

    const sumaValores = items.reduce((acc, curr) => acc + curr.valor, 0);
    if (sumaValores === 0) return [];

    let anguloAcumulado = 0;
    return items.map((item) => {
      const porcentaje = Math.round((item.valor / sumaValores) * 100);
      const anguloRebanada = (item.valor / sumaValores) * 360;
      const startAngle = anguloAcumulado;
      const endAngle = startAngle + anguloRebanada;
      anguloAcumulado = endAngle;

      const pathD = describirArcoDonut(100, 100, 80, 48, startAngle, endAngle);

      return {
        ...item,
        porcentaje,
        pathD,
      };
    });
  });

  // Rebanadas del Pastel de Estados de Tesis (Aceptadas, En trámite, Rechazadas)
  protected readonly rebanadasEstados = computed<RebanadaGrafica[]>(() => {
    const datos = this.indicadores();
    if (!datos || datos.total === 0) return [];

    const vista = this.vistaNivel();
    let aceptadas = datos.aceptadas;
    let enTramite = datos.enTramite;
    let rechazadas = datos.rechazadas;

    if (vista !== 'General') {
      const nivelObj = datos.porNivel.find((n) => n.nivel === vista);
      if (nivelObj) {
        aceptadas = nivelObj.aceptadas;
        enTramite = nivelObj.enTramite ?? 0;
        rechazadas = nivelObj.rechazadas ?? 0;
      }
    }

    const total = aceptadas + enTramite + rechazadas;
    if (total === 0) return [];

    const estados = [
      { id: 'est-aceptadas', etiqueta: 'Aceptadas', valor: aceptadas, color: '#10b981' },
      { id: 'est-tramite', etiqueta: 'En trámite', valor: enTramite, color: '#f59e0b' },
      { id: 'est-rechazadas', etiqueta: 'Rechazadas', valor: rechazadas, color: '#f43f5e' },
    ].filter((e) => e.valor > 0);

    let anguloAcumulado = 0;
    return estados.map((e) => {
      const porcentaje = Math.round((e.valor / total) * 100);
      const anguloRebanada = (e.valor / total) * 360;
      const startAngle = anguloAcumulado;
      const endAngle = startAngle + anguloRebanada;
      anguloAcumulado = endAngle;

      const pathD = describirArcoDonut(100, 100, 80, 48, startAngle, endAngle);

      return {
        ...e,
        porcentaje,
        pathD,
      };
    });
  });

  // Total de artículos publicados
  protected readonly totalArticulos = computed(() => {
    const tipos = this.indicadores()?.porTipoArticulo ?? [];
    return tipos.reduce((acumulado, tipo) => acumulado + tipo.cantidad, 0);
  });

  // Rebanadas del Pastel de Artículos por Tipo
  protected readonly rebanadasArticulos = computed<RebanadaGrafica[]>(() => {
    const datos = this.indicadores();
    if (!datos) return [];

    const tipos = datos.porTipoArticulo.filter((t) => t.cantidad > 0);
    const total = tipos.reduce((acc, t) => acc + t.cantidad, 0);
    if (total === 0) return [];

    let anguloAcumulado = 0;
    return tipos.map((t, i) => {
      const porcentaje = Math.round((t.cantidad / total) * 100);
      const anguloRebanada = (t.cantidad / total) * 360;
      const startAngle = anguloAcumulado;
      const endAngle = startAngle + anguloRebanada;
      anguloAcumulado = endAngle;

      const pathD = describirArcoDonut(100, 100, 80, 48, startAngle, endAngle);

      return {
        id: `tipo-art-${i}`,
        etiqueta: t.tipo,
        valor: t.cantidad,
        porcentaje,
        color: PALETA_COLORES[(i + 3) % PALETA_COLORES.length],
        pathD,
      };
    });
  });

  constructor() {

    void this.cargar();
  }

  protected seleccionarVista(vista: 'General' | 'Maestría' | 'Doctorado'): void {
    this.vistaNivel.set(vista);
    this.rebanadaActiva.set(null);
  }

  protected clicRebanada(rebanada: RebanadaGrafica): void {
    this.rebanadaActiva.set(rebanada);

    // Si hace clic en Maestría o Doctorado en la vista general, secciona automáticamente
    if (this.vistaNivel() === 'General' && rebanada.nivel) {
      this.vistaNivel.set(rebanada.nivel);
    }
  }

  protected volverGeneral(): void {
    this.vistaNivel.set('General');
    this.rebanadaActiva.set(null);
  }

  protected barraTipos(cantidad: number): number {
    const tipos = this.indicadores()?.porTipoArticulo ?? [];
    const totalArticulos = tipos.reduce((acumulado, tipo) => acumulado + tipo.cantidad, 0);
    return totalArticulos > 0 ? Math.round((cantidad / totalArticulos) * 100) : 0;
  }

  protected calcularPorcentaje(valor: number, total: number): number {
    return total > 0 ? Math.round((valor / total) * 100) : 0;
  }

  private async cargar(): Promise<void> {
    this.cargando.set(true);
    const datos = await primera(this.repositorio.indicadores());
    this.indicadores.set(datos);
    this.cargando.set(false);
  }
}
