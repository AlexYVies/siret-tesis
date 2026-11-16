import os
import sys
import time
import json
import asyncio
import platform
import subprocess
import tempfile
from datetime import datetime
from pathlib import Path
import psutil
import httpx
import fitz  # PyMuPDF

# Asegurar que encuentre el paquete src
sys.path.insert(0, os.path.abspath(os.path.dirname(__file__)))

from src.core.config import settings
from src.db.database import init_db, get_db_connection
from src.services.extractor import DocumentExtractor
from src.services.citation_parser import CitationParser
from src.services.citation_verifier import CitationVerifier
from src.services.llm_auditor import ThesisAuditor


def detectar_hardware():
    """Recopila información detallada de CPU, RAM y GPU del servidor."""
    info = {
        "os": f"{platform.system()} {platform.release()} ({platform.version()})",
        "cpu_procesador": platform.processor(),
        "cpu_nucleos_fisicos": psutil.cpu_count(logical=False) or 1,
        "cpu_hilos_logicos": psutil.cpu_count(logical=True) or 1,
        "ram_total_gb": round(psutil.virtual_memory().total / (1024**3), 2),
        "ram_disponible_gb": round(psutil.virtual_memory().available / (1024**3), 2),
        "gpu_detectada": "No detectada (Modo CPU)",
        "vram_mb": 0
    }

    # Intentar detectar GPU NVIDIA con nvidia-smi
    try:
        res = subprocess.run(
            ["nvidia-smi", "--query-gpu=name,memory.total", "--format=csv,noheader,nounits"],
            capture_output=True, text=True, timeout=5
        )
        if res.returncode == 0 and res.stdout.strip():
            line = res.stdout.strip().split("\n")[0]
            parts = [p.strip() for p in line.split(",")]
            info["gpu_detectada"] = f"NVIDIA {parts[0]}"
            if len(parts) > 1 and parts[1].isdigit():
                info["vram_mb"] = int(parts[1])
    except Exception:
        # Fallback con WMIC en Windows para ver qué adaptador de video tiene
        try:
            res = subprocess.run(
                ["wmic", "path", "win32_VideoController", "get", "name"],
                capture_output=True, text=True, timeout=5
            )
            if res.returncode == 0:
                lines = [l.strip() for l in res.stdout.split("\n") if l.strip() and "Name" not in l]
                if lines:
                    info["gpu_detectada"] = ", ".join(lines)
        except Exception:
            pass

    return info


async def verificar_ollama():
    """Verifica si Ollama está corriendo en el servidor y qué modelos tiene."""
    estado = {
        "activo": False,
        "modelos": [],
        "modelo_seleccionado": settings.OLLAMA_MODEL
    }
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            resp = await client.get(f"{settings.OLLAMA_BASE_URL}/api/tags")
            if resp.status_code == 200:
                estado["activo"] = True
                models_data = resp.json().get("models", [])
                nombres = [m.get("name") for m in models_data if m.get("name")]
                estado["modelos"] = nombres

                if settings.OLLAMA_MODEL in nombres:
                    estado["modelo_seleccionado"] = settings.OLLAMA_MODEL
                elif nombres:
                    # Preferir algún qwen o llama instalado
                    preferidos = [n for n in nombres if "qwen" in n.lower() or "llama" in n.lower()]
                    estado["modelo_seleccionado"] = preferidos[0] if preferidos else nombres[0]
                    settings.OLLAMA_MODEL = estado["modelo_seleccionado"]
                else:
                    estado["modelo_seleccionado"] = None
    except Exception:
        pass
    return estado


