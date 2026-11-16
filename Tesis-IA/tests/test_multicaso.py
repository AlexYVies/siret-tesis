import os
import sys
import asyncio
import json
import fitz  # PyMuPDF
import httpx

PDF_TEST_PATH = "tests/tesis_multicaso.pdf"

def generar_tesis_multicaso():
    """Genera un PDF con los 4 casos solicitados por el usuario."""
    doc = fitz.open()

    cuerpo_texto = (
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

    page1 = doc.new_page()
    rect1 = fitz.Rect(40, 40, 560, 800)
    page1.insert_textbox(rect1, cuerpo_texto, fontsize=10)

    bibliografia_texto = (
        "REFERENCIAS BIBLIOGRAFICAS\n\n"
        "Goodfellow, I., Pouget-Abadie, J., Mirza, M., Xu, B., Warde-Farley, D., Ozair, S., Courville, A., & Bengio, Y. (2014). Generative adversarial nets. Advances in neural information processing systems, 27.\n\n"
        "Vaswani, A., Shazeer, N., Parmar, N., Uszkoreit, J., Jones, L., Gomez, A. N., Kaiser, L., & Polosukhin, I. (2017). Attention is all you need. Advances in neural information processing systems, 30.\n\n"
        "Hernandez Sampieri, R., Fernandez Collado, C., & Baptista Lucio, P. (2004). Metodologia de la investigacion. McGraw-Hill.\n\n"
        "Smith, J., & Robotnik, I. (2025). Teleportacion cuantica instantanea en microtubulos cerebrales de mamiferos. International Journal of Advanced Quantum Neurobiology, 15(2), 100-115.\n"
    )

    page2 = doc.new_page()
    rect2 = fitz.Rect(40, 40, 560, 800)
    page2.insert_textbox(rect2, bibliografia_texto, fontsize=10)

    os.makedirs("tests", exist_ok=True)
    doc.save(PDF_TEST_PATH)
    doc.close()
    print(f"[OK] PDF de prueba generado en: {PDF_TEST_PATH}")


async def ejecutar_auditoria():
    generar_tesis_multicaso()

    async with httpx.AsyncClient(base_url="http://localhost:8000/api/v1", timeout=120.0) as client:
        print("\n--- 1. Enviando Tesis Multi-Caso a la API (POST /auditorias) ---")
        with open(PDF_TEST_PATH, "rb") as f:
            files = {"archivo": ("tesis_multicaso.pdf", f, "application/pdf")}
            data = {"tesis_id": "TESIS-MULTICASO-001", "titulo": "Tesis Evaluacion Exhaustiva", "alumno_id": "ALUMNO-TEST"}
            resp = await client.post("/auditorias", data=data, files=files)

        if resp.status_code not in [200, 202]:
            print(f"Error al encolar ({resp.status_code}): {resp.text}")
            sys.exit(1)

        ticket_id = resp.json()["ticket_id"]
        print(f"Ticket asignado: {ticket_id}")

        print("\n--- 2. Monitoreando Progreso en Vivo ---")
        while True:
            await asyncio.sleep(3)
            r = await client.get(f"/auditorias/{ticket_id}")
            data = r.json()
            st = data.get("estado")
            pct = data.get("progreso_porcentaje", 0)
            msg = data.get("mensaje", "")
            print(f"[{pct}%] {st} -> {msg}")

            if st in ["COMPLETADO", "ERROR"]:
                break

        print("\n" + "="*80)
        print("           REPORTE FINAL DE LA AUDITORIA MULTI-CASO")
        print("="*80)
        resumen = data.get("resumen_ejecutivo", {})
        print(f"Dictamen Sugerido: {resumen.get('dictamen_sugerido')}")
        print(f"Semaforo:          {resumen.get('semaforo')}")
        print(f"Total Citas:       {resumen.get('total_citas')}")
        print(f"Citas Validadas:   {resumen.get('citas_verificadas')}")
        print(f"Citas Sospechosas: {resumen.get('citas_sospechosas')}")
        print(f"Puntaje Coherencia:{resumen.get('puntaje_coherencia')}/10")
        print(f"Puntaje Ortografia:{resumen.get('puntaje_ortografia')}/10")

        print("\n--- DETALLE DE CITAS AUDITADAS ---")
        citas = data.get("auditoria_citas", [])
        for idx, c in enumerate(citas, 1):
            print(f"\n[CITA {idx}]")
            print(f"  Texto Original:      {c.get('cita_original')[:90]}...")
            print(f"  Estado Verificacion: {c.get('estado_verificacion')}")
            print(f"  Fuente:              {c.get('fuente_verificacion')}")
            print(f"  Titulo Encontrado:   {c.get('titulo_encontrado')}")
            print(f"  Anio Encontrado:     {c.get('anio_encontrado')}")
            if c.get("validacion_contenido"):
                vc = c["validacion_contenido"]
                print(f"  ¿Coincide con Abstract?: {vc.get('coincide')}")
                print(f"  Analisis de Fidelidad:   {vc.get('explicacion')}")
            if c.get("motivo"):
                print(f"  Detalle/Motivo:          {c.get('motivo')}")

        print("\n--- OBSERVACIONES DE REDACCION ---")
        for obs in data.get("auditoria_redaccion", []):
            print(f"  [{obs.get('tipo')}] {obs.get('observacion')} | Sugerencia: {obs.get('sugerencia')}")

        print("\n" + "="*80)

if __name__ == "__main__":
    asyncio.run(ejecutar_auditoria())
