import asyncio
import httpx
from pathlib import Path

# Creamos un archivo de tesis de prueba en formato de texto / PDF si es necesario
CONTENIDO_TESIS = """
TITULO: SISTEMA DE INTELIGENCIA ARTIFICIAL PARA REVISION DE TESIS UNIVERSITARIAS

Capítulo 1: Introducción
El presente trabajo de investigacion aborda el desarrollo de sistemas inteligentes aplicados a la educación superior.
En la actualidad, muchas universidades carecen de metodos automatizados para revisar la autenticidad de las tesis.
Por lo tanto se diseño un prototipo. La inteligencia artificial no sirve para nada pero a la vez es el futuro del universo y por eso todos los alumnos deben usarla obligatoriamente.

Capítulo 2: Metodología
Se utilizo un enfoque mixto con tecnicas cualitativas y cuantitativas. Los datos fueron recolectados a traves de encuestas digitales. Sin embargo, no se verifico la muestra estadística, pero los resultados son 100% ciertos porque nosotros lo comprobamos en una tarde de pruebas.

REFERENCIAS BIBLIOGRÁFICAS
Vaswani, A., Shazeer, N., Parmar, N., Uszkoreit, J., Jones, L., Gomez, A. N., Kaiser, Ł., & Polosukhin, I. (2017). Attention is all you need. Advances in Neural Information Processing Systems, 30. https://doi.org/10.48550/arXiv.1706.03762

Goodfellow, I., Pouget-Abadie, J., Mirza, M., Xu, B., Warde-Farley, D., Ozair, S., Courville, A., & Bengio, Y. Generative adversarial nets. Advances in neural information processing systems, 2014.

Pérez-Gómez, J. M., & Robotnik, E. (2025). Algoritmos cuánticos para teleportación de tesis en universidades cósmicas. Revista Intergaláctica de Computación, 99(1), 1-10.
"""

async def test_api_flow():
    # 1. Crear documento PDF de prueba usando PyMuPDF
    import fitz
    doc = fitz.open()
    page = doc.new_page()
    page.insert_text((50, 50), CONTENIDO_TESIS, fontsize=10)
    pdf_path = Path("tests/tesis_ejemplo.pdf")
    doc.save(str(pdf_path))
    doc.close()
    print(f"PDF de prueba generado en: {pdf_path}")

    # 2. Conectar a FastAPI (debe estar corriendo)
    async with httpx.AsyncClient(base_url="http://localhost:8000/api/v1", timeout=120.0) as client:
        # Encolar auditoría
        with open(pdf_path, "rb") as f:
            files = {"archivo": ("tesis_ejemplo.pdf", f, "application/pdf")}
            data = {
                "tesis_id": "tesis_test_101",
                "titulo": "Sistema de Inteligencia Artificial para Tesis",
                "alumno_id": "alumno_45"
            }
            resp = await client.post("/auditorias", data=data, files=files)
            print(f"Respuesta Encolar: {resp.status_code}")
            ticket = resp.json()
            ticket_id = ticket["ticket_id"]
            print(f"Ticket asignado: {ticket_id}")

        # Polling del progreso
        print("\nEsperando a que la cola y la IA procesen el reporte...")
        while True:
            await asyncio.sleep(5)
            status_resp = await client.get(f"/auditorias/{ticket_id}")
            st = status_resp.json()
            print(f"[{st['estado']}] Progreso: {st['progreso_porcentaje']}% - Mensaje: {st['mensaje']}")
            
            if st["estado"] in ["COMPLETADO", "ERROR"]:
                if st["estado"] == "COMPLETADO":
                    print("\n========== REPORTE FINAL CONSOLIDADO ==========")
                    resumen = st["resumen_ejecutivo"]
                    print(f"Semáforo: {resumen['semaforo']}")
                    print(f"Dictamen: {resumen['dictamen_sugerido']}")
                    print(f"Citas Verificadas: {resumen['citas_verificadas']}/{resumen['total_citas']}")
                    print(f"Citas Sospechosas: {resumen['citas_sospechosas']}")
                    print(f"Puntaje Coherencia: {resumen['puntaje_coherencia']}")
                    print(f"Puntaje Ortografía: {resumen['puntaje_ortografia']}")
                    print("\nObservaciones para el Alumno:")
                    for obs in st.get("observaciones_para_alumno", []):
                        print(f" * {obs}")
                else:
                    print("Error:", st.get("error"))
                break

if __name__ == "__main__":
    asyncio.run(test_api_flow())
