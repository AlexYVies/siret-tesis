import os
import re
from typing import Dict, Any, List, Tuple
from pathlib import Path
import fitz  # PyMuPDF
from docx import Document

class DocumentExtractor:
    """Extrae texto, capítulos y la sección de bibliografía/referencias."""

    BIBLIOGRAPHY_HEADERS = [
        r"referencias\s+bibliogr[aá]ficas",
        r"referencias",
        r"bibliograf[ií]a",
        r"fuentes\s+consultadas",
        r"literatura\s+citada",
        r"references",
        r"bibliography"
    ]

    @classmethod
    def extract(cls, file_path: str) -> Dict[str, Any]:
        path = Path(file_path)
        ext = path.suffix.lower()

        if ext == ".pdf":
            return cls._extract_from_pdf(file_path)
        elif ext in [".docx", ".doc"]:
            return cls._extract_from_docx(file_path)
        else:
            raise ValueError(f"Formato no soportado: {ext}. Solo se admite PDF o DOCX.")

    @classmethod
    def _extract_from_pdf(cls, file_path: str) -> Dict[str, Any]:
        doc = fitz.open(file_path)
        total_pages = len(doc)
        full_text_pages = []

        for page_num in range(total_pages):
            page = doc.load_page(page_num)
            text = page.get_text("text")
            full_text_pages.append((page_num + 1, text))

        doc.close()
        return cls._split_body_and_references(full_text_pages)

    @classmethod
    def _extract_from_docx(cls, file_path: str) -> Dict[str, Any]:
        doc = Document(file_path)
        full_text_pages = []
        current_page_text = []

        # En DOCX no hay paginación fija exacta, agrupamos por párrafos
        for p in doc.paragraphs:
            text = p.text.strip()
            if text:
                current_page_text.append(text)

        full_text_pages.append((1, "\n".join(current_page_text)))
        return cls._split_body_and_references(full_text_pages)

    @classmethod
    def _split_body_and_references(cls, pages_data: List[Tuple[int, str]]) -> Dict[str, Any]:
        """Localiza dónde empieza la bibliografía y separa el cuerpo de las citas,
        soportando tanto tesis con bibliografía global final como tesis con referencias al final de cada capítulo."""
        combined_text = "\n\n".join([text for _, text in pages_data])
        
        # Patrón para localizar el inicio de las referencias
        pattern = re.compile(
            rf"^(?:(?:\d+\.?\s*)?(?:{'|'.join(cls.BIBLIOGRAPHY_HEADERS)}))\s*$",
            re.IGNORECASE | re.MULTILINE
        )

        matches = list(pattern.finditer(combined_text))
        
        cuerpo_texto = combined_text
        bibliografia_texto = ""

        # Si hay múltiples secciones de referencias (referencias por capítulo o caso clínico)
        if len(matches) > 1 and len(pages_data) > 1:
            ref_blocks = []
            body_blocks = []
            i = 0
            while i < len(pages_data):
                p_num, p_text = pages_data[i]
                m = pattern.search(p_text)
                if m:
                    antes = p_text[:m.start()].strip()
                    if antes:
                        body_blocks.append(antes)
                    
                    ref_text = p_text[m.end():].strip()
                    while i + 1 < len(pages_data):
                        next_num, next_text = pages_data[i + 1]
                        # Si la siguiente página inicia un nuevo capítulo, caso clínico o sección, detener el bloque
                        if re.search(r"(?i)^\s*(?:cap[ií]tulo|caso\s+cl[ií]nico|resumen|abstract|introducci[oó]n|\d+\.\s+[A-Z])", next_text[:200], re.MULTILINE):
                            break
                        if pattern.search(next_text):
                            break
                        # Si contiene patrones típicos de citas bibliográficas, anexarla a la bibliografía
                        if re.search(r"\((?:19|20)\d{2}\)|https?://|doi:", next_text):
                            ref_text += "\n" + next_text.strip()
                            i += 1
                        else:
                            break
                    if ref_text:
                        ref_blocks.append(ref_text)
                else:
                    body_blocks.append(p_text)
                i += 1

            if ref_blocks:
                bibliografia_texto = "\n\n".join(ref_blocks)
                cuerpo_texto = "\n\n".join(body_blocks)

        elif matches:
            # Una sola coincidencia principal (bibliografía global al final)
            last_match = matches[-1]
            inicio_bib_index = last_match.start()
            cuerpo_texto = combined_text[:inicio_bib_index].strip()
            bibliografia_texto = combined_text[last_match.end():].strip()

        return {
            "cuerpo_texto": cuerpo_texto,
            "bibliografia_texto": bibliografia_texto,
            "total_caracteres": len(combined_text),
            "tiene_seccion_referencias": bool(bibliografia_texto)
        }
