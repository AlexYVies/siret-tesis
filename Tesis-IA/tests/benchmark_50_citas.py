import asyncio
import time
import httpx
import fitz
from pathlib import Path
from src.core.config import settings

CUERPO_TESIS_REALISTA = """
UNIVERSIDAD TECNOLÓGICA
DIVISIÓN DE INGENIERÍA EN TECNOLOGÍAS DE LA INFORMACIÓN

TESIS PROFESIONAL
IMPLEMENTACIÓN DE MODELOS DE APRENDIZAJE PROFUNDO Y PROCESAMIENTO DE LENGUAJE NATURAL EN ENTORNOS EDUCATIVOS DISTRIBUIDOS

PRESENTA: ALEXIS RAMÍREZ

CAPÍTULO 1: INTRODUCCIÓN Y PLANTEAMIENTO DEL PROBLEMA
El desarrollo de las redes neuronales artificiales y el procesamiento del lenguaje natural ha experimentado un crecimiento acelerado en la última década. En el ámbito de la educación superior, los procesos de revisión y validación de trabajos de titulación demandan una cantidad significativa de tiempo para los docentes sinodales. La revisión manual de cientos de documentos no solo introduce demoras administrativas, sino que también dificulta la comprobación exhaustiva de las fuentes bibliográficas citadas por los estudiantes.

La inteligencia artificial moderna ofrece mecanismos automatizados para procesar grandes volúmenes de texto, extraer patrones semánticos y auditar referencias cruzadas con repositorios científicos abiertos. Sin embargo, muchas instituciones carecen de herramientas adaptadas a sus flujos internos, lo que genera inconsistencias en la detección de citas dudosas o errores de redacción académica.

CAPÍTULO 2: MARCO TEÓRICO Y ESTADO DEL ARTE
Diversas investigaciones han abordado el uso de arquitecturas basadas en Transformers para tareas de clasificación textual, análisis de coherencia discursiva y extracción de entidades nombradas. Los modelos autorregresivos permiten analizar párrafos enteros en busca de ambigüedades lógicas, mientras que las bases de datos abiertas como Crossref y Semantic Scholar facilitan el cotejo automatizado de identificadores de objetos digitales (DOI).

En este contexto, la integración de servicios distribuidos mediante APIs asíncronas optimiza el consumo de recursos de cómputo en servidores locales, asegurando que el hardware de bajo costo pueda gestionar múltiples solicitudes de auditoría sin degradación en los tiempos de respuesta.

CAPÍTULO 3: METODOLOGÍA
La metodología implementada contempla tres fases principales:
1. Extracción documental y aislamiento de la sección bibliográfica.
2. Consulta concurrente hacia catálogos de metadatos académicos mediante HTTP asíncrono con control de tasa.
3. Evaluación de cohesión discursiva y ortografía utilizando un modelo de lenguaje local optimizado.

Se aplicaron pruebas con lotes de documentos académicos para medir la latencia total del pipeline y la precisión en la identificación de referencias válidas versus citas inconsistentes.

CAPÍTULO 4: CONCLUSIONES Y TRABAJO FUTURO
Los resultados obtenidos demuestran que un esquema híbrido que combina verificación determinista de metadatos con inferencia local de lenguaje natural reduce significativamente el tiempo de dictamen docente. Se recomienda como trabajo futuro la incorporación de detección de similitud textual para prevenir plagio directo.
"""

