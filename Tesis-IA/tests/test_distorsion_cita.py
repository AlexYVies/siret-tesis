import asyncio
import httpx
import fitz
from pathlib import Path

# Tesis donde el alumno cita a Vaswani et al. (2017) pero DISTORSIONA lo que dijeron
# Dice que lograron 99.9% de precisión y que no ocupan GPU, lo cual es falso respecto al abstract.
TESIS_CON_CITA_DISTORSIONADA = """UNIVERSIDAD TECNOLOGICA DE PUEBLA

CAPITULO 1: ESTADO DEL ARTE
En la investigacion de Vaswani et al. (2017), los autores demostraron que su arquitectura Transformer alcanza una precision perfecta del 99.9% en traduccion automatica y que este metodo elimina por completo la necesidad de utilizar computadoras potentes o tarjetas graficas durante el entrenamiento.

REFERENCIAS BIBLIOGRAFICAS
Vaswani, A. (2017). Attention is all you need. Advances in Neural Information Processing Systems. https://doi.org/10.48550/arXiv.1706.03762
"""

async def test_deteccion_distorsion():
    print("=" * 65)
    print("PRUEBA: DETECCION DE CITAS DISTORSIONADAS O FALSEADAS")
    print("=" * 65)

    doc = fitz.open()
    page = doc.new_page()
    rect = fitz.Rect(40, 40, 560, 800)
    page.insert_textbox(rect, TESIS_CON_CITA_DISTORSIONADA, fontsize=10)
    pdf_path = Path("tests/tesis_distorsion.pdf")
    doc.save(str(pdf_path))
    doc.close()

    async with httpx.AsyncClient(base_url="http://localhost:8000/api/v1", timeout=120.0) as client:
        with open(pdf_path, "rb") as f:
            files = {"archivo": ("tesis_distorsion.pdf", f, "application/pdf")}
            data = {"tesis_id": "tesis_distorsion_1", "titulo": "Tesis Cita Falseada", "alumno_id": "alumno_mentiroso"}
            resp = await client.post("/auditorias", data=data, files=files)
            ticket_id = resp.json()["ticket_id"]
            print(f"Ticket asignado: {ticket_id}")

        while True:
            await asyncio.sleep(4)
            r = await client.get(f"/auditorias/{ticket_id}")
            data = r.json()
            st = data["estado"]
            print(f"[{st}] {data['mensaje']}")
            if st in ["COMPLETADO", "ERROR"]:
                print("\n" + "=" * 65)
                print("RESULTADO DE LA AUDITORÍA:")
                print("=" * 65)
                print("Dictamen:", data["resumen_ejecutivo"]["dictamen_sugerido"])
                print("Semáforo:", data["resumen_ejecutivo"]["semaforo"])
                for c in data["auditoria_citas"]:
                    print("\nCITA:", c["cita_original"])
                    print("TITULO REAL:", c.get("titulo_encontrado"))
                    val = c.get("validacion_contenido")
                    if val:
                        print("¿EL PAPER RESPALDA LO QUE DIJO EL ALUMNO?:", "SÍ" if val["coincide"] else "NO (DISTORSIÓN DETECTADA)")
                        print("EXPLICACIÓN:", val["explicacion"])
                break

if __name__ == "__main__":
    asyncio.run(test_deteccion_distorsion())
