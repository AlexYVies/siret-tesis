import asyncio
import time
import httpx
from pathlib import Path

PDF_PATH = Path("tests/tesis_50_citas.pdf")

async def test_3_tesis_concurrentes():
    print("=" * 65)
    print("PRUEBA DE ESTRÉS: ENVIANDO 3 TESIS SIMULTÁNEAMENTE (DE PUTAZO)")
    print("=" * 65)
    
    if not PDF_PATH.exists():
        print(f"Error: {PDF_PATH} no existe.")
        return

    url = "http://localhost:8000/api/v1/auditorias"
    t_global_start = time.time()

    async with httpx.AsyncClient(timeout=600.0) as client:
        # 1. Enviar las 3 tesis al mismo instante
        async def encolar(i: int):
            with open(PDF_PATH, "rb") as f:
                files = {"archivo": (f"tesis_alumno_{i}.pdf", f.read(), "application/pdf")}
                data = {
                    "tesis_id": f"tesis_concurrente_{i}",
                    "titulo": f"Tesis de Evaluación Concurrente #{i}",
                    "alumno_id": f"alumno_estres_{i}"
                }
                t0 = time.time()
                resp = await client.post(url, data=data, files=files)
                latencia = round(time.time() - t0, 3)
                json_data = resp.json()
                print(f"[SUBIDA {i}] HTTP {resp.status_code} | Ticket: {json_data['ticket_id']} | Encolada en {latencia}s")
                return json_data["ticket_id"]

        print("\nDisparando las 3 peticiones POST en paralelo...")
        tickets = await asyncio.gather(encolar(1), encolar(2), encolar(3))
        t_subida_total = round(time.time() - t_global_start, 2)
        print(f"\n¡Las 3 tesis fueron aceptadas y encoladas en {t_subida_total}s en total!\n")

        # 2. Monitorear el progreso de la cola y de los 3 tickets
        estados = {t: "EN_COLA" for t in tickets}
        completados = {}

        print("Monitoreando el procesamiento FIFO en segundo plano...")
        while len(completados) < len(tickets):
            await asyncio.sleep(5)
            elapsed = round(time.time() - t_global_start, 1)
            linea_status = []
            
            for idx, t in enumerate(tickets, 1):
                if t in completados:
                    linea_status.append(f"Tesis #{idx}: LISTA ({completados[t]}s)")
                else:
                    r = await client.get(f"{url}/{t}")
                    data = r.json()
                    st = data["estado"]
                    prog = data.get("progreso_porcentaje", 0)
                    estados[t] = st
                    if st == "COMPLETADO":
                        completados[t] = elapsed
                        linea_status.append(f"Tesis #{idx}: COMPLETADA!")
                    elif st == "ERROR":
                        completados[t] = f"ERROR: {data.get('error')}"
                        linea_status.append(f"Tesis #{idx}: ERROR")
                    else:
                        linea_status.append(f"Tesis #{idx}: {st} ({prog}%)")

            print(f"[{elapsed}s] " + " | ".join(linea_status))

    t_total = round(time.time() - t_global_start, 2)
    print("\n" + "=" * 65)
    print(f"PRUEBA DE CONCURRENCIA FINALIZADA EN: {t_total} SEGUNDOS")
    print("=" * 65)
    for idx, t in enumerate(tickets, 1):
        print(f" - Tesis #{idx} ({t}): Procesada exitosamente en {completados[t]}s")

if __name__ == "__main__":
    asyncio.run(test_3_tesis_concurrentes())
