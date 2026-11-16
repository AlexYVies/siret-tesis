import { Component, effect, HostListener, inject, OnDestroy } from '@angular/core';
import { VisorPdfService } from '../../core/services/visor-pdf.service';

@Component({
  selector: 'app-visor-pdf-modal',
  standalone: true,
  templateUrl: './visor-pdf-modal.component.html',
})
export class VisorPdfModalComponent implements OnDestroy {
  protected readonly visor = inject(VisorPdfService);

  constructor() {
    effect(() => {
      if (typeof document !== 'undefined') {
        if (this.visor.visible()) {
          document.body.style.overflow = 'hidden';
        } else {
          document.body.style.overflow = '';
        }
      }
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.visor.visible()) {
      this.visor.cerrar();
    }
  }

  ngOnDestroy(): void {
    if (typeof document !== 'undefined') {
      document.body.style.overflow = '';
    }
    this.visor.cerrar();
  }
}
