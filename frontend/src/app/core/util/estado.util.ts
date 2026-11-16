import { TesisEstado } from '../models/tesis.model';

export function clasesEstado(estado: TesisEstado): string {
  switch (estado) {
    case 'Registrada':
      return 'bg-blue-50 text-blue-700 border-blue-200';
    case 'Confirmada':
      return 'bg-cyan-50 text-cyan-700 border-cyan-200';
    case 'En revisión IA':
      return 'bg-violet-50 text-violet-700 border-violet-200';
    case 'En revisión del asesor':
      return 'bg-amber-50 text-amber-700 border-amber-200';
    case 'Cambios solicitados':
      return 'bg-orange-50 text-orange-700 border-orange-200';
    case 'En dictamen':
      return 'bg-purple-50 text-purple-700 border-purple-200';
    case 'Aceptada':
      return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    case 'Rechazada':
      return 'bg-red-50 text-red-700 border-red-200';
    case 'Inactiva':
      return 'bg-gray-100 text-gray-500 border-gray-200';
  }
}

export function clasesResultado(resultado: string): string {
  if (resultado === 'Válido') {
    return 'bg-emerald-50 text-emerald-700';
  }

  if (resultado === 'No válido') {
    return 'bg-red-50 text-red-700';
  }

  return 'bg-amber-50 text-amber-700';
}