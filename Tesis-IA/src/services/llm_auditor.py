import json
import re
from typing import List, Dict, Any, Tuple
import ollama
from src.core.config import settings
from src.models.schemas import ObservacionRedaccion, TipoObservacionRedaccion

class ThesisAuditor:
    """Audita ortografía, coherencia y estilo académico usando el modelo local en Ollama."""

    PROMPT_VERIFICACION_CITA = """Eres un árbitro científico riguroso.
Tu labor es comparar lo que el estudiante AFIRMA en el texto de su tesis respecto a una cita, contra el RESUMEN (ABSTRACT) REAL del artículo académico publicado.

Debes determinar si:
1. El estudiante cita fielmente los hallazgos o datos del autor.
2. O por el contrario, el estudiante DISTORSIONA, INVENTA porcentajes/datos, o atribuye al autor conclusiones que el abstract NO respalda ni menciona.

Debes responder ÚNICAMENTE en JSON:
{
  "coincide": true | false,
  "explicacion": "breve explicación de máximo 2 oraciones indicando si coincide o cuál es la distorsión detectada"
}"""

    @classmethod
    async def verify_citation_claim(cls, texto_tesis: str, autor_o_anio: str, abstract_real: str) -> Dict[str, Any]:
        """Verifica si la cita en el texto de la tesis distorsiona o inventa lo que dice el abstract."""
        if not abstract_real or not abstract_real.strip():
            return {"coincide": True, "explicacion": "No se cuenta con abstract público para contrastar el contenido."}

        # Extraer oraciones en la tesis donde se menciona al autor o año
        oraciones_relevantes = [
            oracion.strip()
            for oracion in re.split(r"\. |\.\n", texto_tesis)
            if autor_o_anio.lower() in oracion.lower()
        ]
        
        afirmacion = " ".join(oraciones_relevantes[:3]) if oraciones_relevantes else texto_tesis[:400]

        client = ollama.AsyncClient(host=settings.OLLAMA_BASE_URL)
        prompt_user = (
            f"AFIRMACIÓN DEL ALUMNO EN LA TESIS:\n\"{afirmacion}\"\n\n"
            f"ABSTRACT REAL DEL ARTÍCULO CIENTÍFICO:\n\"{abstract_real[:1000]}\"\n\n"
            "¿La afirmación del alumno coincide con el abstract real o distorsiona los datos? Responde en JSON."
        )

        try:
            resp = await client.chat(
                model=settings.OLLAMA_MODEL,
                messages=[
                    {"role": "system", "content": cls.PROMPT_VERIFICACION_CITA},
                    {"role": "user", "content": prompt_user}
                ],
                format="json",
                options={"temperature": 0.1, "num_predict": 300}
            )
            parsed = cls._clean_and_parse_json(resp["message"]["content"].strip())
            return {
                "coincide": bool(parsed.get("coincide", True)),
                "explicacion": parsed.get("explicacion", "Verificación de afirmación completada.")
            }
        except Exception as e:
            return {"coincide": True, "explicacion": f"No se pudo contrastar con LLM: {str(e)}"}

    PROMPT_SISTEMA = """Eres un evaluador académico riguroso y experto en revisión de tesis universitarias y normas de estilo APA 7ma edición (American Psychological Association, 7.ª edición).
Tu labor es auditar un fragmento de tesis y evaluar:
1. Errores ortográficos o gramaticales evidentes (acentuación, puntuación, concordancia).
2. Incoherencias lógicas o falta de cohesión entre ideas.
3. Cumplimiento estricto de NORMAS APA 7MA EDICIÓN:
   - Citas en el texto: formato parentético (Apellido, Año) o narrativo Apellido (Año).
   - Regla de 3 o más autores: en APA 7 se debe utilizar "et al." desde la primera mención (ej. Pérez et al., 2021).
   - Citas textuales: incluir número de página (p. 25 o pp. 25-26). Citas de más de 40 palabras en bloque con sangría y sin comillas.
   - En lista de referencias: suprimir lugar geográfico (ciudad/país de publicación), formato DOI estándar (https://doi.org/10.xxxx), sin "Recuperado de" innecesario, sangría francesa y autores como Apellido, Inicial.
4. Tono y estilo académico universitario formal.

Debes responder ÚNICAMENTE en formato JSON válido con la siguiente estructura exacta:
{
  "puntaje_coherencia": 8.5,
  "puntaje_ortografia": 9.0,
  "puntaje_apa7": 8.5,
  "observaciones": [
    {
      "tipo": "ORTOGRAFIA" | "COHERENCIA" | "FORMATO_ACADEMICO" | "FORMATO_APA7",
      "texto_afectado": "frase textual con el error o incumplimiento",
      "observacion": "explicación clara del error o regla APA 7 infringida",
      "sugerencia": "texto corregido sugerido para el alumno aplicando APA 7"
    }
  ],
  "consejos_alumno": [
    "consejo concreto para mejorar redacción o formato APA 7",
    "consejo concreto 2"
  ]
}
No incluyas explicaciones antes o después del JSON. Solo el JSON."""

    @classmethod
    async def audit_text_chunks(cls, full_text: str, bib_text: str = "") -> Dict[str, Any]:
        """Divide el texto en partes representativas para auditar sin saturar memoria."""
        # Tomamos muestras representativas: inicio (introducción), medio (marco/metodología), final (conclusiones) y referencias
        chunks = cls._create_representative_chunks(full_text, bib_text)
        
        all_observaciones: List[ObservacionRedaccion] = []
        all_consejos: List[str] = []
        total_coherencia = 0.0
        total_ortografia = 0.0
        total_apa7 = 0.0
        chunks_count = len(chunks)

        client = ollama.AsyncClient(host=settings.OLLAMA_BASE_URL)

        for seccion_nombre, chunk_text in chunks:
            prompt_usuario = (
                f"SECCIÓN: {seccion_nombre}\n\nTEXTO:\n{chunk_text}\n\n"
                "Instrucción: Evalúa ortografía, coherencia y cumplimiento de APA 7ma edición. "
                "Genera máximo 3 observaciones principales y responde ÚNICAMENTE con el objeto JSON cerrado."
            )
            try:
                response = await client.chat(
                    model=settings.OLLAMA_MODEL,
                    messages=[
                        {"role": "system", "content": cls.PROMPT_SISTEMA},
                        {"role": "user", "content": prompt_usuario}
                    ],
                    format="json",
                    options={
                        "temperature": 0.1,
                        "num_ctx": 2048,
                        "num_predict": 900
                    }
                )
                raw_content = response["message"]["content"].strip()
                parsed = cls._clean_and_parse_json(raw_content)

                if parsed:
                    total_coherencia += float(parsed.get("puntaje_coherencia", 8.0))
                    total_ortografia += float(parsed.get("puntaje_ortografia", 8.0))
                    total_apa7 += float(parsed.get("puntaje_apa7", 8.0))

                    for obs in parsed.get("observaciones", []):
                        if isinstance(obs, str):
                            obs = {"observacion": obs, "texto_afectado": "N/A", "sugerencia": "Revisar la redacción recomendada."}
                        elif not isinstance(obs, dict):
                            continue

                        tipo_raw = str(obs.get("tipo", "COHERENCIA")).upper()
                        tipo_enum = TipoObservacionRedaccion.COHERENCIA
                        if "APA" in tipo_raw:
                            tipo_enum = TipoObservacionRedaccion.FORMATO_APA7
                        elif "ORTO" in tipo_raw:
                            tipo_enum = TipoObservacionRedaccion.ORTOGRAFIA
                        elif "FORMAT" in tipo_raw:
                            tipo_enum = TipoObservacionRedaccion.FORMATO_ACADEMICO

                        all_observaciones.append(ObservacionRedaccion(
                            seccion=seccion_nombre,
                            tipo=tipo_enum,
                            texto_afectado=obs.get("texto_afectado", ""),
                            observacion=obs.get("observacion", ""),
                            sugerencia=obs.get("sugerencia", "")
                        ))

                    all_consejos.extend(parsed.get("consejos_alumno", []))
            except Exception as e:
                # Si falla Ollama en una muestra, agregamos advertencia genérica
                all_observaciones.append(ObservacionRedaccion(
                    seccion=seccion_nombre,
                    tipo=TipoObservacionRedaccion.COHERENCIA,
                    texto_afectado="N/A",
                    observacion=f"Aviso de auditoría: no se pudo completar el análisis del bloque ({str(e)}).",
                    sugerencia="Revisar manualmente la redacción del capítulo."
                ))

        avg_coherencia = round(total_coherencia / max(1, chunks_count), 1) if chunks_count else 8.0
        avg_ortografia = round(total_ortografia / max(1, chunks_count), 1) if chunks_count else 8.0
        avg_apa7 = round(total_apa7 / max(1, chunks_count), 1) if chunks_count else 8.0

        # Eliminar consejos duplicados
        unique_consejos = list(dict.fromkeys(all_consejos))

        return {
            "puntaje_coherencia": avg_coherencia,
            "puntaje_ortografia": avg_ortografia,
            "puntaje_apa7": avg_apa7,
            "observaciones": all_observaciones,
            "consejos_alumno": unique_consejos[:5]
        }

    @classmethod
    def _create_representative_chunks(cls, text: str, bib_text: str = "") -> List[Tuple[str, str]]:
        """Extrae hasta 3 bloques clave de ~1200 palabras cada uno y una muestra de bibliografía si existe."""
        words = text.split()
        total_words = len(words)
        
        chunks: List[Tuple[str, str]] = []
        if total_words <= 1500:
            chunks.append(("Documento General", text))
        else:
            chunk_size = 1000
            # 1. Inicio (Introducción / Planteamiento)
            inicio = " ".join(words[:chunk_size])
            
            # 2. Medio (Desarrollo / Metodología)
            mid_idx = total_words // 2
            medio = " ".join(words[mid_idx: mid_idx + chunk_size])

            # 3. Final (Conclusiones)
            fin_idx = max(0, total_words - chunk_size)
            fin = " ".join(words[fin_idx:])

            chunks.extend([
                ("Introducción y Justificación", inicio),
                ("Desarrollo Metodológico", medio),
                ("Discusión y Conclusiones", fin)
            ])

        if bib_text and bib_text.strip():
            bib_words = bib_text.split()
            bib_sample = " ".join(bib_words[:500])
            chunks.append(("Lista de Referencias (Formato APA 7)", bib_sample))

        return chunks

    @classmethod
    def _clean_and_parse_json(cls, raw: str) -> Dict[str, Any]:
        """Limpia bloques de código markdown como ```json ... ``` si el LLM los incluye."""
        match = re.search(r"```(?:json)?\s*([\s\S]*?)\s*```", raw)
        clean = match.group(1) if match else raw
        clean = clean.strip()
        try:
            return json.loads(clean)
        except Exception:
            # Fallback buscando las llaves principales
            first_brace = clean.find("{")
            last_brace = clean.rfind("}")
            if first_brace != -1 and last_brace != -1:
                return json.loads(clean[first_brace:last_brace + 1])
            return {}