def crear_pdf_prueba(ruta_destino: str, num_citas_extra: int = 0):
    """Genera un PDF de tesis con los 4 casos clave + citas adicionales si se solicita."""
    doc = fitz.open()

    cuerpo = (
        "CAPITULO I: MARCO TEORICO Y ANTECEDENTES CIENTIFICOS\n\n"
        "La evolucion de la inteligencia artificial y los modelos generativos ha transformado el paradigma computacional contemporaneo. "
        "Como demostraron Goodfellow et al. (2014), las redes generativas antagonicas se componen de dos modelos que se entrenan simultaneamente: "
        "un modelo generativo que captura la distribucion de datos y un modelo discriminativo que estima la probabilidad de que una muestra provenga del entrenamiento.\n\n"
        "Por otro lado, en el area de procesamiento de lenguaje natural y atencion neuronal, "
        "de acuerdo con las afirmaciones empiricas de Vaswani et al. (2017), el modelo Transformer logro alcanzar una reduccion del 99.8% en el consumo energetico global "
        "y demostro fehacientemente que la arquitectura no requiere computadoras electronicas para operar con exito.\n\n"
        "En lo referente al diseno metodologico cuantitativo de este trabajo, seguimos las directrices epistemologicas de Hernandez Sampieri et al. (2004), "
        "quienes estructuran minuciosamente las fases secuenciales de recoleccion de datos y formulacion del problema cientifico.\n\n"
        "Finalmente, en la frontera de la fisica teorica aplicada, siguiendo los sorprendentes descubrimientos de Smith y Robotnik (2025), "
        "se logro comprobar experimentalmente la teleportacion cuantica instantanea en microtubulos cerebrales de mamiferos a temperatura ambiente.\n"
    )

    p1 = doc.new_page()
    p1.insert_textbox(fitz.Rect(40, 40, 560, 800), cuerpo, fontsize=10)

    citas_base = [
        "Goodfellow, I., Pouget-Abadie, J., Mirza, M., Xu, B., Warde-Farley, D., Ozair, S., Courville, A., & Bengio, Y. (2014). Generative adversarial nets. Advances in neural information processing systems, 27.",
        "Vaswani, A., Shazeer, N., Parmar, N., Uszkoreit, J., Jones, L., Gomez, A. N., Kaiser, L., & Polosukhin, I. (2017). Attention is all you need. Advances in neural information processing systems, 30.",
        "Hernandez Sampieri, R., Fernandez Collado, C., & Baptista Lucio, P. (2004). Metodologia de la investigacion. McGraw-Hill.",
        "Smith, J., & Robotnik, I. (2025). Teleportacion cuantica instantanea en microtubulos cerebrales de mamiferos. International Journal of Advanced Quantum Neurobiology, 15(2), 100-115."
    ]

    citas_catalogo_extra = [
        "He, K., Zhang, X., Ren, S., & Sun, J. (2016). Deep residual learning for image recognition. Proceedings of the IEEE conference on computer vision and pattern recognition.",
        "LeCun, Y., Bengio, Y., & Hinton, G. (2015). Deep learning. Nature, 521(7553), 436-444. https://doi.org/10.1038/nature14539",
        "Krizhevsky, A., Sutskever, I., & Hinton, G. E. (2012). ImageNet classification with deep convolutional neural networks. Communications of the ACM, 60(6), 84-90.",
        "Hochreiter, S., & Schmidhuber, J. (1997). Long short-term memory. Neural computation, 9(8), 1735-1780.",
        "Devlin, J., Chang, M. W., Lee, K., & Toutanova, K. (2019). BERT: Pre-training of deep bidirectional transformers for language understanding. NAACL-HLT.",
        "Silver, D., Huang, A., Maddison, C. J., Guez, A., Sifre, L., Van Den Driessche, G., & Hassabis, D. (2016). Mastering the game of Go with deep neural networks and tree search. Nature, 529(7587), 484-489.",
        "Mnih, V., Kavukcuoglu, K., Silver, D., Rusu, A. A., Veness, J., Bellemare, M. G., & Hassabis, D. (2015). Human-level control through deep reinforcement learning. Nature, 518(7540), 529-533.",
        "Kingma, D. P., & Ba, J. (2015). Adam: A method for stochastic optimization. International Conference on Learning Representations (ICLR).",
        "Srivastava, N., Hinton, G., Krizhevsky, A., Sutskever, I., & Salakhutdinov, R. (2014). Dropout: a simple way to prevent neural networks from overfitting. The journal of machine learning research, 15(1), 1929-1958.",
        "Ronneberger, O., Fischer, P., & Brox, T. (2015). U-net: Convolutional networks for biomedical image segmentation. Medical Image Computing and Computer-Assisted Intervention (MICCAI).",
        "Redmon, J., Divvala, S., Girshick, R., & Farhadi, A. (2016). You only look once: Unified, real-time object detection. Proceedings of the IEEE conference on computer vision and pattern recognition.",
        "Simonyan, K., & Zisserman, A. (2015). Very deep convolutional networks for large-scale image recognition. International Conference on Learning Representations.",
        "Szegedy, C., Liu, W., Jia, Y., Sermanet, P., Reed, S., Anguelov, D., & Rabinovich, A. (2015). Going deeper with convolutions. Proceedings of the IEEE conference on computer vision and pattern recognition.",
        "Bahdanau, D., Cho, K., & Bengio, Y. (2015). Neural machine translation by jointly learning to align and translate. International Conference on Learning Representations.",
        "Mikolov, T., Sutskever, I., Chen, K., Corrado, G. S., & Dean, J. (2013). Distributed representations of words and phrases and their compositionality. Advances in neural information processing systems, 26.",
        "Pennington, J., Socher, R., & Manning, C. D. (2014). Glove: Global vectors for word representation. Empirical methods in natural language processing (EMNLP)."
    ]

    todas_citas = citas_base + citas_catalogo_extra[:num_citas_extra]

    # Paginar referencias de 10 en 10 para que quepan bien en cada página
    for i in range(0, len(todas_citas), 10):
        chunk = todas_citas[i:i+10]
        encabezado = "REFERENCIAS BIBLIOGRAFICAS\n\n" if i == 0 else ""
        bloque = encabezado + "\n\n".join(chunk)
        p = doc.new_page()
        p.insert_textbox(fitz.Rect(40, 40, 560, 800), bloque, fontsize=10)

    doc.save(ruta_destino)
    doc.close()


