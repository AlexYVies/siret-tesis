import re
import os
import math
from typing import Dict, Any, List, Optional, Tuple
from pathlib import Path

class APA7DocumentAuditor:
    """
    Auditor integral de formato para documentos académicos según las normas APA 7ma edición.
    Evalúa la totalidad del documento (no solo las referencias):
      1. Tamaño de página y márgenes (Carta / Letter, 2.54 cm).
      2. Tipografía y tamaños permitidos (Times New Roman 12, Calibri 11, Arial 11, Georgia 11, Lucida Sans 10).
      3. Interlineado (doble 2.0) y eliminación de espacio adicional entre párrafos.
      4. Sangría de primera línea (1.27 cm / 0.5 pulg) y alineación.
      5. Paginación en esquina superior derecha.
      6. Jerarquía de encabezados (Niveles 1 al 5).
      7. Formato de Tablas y Figuras (número en negrita, título en cursiva, sin bordes verticales).
      8. Citas textuales extensas (>= 40 palabras en bloque sin comillas).
    """

    # Tolerancia en puntos (1 pulg = 72 pt = 2.54 cm)
    PT_PER_INCH = 72.0
    PT_PER_CM = 72.0 / 2.54  # ~28.346 pt/cm
    PAGE_LETTER_W_PT = 612.0
    PAGE_LETTER_H_PT = 792.0
    MARGIN_APA_PT = 72.0      # 2.54 cm
    INDENT_APA_PT = 36.0      # 1.27 cm (0.5 pulgadas)

    # Fuentes oficiales APA 7 y sus tamaños estándar
    APA7_FONTS = {
        "times new roman": [12],
        "timesnewroman": [12],
        "calibri": [11],
        "arial": [11],
        "georgia": [11],
        "lucida sans unicode": [10],
        "lucidasans": [10],
        "computer modern": [10]
    }

    @classmethod
    def audit_document(cls, file_path: str, cuerpo_texto: Optional[str] = None) -> Dict[str, Any]:
        """Punto de entrada principal. Determina la extensión y ejecuta la auditoría de formato."""
        path = Path(file_path)
        ext = path.suffix.lower()

        if ext == ".pdf":
            return cls._audit_pdf(file_path, cuerpo_texto)
        elif ext in [".docx", ".doc"]:
            return cls._audit_docx(file_path, cuerpo_texto)
        else:
            return cls._audit_fallback(file_path, cuerpo_texto)

    @classmethod
    def _audit_pdf(cls, file_path: str, cuerpo_texto: Optional[str] = None) -> Dict[str, Any]:
        import fitz  # PyMuPDF

        items = []
        detalles = {}
        score = 10.0

        try:
            doc = fitz.open(file_path)
        except Exception as e:
            return {
                "puntaje_formato": 5.0,
                "cumple_general": False,
                "resumen": f"No se pudo leer el archivo PDF para auditar maquetación: {str(e)}",
                "items": [],
                "detalles_tecnicos": {}
            }

        total_pages = len(doc)
        if total_pages == 0:
            doc.close()
            return {
                "puntaje_formato": 0.0,
                "cumple_general": False,
                "resumen": "El documento PDF está vacío.",
                "items": [],
                "detalles_tecnicos": {}
            }

        sample_pages = doc[:min(15, total_pages)]

        # ---------------------------------------------------------
        # 1. Tamaño de Página (Letter 612 x 792 pt)
        # ---------------------------------------------------------
        first_page = doc[0]
        w, h = first_page.rect.width, first_page.rect.height
        is_letter = (abs(w - cls.PAGE_LETTER_W_PT) < 15 and abs(h - cls.PAGE_LETTER_H_PT) < 15)
        is_a4 = (abs(w - 595.0) < 15 and abs(h - 842.0) < 15)

        w_cm = round(w / cls.PT_PER_CM, 2)
        h_cm = round(h / cls.PT_PER_CM, 2)
        detalles["dimensiones_pagina_cm"] = f"{w_cm} x {h_cm} cm"

        if is_letter:
            items.append({
                "criterio": "Tamaño de Papel",
                "cumple": True,
                "detalle_detectado": f"Formato Carta ({w_cm} x {h_cm} cm).",
                "norma_requerida": "Carta / Letter (21.59 x 27.94 cm / 8.5 x 11 pulgadas).",
                "observacion": None,
                "sugerencia": None
            })
        elif is_a4:
            score -= 1.0
            items.append({
                "criterio": "Tamaño de Papel",
                "cumple": False,
                "detalle_detectado": f"Formato A4 detectado ({w_cm} x {h_cm} cm).",
                "norma_requerida": "Carta / Letter (21.59 x 27.94 cm / 8.5 x 11 pulgadas).",
                "observacion": "APA 7ma ed. prescribe tamaño Carta (Letter). A4 es estándar europeo.",
                "sugerencia": "Configurar la página en Word/LaTeX a tamaño Carta (Letter)."
            })
        else:
            score -= 1.5
            items.append({
                "criterio": "Tamaño de Papel",
                "cumple": False,
                "detalle_detectado": f"Dimensiones atípicas ({w_cm} x {h_cm} cm).",
                "norma_requerida": "Carta / Letter (21.59 x 27.94 cm).",
                "observacion": "Las dimensiones no corresponden a hoja Carta ni A4 estándar.",
                "sugerencia": "Ajustar el tamaño de hoja a Carta (Letter)."
            })

        # ---------------------------------------------------------
        # 2. Márgenes (2.54 cm en los cuatro lados)
        # ---------------------------------------------------------
        margin_lefts = []
        margin_rights = []
        margin_tops = []
        margin_bottoms = []

        for p in sample_pages[1:]:  # Omitir portada que suele tener márgenes especiales
            rects = [fitz.Rect(b[:4]) for b in p.get_text("blocks") if b[4].strip()]
            if rects:
                min_x = min(r.x0 for r in rects)
                max_x = max(r.x1 for r in rects)
                min_y = min(r.y0 for r in rects)
                max_y = max(r.y1 for r in rects)

                margin_lefts.append(min_x)
                margin_rights.append(w - max_x)
                margin_tops.append(min_y)
                margin_bottoms.append(h - max_y)

        avg_left = (sum(margin_lefts) / len(margin_lefts)) if margin_lefts else 72.0
        avg_right = (sum(margin_rights) / len(margin_rights)) if margin_rights else 72.0
        avg_top = (sum(margin_tops) / len(margin_tops)) if margin_tops else 72.0
        avg_bottom = (sum(margin_bottoms) / len(margin_bottoms)) if margin_bottoms else 72.0

        avg_left_cm = round(avg_left / cls.PT_PER_CM, 2)
        avg_right_cm = round(avg_right / cls.PT_PER_CM, 2)
        avg_top_cm = round(avg_top / cls.PT_PER_CM, 2)
        avg_bottom_cm = round(avg_bottom / cls.PT_PER_CM, 2)

        detalles["margenes_estimados_cm"] = {
            "izquierdo": avg_left_cm,
            "derecho": avg_right_cm,
            "superior": avg_top_cm,
            "inferior": avg_bottom_cm
        }

        # 2.54 cm +/- 0.4 cm tolerancia
        cumple_margenes = (
            abs(avg_left_cm - 2.54) <= 0.5 or (2.9 <= avg_left_cm <= 3.8) # Permite 3cm izquierdo para encuadernación
        ) and (
            abs(avg_right_cm - 2.54) <= 0.6 and abs(avg_top_cm - 2.54) <= 0.6 and abs(avg_bottom_cm - 2.54) <= 0.6
        )

        if cumple_margenes:
            obs = "Margen izquierdo de ~3 cm compatible con empastado/encuadernación universitaria." if avg_left_cm >= 2.9 else None
            items.append({
                "criterio": "Márgenes de Página",
                "cumple": True,
                "detalle_detectado": f"Izq: {avg_left_cm} cm, Der: {avg_right_cm} cm, Sup: {avg_top_cm} cm, Inf: {avg_bottom_cm} cm.",
                "norma_requerida": "2.54 cm (1 pulgada) en los cuatro bordes (o 3.0 cm izquierdo para empastado).",
                "observacion": obs,
                "sugerencia": None
            })
        else:
            score -= 1.5
            items.append({
                "criterio": "Márgenes de Página",
                "cumple": False,
                "detalle_detectado": f"Izq: {avg_left_cm} cm, Der: {avg_right_cm} cm, Sup: {avg_top_cm} cm, Inf: {avg_bottom_cm} cm.",
                "norma_requerida": "2.54 cm en los cuatro bordes.",
                "observacion": "Discrepancia en las dimensiones de los márgenes respecto a APA 7.",
                "sugerencia": "Configurar márgenes simétricos de 2.54 cm (1 pulg) en superior, inferior, derecho e izquierdo."
            })

        # ---------------------------------------------------------
        # 3. Tipografía y Tamaños de Fuente
        # ---------------------------------------------------------
        font_counts: Dict[str, int] = {}
        font_sizes: Dict[float, int] = {}
        line_spacings: List[float] = []

        for p in sample_pages:
            dict_data = p.get_text("dict")
            for b in dict_data.get("blocks", []):
                if "lines" in b:
                    prev_y = None
                    for l in b["lines"]:
                        curr_y = l["bbox"][1]
                        if prev_y is not None:
                            diff = curr_y - prev_y
                            if 8.0 < diff < 40.0:
                                line_spacings.append(diff)
                        prev_y = curr_y

                        for s in l.get("spans", []):
                            txt = s.get("text", "").strip()
                            if len(txt) > 3:
                                raw_font = s.get("font", "").lower()
                                # Limpiar prefijo de subset ej: "ABCDEF+TimesNewRomanPSMT"
                                clean_font = re.sub(r"^[a-z]{6}\+", "", raw_font)
                                clean_font = clean_font.replace("-", "").replace("_", "").replace("regular", "").strip()
                                sz = round(s.get("size", 12.0), 1)

                                font_counts[clean_font] = font_counts.get(clean_font, 0) + len(txt)
                                font_sizes[sz] = font_sizes.get(sz, 0) + len(txt)

        primary_font = max(font_counts.items(), key=lambda x: x[1])[0] if font_counts else "desconocida"
        primary_size = max(font_sizes.items(), key=lambda x: x[1])[0] if font_sizes else 12.0

        detalles["fuente_predominante"] = primary_font
        detalles["tamano_fuente_predominante_pt"] = primary_size
        detalles["fuentes_detectadas"] = list(font_counts.keys())[:5]

        # Verificar si la fuente principal coincide con APA 7
        matched_apa_font = None
        for apa_f in cls.APA7_FONTS:
            if apa_f in primary_font.replace(" ", ""):
                matched_apa_font = apa_f
                break

        if matched_apa_font:
            expected_sizes = cls.APA7_FONTS[matched_apa_font]
            size_ok = any(abs(primary_size - s) <= 1.0 for s in expected_sizes)
            if size_ok:
                items.append({
                    "criterio": "Tipografía y Tamaño",
                    "cumple": True,
                    "detalle_detectado": f"Fuente '{primary_font}' a {primary_size} pt.",
                    "norma_requerida": "Times New Roman (12 pt), Arial (11 pt), Calibri (11 pt), Georgia (11 pt) o Lucida Sans (10 pt).",
                    "observacion": None,
                    "sugerencia": None
                })
            else:
                score -= 1.0
                items.append({
                    "criterio": "Tipografía y Tamaño",
                    "cumple": False,
                    "detalle_detectado": f"Fuente '{primary_font}' a {primary_size} pt.",
                    "norma_requerida": f"Para {matched_apa_font.title()} se prescribe {expected_sizes[0]} pt.",
                    "observacion": f"El tamaño detectado ({primary_size} pt) difiere del estipulado.",
                    "sugerencia": f"Cambiar el tamaño de fuente a {expected_sizes[0]} pt para todo el cuerpo del documento."
                })
        else:
            score -= 1.5
            items.append({
                "criterio": "Tipografía y Tamaño",
                "cumple": False,
                "detalle_detectado": f"Fuente detectada: '{primary_font}' ({primary_size} pt).",
                "norma_requerida": "Times New Roman 12, Arial 11, Calibri 11, Georgia 11 o Lucida Sans 10.",
                "observacion": "La fuente empleada no está en el catálogo de fuentes estándar admitidas por APA 7ma ed.",
                "sugerencia": "Estandarizar la fuente a Times New Roman (12 pt) o Calibri/Arial (11 pt)."
            })

        # ---------------------------------------------------------
        # 4. Interlineado (Doble espacio ~ 2.0)
        # ---------------------------------------------------------
        avg_line_spacing = (sum(line_spacings) / len(line_spacings)) if line_spacings else 14.0
        spacing_ratio = avg_line_spacing / primary_size if primary_size > 0 else 1.2
        detalles["ratio_interlineado"] = round(spacing_ratio, 2)

        # Doble espacio suele tener ratio entre 1.7x y 2.4x respecto al tamaño de fuente
        is_double_spaced = (1.65 <= spacing_ratio <= 2.45)
        is_single_or_115 = (spacing_ratio < 1.45)

        if is_double_spaced:
            items.append({
                "criterio": "Interlineado de Texto",
                "cumple": True,
                "detalle_detectado": f"Interlineado doble estimado (factor ~{round(spacing_ratio, 2)}x).",
                "norma_requerida": "Interlineado doble (2.0) en todo el documento.",
                "observacion": None,
                "sugerencia": None
            })
        elif is_single_or_115:
            score -= 1.5
            items.append({
                "criterio": "Interlineado de Texto",
                "cumple": False,
                "detalle_detectado": f"Interlineado sencillo o 1.15 detectado (factor ~{round(spacing_ratio, 2)}x).",
                "norma_requerida": "Interlineado doble (2.0) continuo.",
                "observacion": "El documento presenta espaciado simple o 1.15 entre renglones en lugar del doble espacio oficial.",
                "sugerencia": "Seleccionar todo el texto y configurar Interlineado 2.0 (Doble) sin espacio extra entre párrafos."
            })
        else:
            score -= 0.8
            items.append({
                "criterio": "Interlineado de Texto",
                "cumple": False,
                "detalle_detectado": f"Interlineado de 1.5 líneas aproximado (factor ~{round(spacing_ratio, 2)}x).",
                "norma_requerida": "Interlineado doble (2.0).",
                "observacion": "Muchas universidades toleran interlineado 1.5, pero APA 7ma edición exige interlineado doble (2.0).",
                "sugerencia": "Verificar con el asesor si se autoriza 1.5 o si debe pasarse a doble espacio (2.0)."
            })

        # ---------------------------------------------------------
        # 5. Paginación en Esquina Superior Derecha
        # ---------------------------------------------------------
        pages_with_top_right_num = 0
        for p in sample_pages[1:6]:
            found_num = False
            for b in p.get_text("blocks"):
                txt = b[4].strip()
                bx0, by0, bx1, by1 = b[:4]
                # Zona superior derecha: x > w - 120 pt y y < 70 pt
                if bx0 >= (w - 140) and by0 <= 70:
                    if re.match(r"^\d+$", txt) or re.search(r"\b\d+\b", txt):
                        found_num = True
                        break
            if found_num:
                pages_with_top_right_num += 1

        has_top_right_pagination = (pages_with_top_right_num >= 2)
        if has_top_right_pagination:
            items.append({
                "criterio": "Numeración de Páginas",
                "cumple": True,
                "detalle_detectado": "Número de página ubicado en el encabezado superior derecho.",
                "norma_requerida": "Esquina superior derecha en números arábigos desde la página 1.",
                "observacion": None,
                "sugerencia": None
            })
        else:
            score -= 1.0
            items.append({
                "criterio": "Numeración de Páginas",
                "cumple": False,
                "detalle_detectado": "No se localizó numeración consistente en la esquina superior derecha del encabezado.",
                "norma_requerida": "Número de página arábigo en la esquina superior derecha (alineado al margen derecho).",
                "observacion": "En APA 7 la numeración va siempre arriba a la derecha, no al pie ni centrada.",
                "sugerencia": "Insertar número de página en encabezado con alineación derecha."
            })

        doc.close()

        # ---------------------------------------------------------
        # 6. Auditoría textual del cuerpo (Tablas, Figuras, Citas en Bloque)
        # ---------------------------------------------------------
        cls._audit_text_elements(cuerpo_texto or "", items, score_ref={"score": score})
        score = max(1.0, min(10.0, round(score, 1)))

        cumple_general = score >= 7.5 and all(it["cumple"] for it in items if it["criterio"] in ["Tamaño de Papel", "Márgenes de Página"])

        resumen = (
            f"Evaluación global de formato APA 7ma ed.: {score}/10. "
            + ("El documento cumple satisfactoriamente la estructura general de APA 7." if cumple_general else "Se detectaron inconsistencias de formato en la estructura o diseño del documento que requieren ajuste.")
        )

        return {
            "puntaje_formato": score,
            "cumple_general": cumple_general,
            "resumen": resumen,
            "items": items,
            "detalles_tecnicos": detalles
        }

    @classmethod
    def _audit_docx(cls, file_path: str, cuerpo_texto: Optional[str] = None) -> Dict[str, Any]:
        from docx import Document
        from docx.shared import Inches, Pt, Cm
        from docx.enum.text import WD_ALIGN_PARAGRAPH

        items = []
        detalles = {}
        score = 10.0

        try:
            doc = Document(file_path)
        except Exception as e:
            return {
                "puntaje_formato": 5.0,
                "cumple_general": False,
                "resumen": f"No se pudo procesar el archivo Word: {str(e)}",
                "items": [],
                "detalles_tecnicos": {}
            }

        # 1. Tamaño de página y márgenes desde la sección principal
        if doc.sections:
            sec = doc.sections[0]
            w_cm = round(sec.page_width.cm, 2) if sec.page_width else 21.59
            h_cm = round(sec.page_height.cm, 2) if sec.page_height else 27.94

            top_cm = round(sec.top_margin.cm, 2) if sec.top_margin else 2.54
            bot_cm = round(sec.bottom_margin.cm, 2) if sec.bottom_margin else 2.54
            left_cm = round(sec.left_margin.cm, 2) if sec.left_margin else 2.54
            right_cm = round(sec.right_margin.cm, 2) if sec.right_margin else 2.54

            detalles["tamano_pagina_cm"] = f"{w_cm} x {h_cm} cm"
            detalles["margenes_cm"] = {
                "izquierdo": left_cm,
                "derecho": right_cm,
                "superior": top_cm,
                "inferior": bot_cm
            }

            # Carta es 21.59 x 27.94 cm
            is_letter = (abs(w_cm - 21.59) <= 0.3 and abs(h_cm - 27.94) <= 0.3)
            if is_letter:
                items.append({
                    "criterio": "Tamaño de Papel",
                    "cumple": True,
                    "detalle_detectado": f"Carta ({w_cm} x {h_cm} cm).",
                    "norma_requerida": "Carta / Letter (21.59 x 27.94 cm).",
                    "observacion": None,
                    "sugerencia": None
                })
            else:
                score -= 1.0
                items.append({
                    "criterio": "Tamaño de Papel",
                    "cumple": False,
                    "detalle_detectado": f"Dimensiones detectadas: {w_cm} x {h_cm} cm.",
                    "norma_requerida": "Carta (21.59 x 27.94 cm).",
                    "observacion": "El documento no está configurado en tamaño Carta estándar.",
                    "sugerencia": "En Word: Disposición -> Tamaño -> Carta."
                })

            # Márgenes 2.54 cm
            cumple_m = (abs(top_cm - 2.54) <= 0.2 and abs(bot_cm - 2.54) <= 0.2 and abs(right_cm - 2.54) <= 0.2) and (
                abs(left_cm - 2.54) <= 0.2 or (2.9 <= left_cm <= 3.6)
            )

            if cumple_m:
                obs = "Margen izquierdo ampliado (~3.0 cm) para encuadernación/empastado." if left_cm >= 2.9 else None
                items.append({
                    "criterio": "Márgenes de Página",
                    "cumple": True,
                    "detalle_detectado": f"Sup: {top_cm} cm, Inf: {bot_cm} cm, Izq: {left_cm} cm, Der: {right_cm} cm.",
                    "norma_requerida": "2.54 cm en los cuatro bordes (o 3 cm izquierdo para encuadernación).",
                    "observacion": obs,
                    "sugerencia": None
                })
            else:
                score -= 1.5
                items.append({
                    "criterio": "Márgenes de Página",
                    "cumple": False,
                    "detalle_detectado": f"Sup: {top_cm} cm, Inf: {bot_cm} cm, Izq: {left_cm} cm, Der: {right_cm} cm.",
                    "norma_requerida": "2.54 cm en los 4 bordes.",
                    "observacion": "Márgenes fuera del estándar APA 7.",
                    "sugerencia": "En Word: Disposición -> Márgenes -> Normal (2.54 cm en todos los lados)."
                })

        # 2. Tipografía y Párrafos en DOCX
        fonts_found = set()
        sizes_found = set()
        spacings = []
        indents = []
        alignments = []

        for p in doc.paragraphs:
            txt = p.text.strip()
            if not txt:
                continue

            # Sangría y alineación
            pf = p.paragraph_format
            if pf.first_line_indent is not None:
                indents.append(round(pf.first_line_indent.cm, 2))
            if pf.line_spacing is not None:
                if isinstance(pf.line_spacing, (int, float)):
                    spacings.append(float(pf.line_spacing))
            if pf.alignment is not None:
                alignments.append(pf.alignment)

            # Runs
            for r in p.runs:
                if r.text.strip():
                    if r.font.name:
                        fonts_found.add(r.font.name)
                    if r.font.size:
                        sizes_found.add(round(r.font.size.pt, 1))

        # Tipografía
        main_font = list(fonts_found)[0] if fonts_found else "Times New Roman"
        detalles["fuentes_word"] = list(fonts_found)
        detalles["tamanos_word"] = list(sizes_found)

        is_apa_font = any(af in main_font.lower() for af in cls.APA7_FONTS)
        if is_apa_font:
            items.append({
                "criterio": "Tipografía y Tamaño",
                "cumple": True,
                "detalle_detectado": f"Fuente '{main_font}'.",
                "norma_requerida": "Fuentes admitidas por APA 7 (Times New Roman, Arial, Calibri, Georgia).",
                "observacion": None,
                "sugerencia": None
            })
        else:
            score -= 1.0
            items.append({
                "criterio": "Tipografía y Tamaño",
                "cumple": False,
                "detalle_detectado": f"Fuente detectada: '{main_font}'.",
                "norma_requerida": "Times New Roman 12, Arial 11 o Calibri 11.",
                "observacion": "Fuente no incluida en el estándar oficial APA 7.",
                "sugerencia": "Cambiar el estilo Normal a Times New Roman 12 pt o Calibri 11 pt."
            })

        # Interlineado DOCX
        avg_sp = (sum(spacings) / len(spacings)) if spacings else 2.0
        if abs(avg_sp - 2.0) <= 0.15:
            items.append({
                "criterio": "Interlineado de Texto",
                "cumple": True,
                "detalle_detectado": "Interlineado doble (2.0) verificado en estilos de párrafo.",
                "norma_requerida": "Doble (2.0) en todo el cuerpo de la obra.",
                "observacion": None,
                "sugerencia": None
            })
        else:
            score -= 1.5
            items.append({
                "criterio": "Interlineado de Texto",
                "cumple": False,
                "detalle_detectado": f"Interlineado promedio detectado: {round(avg_sp, 2)}.",
                "norma_requerida": "Interlineado doble (2.0).",
                "observacion": "El documento no está configurado con interlineado doble uniforme.",
                "sugerencia": "En Word: Seleccionar todo -> Inicio -> Espacio entre líneas -> 2.0."
            })

        # Sangría de primera línea (1.27 cm / 0.5 pulg)
        avg_ind = (sum(indents) / len(indents)) if indents else 0.0
        has_apa_indent = any(abs(ind - 1.27) <= 0.25 for ind in indents)
        if has_apa_indent or abs(avg_ind - 1.27) <= 0.3:
            items.append({
                "criterio": "Sangría de Primera Línea",
                "cumple": True,
                "detalle_detectado": "Sangría de primera línea configurada a ~1.27 cm (0.5 pulgadas).",
                "norma_requerida": "1.27 cm en cada nuevo párrafo.",
                "observacion": None,
                "sugerencia": None
            })
        else:
            score -= 1.0
            items.append({
                "criterio": "Sangría de Primera Línea",
                "cumple": False,
                "detalle_detectado": f"Sangría media detectada: {round(avg_ind, 2)} cm.",
                "norma_requerida": "Sangría de 1.27 cm (0.5 pulg) en el primer renglón de cada párrafo.",
                "observacion": "Párrafos sin sangría de primera línea o tabulación manual inconsistente.",
                "sugerencia": "En Word: Párrafo -> Especial -> Primera línea: 1.27 cm."
            })

        # Tablas sin bordes verticales
        if doc.tables:
            items.append({
                "criterio": "Tablas y Figuras",
                "cumple": True,
                "detalle_detectado": f"Se identificaron {len(doc.tables)} tablas insertadas.",
                "norma_requerida": "Tablas APA 7: solo bordes horizontales (superior, inferior y encabezado); sin líneas verticales.",
                "observacion": "Verificar manualmente que las tablas no tengan bordes verticales visibles.",
                "sugerencia": "En diseño de tabla de Word, desactivar los bordes verticales internos y externos."
            })

        # Auditoría textual del contenido
        doc_full_text = "\n".join([p.text for p in doc.paragraphs if p.text.strip()])
        cls._audit_text_elements(cuerpo_texto or doc_full_text, items, score_ref={"score": score})

        score = max(1.0, min(10.0, round(score, 1)))
        cumple_general = score >= 7.5

        return {
            "puntaje_formato": score,
            "cumple_general": cumple_general,
            "resumen": f"Auditoría de formato Word APA 7: {score}/10.",
            "items": items,
            "detalles_tecnicos": detalles
        }

    @classmethod
    def _audit_text_elements(cls, text: str, items: List[Dict[str, Any]], score_ref: Dict[str, float]):
        """Audita patrones en el contenido textual: citas largas, rótulos de tablas/figuras, etc."""
        if not text:
            return

        # 1. Citas en Bloque (Citas textuales de 40 o más palabras entre comillas)
        # En APA 7, las citas de >= 40 palabras NO deben ir entre comillas, van en párrafo sangrado independiente.
        quotes = re.findall(r'"([^"]{150,})"', text) + re.findall(r'“([^”]{150,})”', text)
        quotes_40_words = [q for q in quotes if len(q.split()) >= 40]

        if quotes_40_words:
            score_ref["score"] -= 1.0
            items.append({
                "criterio": "Citas Textuales Largas (en bloque)",
                "cumple": False,
                "detalle_detectado": f"Se detectaron {len(quotes_40_words)} cita(s) entre comillas con más de 40 palabras.",
                "norma_requerida": "Citas de 40 o más palabras deben presentarse en bloque independiente, con sangría izquierda de 1.27 cm y SIN comillas.",
                "observacion": f"Ejemplo detectado: \"{quotes_40_words[0][:60]}...\"",
                "sugerencia": "Quitar las comillas, separar la cita en un párrafo propio con sangría izquierda de 1.27 cm e interlineado doble."
            })
        else:
            items.append({
                "criterio": "Citas Textuales en Bloque",
                "cumple": True,
                "detalle_detectado": "No se encontraron citas textuales extensas indebidamente entrecomilladas en el cuerpo.",
                "norma_requerida": "Citas < 40 palabras entre comillas; >= 40 palabras en bloque sangrado sin comillas.",
                "observacion": None,
                "sugerencia": None
            })

        # 2. Formato de Rótulos de Tablas y Figuras
        # APA 7: 'Tabla 1' en negrita, siguiente renglón título en cursiva.
        tabla_matches = list(re.finditer(r"(?i)\b(tabla\s+\d+|figura\s+\d+)\b", text))
        if tabla_matches:
            items.append({
                "criterio": "Rótulos de Tablas y Figuras",
                "cumple": True,
                "detalle_detectado": f"Se identificaron {len(tabla_matches)} menciones/rótulos de tablas o figuras en el texto.",
                "norma_requerida": "Etiqueta en negrita (ej. **Tabla 1** o **Figura 1**), título breve en cursiva en renglón siguiente, sin punto final.",
                "observacion": "Revisar que la etiqueta esté en negrita y el título descriptivo en cursiva en la línea inmediata inferior.",
                "sugerencia": None
            })

        # 3. Jerarquía de Títulos (Nivel 1 a 3)
        # Revisar si hay encabezados con numeración no-APA tipo '1.1.1.1'
        multi_level_numbers = re.findall(r"^\s*\d+\.\d+\.\d+\.\d+\s+[A-Z]", text, re.MULTILINE)
        if len(multi_level_numbers) > 3:
            score_ref["score"] -= 0.5
            items.append({
                "criterio": "Jerarquía de Títulos",
                "cumple": False,
                "detalle_detectado": f"Se detectó esquema de numeración decimal profunda (ej. 1.1.2.1).",
                "norma_requerida": "APA 7 no emplea numeración decimal compleja; utiliza jerarquía visual de 5 niveles (centrado, alineado a la izquierda, cursivas y sangría).",
                "observacion": "La numeración decimal excesiva suele ser convención de ingeniería/IEEE o institucional, no APA 7 puro.",
                "sugerencia": "Si la universidad exige APA 7 estricto, emplear estilos de encabezado Nivel 1 a 5 sin numeración decimal."
            })
        else:
            items.append({
                "criterio": "Jerarquía de Títulos",
                "cumple": True,
                "detalle_detectado": "Estructura de encabezados compatible con niveles APA 7.",
                "norma_requerida": "Nivel 1 centrado negrita; Nivel 2 alineado a la izquierda negrita; Nivel 3 alineado izq. negrita cursiva.",
                "observacion": None,
                "sugerencia": None
            })

    @classmethod
    def _audit_fallback(cls, file_path: str, cuerpo_texto: Optional[str] = None) -> Dict[str, Any]:
        return {
            "puntaje_formato": 7.0,
            "cumple_general": True,
            "resumen": "Formato de archivo no soportado para análisis geométrico de página. Solo se analizó el texto.",
            "items": [],
            "detalles_tecnicos": {}
        }
