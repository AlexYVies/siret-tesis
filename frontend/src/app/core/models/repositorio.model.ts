import { NivelPosgrado } from './tesis.model';

export type OrdenRepositorio = 'recientes' | 'autor';

export interface RepositorioEntrada {
  folio: string;
  titulo: string;
  nivel: NivelPosgrado;
  programa: string;
  autor: string;
  asesor?: string | null;
  fechaAceptacion?: string | null;
}

export interface RepositorioBusqueda {
  termino?: string;
  nivel?: NivelPosgrado;
  programa?: string;
  autor?: string;
  orden?: OrdenRepositorio;
  pagina?: number;
  tamano?: number;
}

export interface RepositorioResultado {
  data: RepositorioEntrada[];
  total: number;
  pagina: number;
  tamano: number;
}

export interface IndicadorPrograma {
  programa: string;
  nivel: NivelPosgrado;
  total: number;
  aceptadas: number;
  enTramite: number;
  rechazadas: number;
}

export interface IndicadorNivel {
  nivel: NivelPosgrado;
  total: number;
  aceptadas: number;
  enTramite?: number;
  rechazadas?: number;
  programas?: IndicadorPrograma[];
}

export interface IndicadorTipoArticulo {
  tipo: string;
  cantidad: number;
}

export interface Indicadores {
  total: number;
  aceptadas: number;
  rechazadas: number;
  enTramite: number;
  porNivel: IndicadorNivel[];
  porTipoArticulo: IndicadorTipoArticulo[];
  porPrograma?: IndicadorPrograma[];
}
