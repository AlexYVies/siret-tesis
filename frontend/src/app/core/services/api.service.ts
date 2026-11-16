import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, from, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { ApiError } from '../models/api-error';
import { environment } from '../../../environments/environment';

type ParametrosConsulta = Record<string, string | number | boolean | undefined>;

@Injectable({ providedIn: 'root' })
export class ApiService {
  protected readonly baseUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  protected get<T>(ruta: string, parametros?: ParametrosConsulta): Observable<T> {
    return this.http
      .get<T>(this.url(ruta), { params: this.construirParams(parametros) })
      .pipe(catchError((error) => this.mapearError(error)));
  }

  protected getBlob(ruta: string): Observable<Blob> {
    return this.http.get(this.url(ruta), { responseType: 'blob' }).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.error instanceof Blob) {
          return from(error.error.text()).pipe(
            switchMap((texto) => {
              let cuerpo: Partial<ApiError> | undefined;
              try {
                cuerpo = texto ? (JSON.parse(texto) as ApiError) : undefined;
              } catch {
                cuerpo = undefined;
              }
              return throwError(() => ({
                code: cuerpo?.code ?? 'ERROR_DESCONOCIDO',
                message: cuerpo?.message ?? error.message,
                details: cuerpo?.details ?? [],
              }));
            }),
          );
        }
        return this.mapearError(error);
      }),
    );
  }

  protected post<T>(ruta: string, cuerpo?: unknown): Observable<T> {
    return this.http.post<T>(this.url(ruta), cuerpo).pipe(catchError((error) => this.mapearError(error)));
  }

  protected postForm<T>(ruta: string, formulario: FormData): Observable<T> {
    return this.http.post<T>(this.url(ruta), formulario).pipe(catchError((error) => this.mapearError(error)));
  }

  protected put<T>(ruta: string, cuerpo?: unknown): Observable<T> {
    return this.http.put<T>(this.url(ruta), cuerpo).pipe(catchError((error) => this.mapearError(error)));
  }

  protected patch<T>(ruta: string, cuerpo?: unknown): Observable<T> {
    return this.http.patch<T>(this.url(ruta), cuerpo).pipe(catchError((error) => this.mapearError(error)));
  }

  protected delete<T = void>(ruta: string): Observable<T> {
    return this.http.delete<T>(this.url(ruta)).pipe(catchError((error) => this.mapearError(error)));
  }

  private url(ruta: string): string {
    return ruta.startsWith('/') ? `${this.baseUrl}${ruta}` : `${this.baseUrl}/${ruta}`;
  }

  private construirParams(parametros?: ParametrosConsulta): HttpParams {
    let params = new HttpParams();

    if (parametros) {
      for (const [clave, valor] of Object.entries(parametros)) {
        if (valor !== undefined && valor !== null && valor !== '') {
          params = params.set(clave, String(valor));
        }
      }
    }

    return params;
  }

  private mapearError(error: HttpErrorResponse): Observable<never> {
    const cuerpo = error.error as Partial<ApiError> | undefined;

    const apiError: ApiError = {
      code: cuerpo?.code ?? 'ERROR_DESCONOCIDO',
      message: cuerpo?.message ?? error.message,
      details: cuerpo?.details ?? [],
    };

    return throwError(() => apiError);
  }
}