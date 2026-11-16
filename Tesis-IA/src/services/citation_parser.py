import re
from typing import List, Dict, Any, Optional, Tuple

class CitationParser:
    """Detecta y desglosa citas individuales en formatos estándar (APA 7, IEEE, etc.)."""

    # Expresión regular para extraer DOIs (ej. 10.1000/182 o https://doi.org/10.xxxx/yyyy)
    DOI_REGEX = re.compile(r"(?:https?://(?:dx\.)?doi\.org/|doi:\s*)?(10\.\d{4,9}/[-._;()/:A-Za-z0-9]+)", re.IGNORECASE)
    
    # Expresión para año entre paréntesis o suelto (19xx o 20xx)
    YEAR_REGEX = re.compile(r"\b(19\d{2}|20\d{2})\b")

    @classmethod
    def parse_references_block(cls, references_text: str) -> List[Dict[str, Any]]:
        """Divide el bloque de bibliografía en referencias individuales y extrae metadatos,
        manejando formatos estándar (APA 7, IEEE, etc.) tanto en tesis monolíticas como estructuradas por capítulos."""
        if not references_text.strip():
            return []

        # 1. Separar por párrafos o líneas numeradas
        raw_entries = cls._split_into_entries(references_text)
        
        parsed_citations = []
        for raw in raw_entries:
            cleaned = " ".join(raw.split())  # Quitar saltos de línea intermedios
            if len(cleaned) < 15:  # Descartar basura o encabezados residuales
                continue
            
            parsed = cls._parse_single_citation(cleaned)
            parsed_citations.append(parsed)

        # 2. Si dio pocas citas pero hay mucho texto (común en referencias por capítulos o sangría francesa sin números)
        if len(parsed_citations) < 10 and len(references_text) > 1500:
            lines = [l.strip() for l in references_text.split("\n") if l.strip()]
            bloques = []
            bloque_actual = []
            
            for line in lines:
                es_inicio = bool(
                    re.search(r"^[A-ZÁÉÍÓÚ\s]{3,},\s*[A-ZÁÉÍÓÚ]\.?", line) or
                    re.search(r"^\[\d+\]", line) or
                    re.search(r"^\d+\.\s+[A-Z]", line) or
                    re.search(r"^[A-ZÁÉÍÓÚ][a-zA-ZÁÉÍÓÚáéíóúñÑ]+,\s*[A-Z]\.\s*\([12]\d{3}\)", line) or
                    re.search(r"^[A-ZÁÉÍÓÚ][a-zA-ZÁÉÍÓÚáéíóúñÑ\s\.,&]+?\((?:19|20)\d{2}\)", line)
                )
                if es_inicio and bloque_actual:
                    texto = " ".join(bloque_actual).strip()
                    if len(texto) >= 20 and not re.search(r"(?i)^(referencias|bibliograf[ií]a)", texto):
                        bloques.append(texto)
                    bloque_actual = [line]
                else:
                    bloque_actual.append(line)
                    
            if bloque_actual:
                texto = " ".join(bloque_actual).strip()
                if len(texto) >= 20:
                    bloques.append(texto)
                    
            if len(bloques) > len(parsed_citations):
                return [cls._parse_single_citation(b) for b in bloques]

        return parsed_citations

    @classmethod
    def _split_into_entries(cls, text: str) -> List[str]:
        # 1. Si tiene formato numérico tipo IEEE [1], [2] o 1. 2.
        if re.search(r"^\[\d+\]|\n\[\d+\]|^\d+\.\s", text, re.MULTILINE):
            entries = re.split(r"(?:(?<=\n)|(?<=^))(?:\[\d+\]|\d+\.)\s+", text)
            return [e.strip() for e in entries if e.strip()]

        # 2. Si tiene saltos dobles explícitos
        if "\n\n" in text:
            entries = re.split(r"\n\s*\n", text)
            if len(entries) > 1:
                return [e.strip() for e in entries if e.strip()]

        # 3. Separación por líneas donde cada cita inicia con Autor(es) y (Año)
        lines = [l.strip() for l in text.split("\n") if l.strip()]
        entries = []
        current = []
        for line in lines:
            # Detecta patrones típicos de inicio de cita APA: Autor(es)... (YYYY) o Apellido, Inicial.
            is_start = bool(
                re.search(r"^\s*[A-ZÁÉÍÓÚ][a-zA-ZÁÉÍÓÚáéíóúñÑ\s\.,&]+?\((?:19|20)\d{2}\)", line)
                or re.search(r"^[A-ZÁÉÍÓÚ][a-zA-ZÁÉÍÓÚáéíóúñÑ\s]+,\s*[A-ZÁÉÍÓÚ]\.", line)
            )
            if is_start and current:
                entries.append(" ".join(current))
                current = [line]
            else:
                current.append(line)
        if current:
            entries.append(" ".join(current))

        return [e.strip() for e in entries if e.strip()]

    @classmethod
    def _parse_single_citation(cls, citation_text: str) -> Dict[str, Any]:
        # Buscar DOI si está explícito
        doi_match = cls.DOI_REGEX.search(citation_text)
        doi = doi_match.group(1).rstrip(".") if doi_match else None

        # Buscar Año
        year_match = cls.YEAR_REGEX.search(citation_text)
        year = int(year_match.group(1)) if year_match else None

        # Intentar extraer primer autor (generalmente antes del año o primera coma)
        autor_estimado = None
        autor_match = re.match(r"^([A-ZÁÉÍÓÚ][a-záéíóú]+(?:,\s*[A-ZÁÉÍÓÚ]\.?)?)", citation_text)
        if autor_match:
            autor_estimado = autor_match.group(1).split(",")[0].strip()

        # Intentar extraer título probable
        titulo_estimado = cls._estimate_title(citation_text, year)

        return {
            "texto_completo": citation_text,
            "doi": doi,
            "anio": year,
            "autor_principal": autor_estimado,
            "titulo_estimado": titulo_estimado
        }

    @classmethod
    def audit_apa7_reference(cls, citation_text: str) -> Tuple[bool, Optional[str]]:
        """Verifica si una referencia individual cumple los lineamientos de APA 7ma edición."""
        text = citation_text.strip()
        observaciones = []

        # 1. No numeración tipo IEEE / Vancouver [1] o 1.
        if re.search(r"^\[\d+\]|^\d+\.\s+", text):
            observaciones.append("Las referencias en APA 7ª ed. no deben numerarse ([1], 1.); deben ordenarse alfabéticamente con sangría francesa.")

        # 2. Supresión del lugar de publicación (Ciudad/País: Editorial era APA 6)
        # Ejemplo: "México, D.F.: McGraw-Hill" o "New York, NY: Springer" o "Madrid: Editorial"
        if re.search(r"\b[A-ZÁÉÍÓÚ][a-záéíóúñ]+(?:\s*,\s*[A-ZÁÉÍÓÚ\.]+)?\s*:\s*[A-ZÁÉÍÓÚ]", text):
            observaciones.append("En APA 7ª ed. se eliminó el lugar geográfico de publicación (ciudad/país). Solo debe especificarse el nombre de la editorial.")

        # 3. Supresión de 'Recuperado de' / 'Retrieved from'
        if re.search(r"(?i)\b(recuperado\s+de|retrieved\s+from)\b", text):
            observaciones.append("En APA 7ª ed. se omitió la etiqueta 'Recuperado de' antes del enlace URL o DOI.")

        # 4. Formato estandarizado de DOI (https://doi.org/10.xxxx)
        if re.search(r"(?i)\bdoi:\s*10\.", text) or re.search(r"http://dx\.doi\.org", text):
            observaciones.append("En APA 7ª ed. el DOI debe presentarse como enlace seguro estándar 'https://doi.org/10.xxxx', sin el prefijo 'doi:'.")

        # 5. Año entre paréntesis (19xx o 20xx)
        if not re.search(r"\((?:19|20)\d{2}[a-z]?\)", text):
            observaciones.append("El año de publicación debe delimitarse entre paréntesis, por ejemplo: (2023).")

        # 6. Formato de autor inicial
        if re.search(r"^[A-ZÁÉÍÓÚ][a-z]+\s+[A-ZÁÉÍÓÚ][a-z]+(?:\s+[A-ZÁÉÍÓÚ][a-z]+)?\s*\(", text):
            observaciones.append("Los autores deben figurar como Apellido, Inicial. (ej. 'Gómez, J. A.'), no con nombres de pila completos.")

        if observaciones:
            return False, " | ".join(observaciones)
        return True, None

    @classmethod
    def _estimate_title(cls, text: str, year: Optional[int]) -> str:
        """Intenta aislar el título de la obra."""
        # En APA suele ser: Autor (Año). Título de la obra. Revista o Editorial.
        if year:
            parts = re.split(rf"\({year}[a-z]?\)\.?", text)
            if len(parts) > 1:
                after_year = parts[1].strip()
                title_parts = after_year.split(".")
                if title_parts and len(title_parts[0].strip()) > 5:
                    return title_parts[0].strip()

        # Si el año no estaba entre paréntesis (ej. Goodfellow et al. Generative adversarial nets... 2014)
        # Separar por puntos que NO formen parte de iniciales como "A.", "J.", "M."
        segments = [s.strip() for s in re.split(r"(?<![A-ZÁÉÍÓÚ])\.\s+", text) if len(s.strip()) > 5]
        if len(segments) > 1:
            return segments[1]

        # Si no, devolvemos un extracto limpio
        words = text.split()
        return " ".join(words[:12])  # Primeras palabras clave para búsqueda