async def limpiar_cache():
    async with get_db_connection() as db:
        await db.execute("DELETE FROM cache_citas")
        await db.commit()


async def correr_benchmark():
    lineas_reporte = []

    def log(texto=""):
        print(texto)
        lineas_reporte.append(texto)

    log("=" * 78)
    log("   BENCHMARK DE RENDIMIENTO DE SERVIDOR - AUDITOR DE TESIS CON IA")
    log(f"   Fecha de ejecucion: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    log("=" * 78)

    # 1. Diagnóstico de Hardware
    log("\n[1/5] ANALIZANDO HARDWARE DEL SERVIDOR...")
    hw = detectar_hardware()
    log(f"  - Sistema Operativo : {hw['os']}")
    log(f"  - Procesador        : {hw['cpu_procesador']}")
    log(f"  - Nucleos / Hilos   : {hw['cpu_nucleos_fisicos']} fisicos / {hw['cpu_hilos_logicos']} hilos logicos")
    log(f"  - Memoria RAM Total : {hw['ram_total_gb']} GB ({hw['ram_disponible_gb']} GB libres)")
    log(f"  - Tarjeta Grafica   : {hw['gpu_detectada']}" + (f" ({hw['vram_mb']} MB VRAM)" if hw['vram_mb'] else ""))

    # Inicializar BD temporal para la prueba
    await init_db()
    await limpiar_cache()

    # 2. Verificar Ollama
    log("\n[2/5] VERIFICANDO MOTOR DE IA LOCAL (OLLAMA)...")
    ollama_info = await verificar_ollama()
    if ollama_info["activo"]:
        log(f"  - Estado Ollama     : ACTIVO en {settings.OLLAMA_BASE_URL}")
        log(f"  - Modelos instalados: {', '.join(ollama_info['modelos']) if ollama_info['modelos'] else 'Ninguno'}")
        log(f"  - Modelo para test  : {ollama_info['modelo_seleccionado'] or 'SIN MODELO DESCARGADO'}")
    else:
        log(f"  - Estado Ollama     : NO DETECTADO en {settings.OLLAMA_BASE_URL}")
        log("    (Se ejecutaran las pruebas de Extraccion PDF y Verificacion de Citas por APIs;")
        log("     para probar la IA en el servidor instala Ollama y ejecuta 'ollama pull qwen2.5-coder:7b')")

    temp_dir = tempfile.mkdtemp(prefix="tesis_bench_")
    pdf_multicaso = os.path.join(temp_dir, "tesis_multicaso.pdf")
    pdf_volumen = os.path.join(temp_dir, "tesis_20_citas.pdf")

    # 3. Prueba 1: Extracción PDF y Parseo
    log("\n[3/5] PRUEBA 1: EXTRACCION DE PDF Y PARSEO DE BIBLIOGRAFIA...")
    t0 = time.perf_counter()
    crear_pdf_prueba(pdf_multicaso, num_citas_extra=0)
    datos_doc = DocumentExtractor.extract(pdf_multicaso)
    citas_parseadas = CitationParser.parse_references_block(datos_doc["bibliografia_texto"])
    t_extraccion = time.perf_counter() - t0
    log(f"  - Citas detectadas  : {len(citas_parseadas)} referencias")
    log(f"  - Tiempo Extraccion : {t_extraccion:.3f} segundos")

    # 4. Prueba 2: Auditoría Multi-Caso (4 escenarios)
    log("\n[4/5] PRUEBA 2: AUDITORIA MULTI-CASO (4 CITAS + IA LOCAL)...")
    await limpiar_cache()
    t_api_start = time.perf_counter()
    citas_auditadas = await CitationVerifier.verify_citations(citas_parseadas)
    t_api_multicaso = time.perf_counter() - t_api_start
    log(f"  - Tiempo Verificacion APIs (4 casos sin cache): {t_api_multicaso:.2f} segundos")

    t_llm_redaccion = 0.0
    t_llm_claims = 0.0
    distorsion_detectada = False

    if ollama_info["activo"] and ollama_info["modelo_seleccionado"]:
        log(f"  - Ejecutando revision de ortografia/coherencia con {ollama_info['modelo_seleccionado']}...")
        t_llm1 = time.perf_counter()
        llm_res = await ThesisAuditor.audit_text_chunks(datos_doc["cuerpo_texto"])
        t_llm_redaccion = time.perf_counter() - t_llm1
        log(f"    -> Completado en {t_llm_redaccion:.2f} segundos (Coherencia: {llm_res['puntaje_coherencia']}/10)")

        log(f"  - Ejecutando verificacion de fidelidad (texto alumno vs abstract real)...")
        t_llm2 = time.perf_counter()
        for c in citas_auditadas:
            if c.abstract_encontrado:
                clave = c.autor_detectado or str(c.anio_encontrado or "")
                val = await ThesisAuditor.verify_citation_claim(datos_doc["cuerpo_texto"], clave, c.abstract_encontrado)
                c.validacion_contenido = val
                if not val.get("coincide", True):
                    distorsion_detectada = True
        t_llm_claims = time.perf_counter() - t_llm2
        log(f"    -> Completado en {t_llm_claims:.2f} segundos (Distorsion detectada: {'SI' if distorsion_detectada else 'NO'})")
    else:
        log("  - [Omitido paso de IA local por falta de Ollama/modelo activo]")

    t_total_multicaso = t_extraccion + t_api_multicaso + t_llm_redaccion + t_llm_claims
    log(f"  => TIEMPO TOTAL TESIS MULTI-CASO: {t_total_multicaso:.2f} segundos")

    # 5. Prueba 3: Prueba de Volumen (20 Citas en Paralelo Sin Caché vs Con Caché)
    log("\n[5/5] PRUEBA 3: VOLUMEN DE CITAS (20 REFERENCIAS ACADEMICAS)...")
    crear_pdf_prueba(pdf_volumen, num_citas_extra=16)
    datos_vol = DocumentExtractor.extract(pdf_volumen)
    citas_vol = CitationParser.parse_references_block(datos_vol["bibliografia_texto"])

    await limpiar_cache()
    t_vol_nocache_start = time.perf_counter()
    res_vol = await CitationVerifier.verify_citations(citas_vol)
    t_vol_nocache = time.perf_counter() - t_vol_nocache_start

    # Segunda pasada para medir velocidad con caché SQLite
    t_vol_cache_start = time.perf_counter()
    await CitationVerifier.verify_citations(citas_vol)
    t_vol_cache = time.perf_counter() - t_vol_cache_start

    verificadas_count = sum(1 for c in res_vol if c.estado_verificacion.value in ["VERIFICADA", "VERIFICADA_SIN_DOI"])
    log(f"  - Total referencias evaluadas      : {len(res_vol)} ({verificadas_count} reales verificadas, {len(res_vol)-verificadas_count} falsas)")
    log(f"  - Tiempo en frio (APIs en paralelo): {t_vol_nocache:.2f} segundos")
    log(f"  - Tiempo con Cache SQLite activa   : {t_vol_cache:.4f} segundos")

    # Resumen Final y Diagnóstico
    log("\n" + "=" * 78)
    log("                 RESUMEN DE TIEMPOS EN ESTE SERVIDOR")
    log("=" * 78)
    log(f"  1. Extraccion PDF y Parseo          : {t_extraccion:.3f} s")
    log(f"  2. Verificacion 4 Citas (Sin Cache) : {t_api_multicaso:.2f} s")
    log(f"  3. Verificacion 20 Citas (Sin Cache): {t_vol_nocache:.2f} s")
    log(f"  4. Verificacion 20 Citas (Con Cache): {t_vol_cache:.4f} s")
    if ollama_info["activo"] and ollama_info["modelo_seleccionado"]:
        log(f"  5. Auditoria Redaccion (LLM)        : {t_llm_redaccion:.2f} s")
        log(f"  6. Fidelidad de Abstracts (LLM)     : {t_llm_claims:.2f} s")
        log(f"  ------------------------------------------------------------------")
        log(f"  TIEMPO PROMEDIO POR TESIS COMPLETA  : {t_total_multicaso:.2f} segundos ({t_total_multicaso/60:.1f} min)")
    else:
        log(f"  5. Auditoria LLM                    : PENDIENTE (Instalar Ollama)")

    log("\n--- DIAGNOSTICO AUTOMATICO DEL SERVIDOR ---")
    if hw["vram_mb"] >= 3500:
        log("  [EXCELENTE] El servidor cuenta con GPU dedicada suficiente para correr")
        log("              qwen2.5-coder:7b a alta velocidad (~1 a 2 min por tesis).")
    elif hw["ram_total_gb"] >= 16:
        log("  [APTO - MODO CPU] El servidor tiene buena RAM pero no se detecto GPU NVIDIA dedicada.")
        log("                    Recomendacion: Usar modelo ligero 'qwen2.5:3b' en Ollama para")
        log("                    mantener tiempos de respuesta rapidos en CPU (~1.5 a 2.5 min).")
    else:
        log("  [LIMITADO] El servidor tiene poca RAM y no tiene GPU dedicada.")
        log("             Recomendacion: Usar 'qwen2.5:1.5b' o conectar un nodo con GPU.")
    log("=" * 78)

    # Guardar archivo de reporte junto al ejecutable
    base_dir = os.path.dirname(sys.executable) if getattr(sys, "frozen", False) else os.path.dirname(os.path.abspath(__file__))
    reporte_path = os.path.join(base_dir, "REPORTE_RENDIMIENTO_SERVIDOR.txt")
    with open(reporte_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lineas_reporte))

    print(f"\n[OK] Se guardo una copia del reporte en:\n     {reporte_path}")


if __name__ == "__main__":
    try:
        asyncio.run(correr_benchmark())
    except Exception as e:
        print(f"\n[ERROR DURANTE EL BENCHMARK]: {e}")
    finally:
        if getattr(sys, "frozen", False):
            input("\nPresiona ENTER para cerrar esta ventana...")
