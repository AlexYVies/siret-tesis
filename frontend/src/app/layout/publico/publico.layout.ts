import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../core/services/auth.service';
import { TokenStorageService } from '../../core/services/token-storage.service';

@Component({
  selector: 'app-publico',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './publico.layout.html',
})
export class PublicoLayout {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tokens = inject(TokenStorageService);
  protected readonly apareciendo = signal(true);
  protected readonly esInicio = signal(this.determinarEsInicio(this.router.url));

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.esInicio.set(this.determinarEsInicio(event.urlAfterRedirects || event.url));
      });
  }

  private determinarEsInicio(url: string): boolean {
    const rutaLimpia = url.split('?')[0].split('#')[0];
    return rutaLimpia === '/inicio' || rutaLimpia === '' || rutaLimpia === '/';
  }

  protected reiniciarAnimacion(): void {
    this.apareciendo.set(false);
    setTimeout(() => this.apareciendo.set(true), 20);
  }

  protected salir(): void {
    this.tokens.eliminarToken();
    this.auth.cerrarSesion();
    void this.router.navigate(['/inicio']);
  }
}