import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { ApiService } from './api.service';
import { Indicadores, RepositorioBusqueda, RepositorioEntrada, RepositorioResultado } from '../models/repositorio.model';

@Injectable({ providedIn: 'root' })
export class RepositorioService extends ApiService {
  consultar(busqueda: RepositorioBusqueda = {}): Observable<RepositorioResultado> {
    return this.get<RepositorioResultado>('/repositorio', {
      q: busqueda.termino,
      nivel: busqueda.nivel,
      programa: busqueda.programa,
      autor: busqueda.autor,
      orden: busqueda.orden,
      pagina: busqueda.pagina,
      tamano: busqueda.tamano,
    });
  }

  agregarFavorito(folio: string): Observable<boolean> {
    return this.post<{ ok?: boolean; favorito?: boolean } | void>(`/repositorio/${folio}/favorito`).pipe(
      map(() => true),
    );
  }

  eliminarFavorito(folio: string): Observable<boolean> {
    return this.delete<{ ok?: boolean; favorito?: boolean } | void>(`/repositorio/${folio}/favorito`).pipe(
      map(() => true),
    );
  }

  descargarDocumento(folio: string): Observable<Blob> {
    return this.getBlob(`/repositorio/${folio}/documento`);
  }

  misFavoritos(idUsuario: number): Observable<RepositorioEntrada[]> {
    return this.get<RepositorioEntrada[]>(`/usuarios/${idUsuario}/favoritos`);
  }

  indicadores(): Observable<Indicadores> {
    return this.get<Indicadores>('/indicadores');
  }
}