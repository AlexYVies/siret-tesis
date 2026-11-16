import { firstValueFrom, Observable } from 'rxjs';

export function primera<T>(origen: Observable<T>): Promise<T | null> {
  return firstValueFrom(origen).catch(() => null);
}