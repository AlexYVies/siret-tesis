import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { InactividadService } from './core/services/inactividad.service';
import { VisorPdfModalComponent } from './components/visor-pdf-modal/visor-pdf-modal.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, VisorPdfModalComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly inactividad = inject(InactividadService);

  ngOnInit(): void {
    this.inactividad.iniciar();
  }
}