async def generar_tesis_50_citas():
    print("Obteniendo 50 papers reales indexados desde Crossref API...")
    url = "https://api.crossref.org/works"
    params = {"query": "artificial intelligence neural networks natural language processing", "rows": 47}
    headers = {"User-Agent": f"TesisAuditorBot/1.0 (mailto:{settings.CROSSREF_MAILTO})"}

    async with httpx.AsyncClient(timeout=20.0) as client:
        r = await client.get(url, params=params, headers=headers)
        items = r.json()["message"]["items"]

    citas_texto = []
    # 47 citas reales de Crossref
    for it in items:
        title = it.get("title", ["Investigación en Inteligencia Artificial"])[0]
        doi = it.get("DOI", "")
        year = it.get("published", {}).get("date-parts", [[2021]])[0][0] if it.get("published", {}).get("date-parts") else 2021
        authors = it.get("author", [])
        if authors and "family" in authors[0]:
            author_str = f"{authors[0]['family']}, {authors[0].get('given', 'A.')[0]}."
        else:
            author_str = "García, M."
        citas_texto.append(f"{author_str} ({year}). {title}. International Journal of Science and Computing. https://doi.org/{doi}")

    # Agregamos 3 citas inventadas/falsas para probar que el detector las marque sospechosas
    citas_texto.append("López-Gómez, R. (2025). Teleportación de datos cuánticos en servidores universitarios imaginarios. Revista Galáctica de Algoritmos, 45(2), 11-20.")
    citas_texto.append("Smith, J. Z., & Robotnik, D. (2024). Flying cars and deep thought synthesis using time machines. Journal of Fictional Inventions, 12(1), 50-60.")
    citas_texto.append("Desconocido, X. (2026). Estudio inventado sin registro científico alguno. Publicación Fantasma.")

    print(f"Total de referencias preparadas: {len(citas_texto)} (47 reales + 3 falsas/sospechosas)")

    # Construir el PDF
    documento_completo = CUERPO_TESIS_REALISTA + "\n\nREFERENCIAS BIBLIOGRÁFICAS\n\n" + "\n\n".join(citas_texto)
    pdf_path = Path("tests/tesis_50_citas.pdf")
    
    doc = fitz.open()
    # Paginamos el documento
    lineas = documento_completo.split("\n")
    lineas_por_pagina = 45
    for i in range(0, len(lineas), lineas_por_pagina):
        page = doc.new_page()
        bloque = "\n".join(lineas[i:i + lineas_por_pagina])
        page.insert_text((40, 40), bloque, fontsize=9)

    doc.save(str(pdf_path))
    doc.close()
    print(f"Documento de tesis generado exitosamente: {pdf_path} (Páginas: {len(fitz.open(str(pdf_path)))})")
    return pdf_path

async def benchmark_auditoria():
    pdf_path = await generar_tesis_50_citas()

    print("\nIniciando Benchmark de Auditoría...")
    print(f"Enviando tesis al endpoint: http://localhost:8000/api/v1/auditorias")
    t_inicio = time.time()

    async with httpx.AsyncClient(base_url="http://localhost:8000/api/v1", timeout=300.0) as client:
        with open(pdf_path, "rb") as f:
            files = {"archivo": ("tesis_50_citas.pdf", f, "application/pdf")}
            data = {
                "tesis_id": "tesis_benchmark_50",
                "titulo": "Implementación de Modelos de Aprendizaje Profundo",
                "alumno_id": "alexis_ramirez"
            }
            resp = await client.post("/auditorias", data=data, files=files)
            ticket_id = resp.json()["ticket_id"]
            t_encolado = time.time() - t_inicio
            print(f"Tesis encolada en: {round(t_encolado, 2)} segundos. Ticket: {ticket_id}")

        print("\nEsperando procesamiento en segundo plano...")
        while True:
            await asyncio.sleep(4)
            r = await client.get(f"/auditorias/{ticket_id}")
            st = r.json()
            tiempo_transcurrido = round(time.time() - t_inicio, 1)
            print(f"[{tiempo_transcurrido}s] Estado: {st['estado']} | Progreso: {st['progreso_porcentaje']}% | {st['mensaje']}")

            if st["estado"] in ["COMPLETADO", "ERROR"]:
                t_total = time.time() - t_inicio
                print("\n" + "="*60)
                print(f"AUDITORÍA FINALIZADA EN: {round(t_total, 2)} SEGUNDOS")
                print("="*60)
                resumen = st["resumen_ejecutivo"]
                print(f"Semáforo asignado: {resumen['semaforo']}")
                print(f"Dictamen sugerido: {resumen['dictamen_sugerido']}")
                print(f"Total Citas Analizadas: {resumen['total_citas']}")
                print(f"Citas Verificadas en Crossref: {resumen['citas_verificadas']}")
                print(f"Citas Sospechosas detectadas: {resumen['citas_sospechosas']}")
                print(f"Puntaje de Coherencia: {resumen['puntaje_coherencia']}/10")
                print(f"Puntaje de Ortografía: {resumen['puntaje_ortografia']}/10")
                
                print("\nCitas marcadas como sospechosas/no encontradas:")
                for c in st["auditoria_citas"]:
                    if c["estado_verificacion"] != "VERIFICADA":
                        print(f" - {c['cita_original'][:80]}... [MOTIVO: {c['motivo']}]")
                break

if __name__ == "__main__":
    asyncio.run(benchmark_auditoria())
