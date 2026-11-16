import hashlib
import json
import asyncio
import re
from typing import Dict, Any, List, Optional
import httpx
from src.core.config import settings
from src.db.database import get_db_connection
from src.models.schemas import CitaAuditada, EstadoCita

class CitationVerifier:
    """Verifica citas contra Crossref (Polite Pool) y Semantic Scholar con caché local."""

    CROSSREF_URL = "https://api.crossref.org/works"
    SEMANTIC_URL = "https://api.semanticscholar.org/graph/v1/paper/search"

    @classmethod
    def _hash_citation(cls, text: str) -> str:
        return hashlib.sha256(text.strip().lower().encode("utf-8")).hexdigest()

    @classmethod
    async def verify_citations(cls, citations: List[Dict[str, Any]]) -> List[CitaAuditada]:
        results: List[CitaAuditada] = []
        semaphore = asyncio.Semaphore(settings.MAX_CONCURRENT_CITATION_FETCHES)

        async with httpx.AsyncClient(timeout=15.0) as client:
            tasks = [cls._verify_single(citation, client, semaphore) for citation in citations]
            results = await asyncio.gather(*tasks)

        return results

    @classmethod
    async def _verify_single(
        cls, citation: Dict[str, Any], client: httpx.AsyncClient, semaphore: asyncio.Semaphore
    ) -> CitaAuditada:
        text = citation["texto_completo"]
        doi = citation.get("doi")
        clave_hash = cls._hash_citation(text)

        # 1. Checar en Caché Local SQLite
        cached = await cls._check_cache(clave_hash)
        if cached:
            return CitaAuditada(**cached)

        # 2. Si no está en caché, consultar APIs con control de semáforo
        async with semaphore:
            result = None
            # Estrategia A: Si ya tiene DOI, consultar directo a Crossref por DOI
            if doi:
                result = await cls._check_crossref_by_doi(doi, text, client)

            # Estrategia B: Búsqueda bibliográfica en Crossref
            if not result:
                result = await cls._search_crossref(citation, client)

            # Estrategia C: Búsqueda de respaldo en Semantic Scholar
            if not result or result.estado_verificacion == EstadoCita.SOSPECHOSA_NO_ENCONTRADA:
                result_semantic = await cls._search_semantic_scholar(citation, client)
                if result_semantic and result_semantic.estado_verificacion == EstadoCita.VERIFICADA:
                    result = result_semantic

            # Estrategia D: Búsqueda de Libros Físicos / Editoriales sin DOI en OpenLibrary
            if not result or result.estado_verificacion == EstadoCita.SOSPECHOSA_NO_ENCONTRADA:
                result_book = await cls._search_openlibrary_book(citation, client)
                if result_book:
                    result = result_book

            # Si de plano no se encontró nada
            if not result:
                result = CitaAuditada(
                    cita_original=text,
                    estado_verificacion=EstadoCita.SOSPECHOSA_NO_ENCONTRADA,
                    motivo="No se encontró registro del documento en Crossref, Semantic Scholar ni en el catálogo de libros de OpenLibrary."
                )

            # 3. Guardar en Caché Local
            await cls._save_to_cache(clave_hash, text, result.model_dump())
            return result

    OPENALEX_WORKS_URL = "https://api.openalex.org/works"

    @classmethod
    async def _fetch_abstract_openalex(cls, doi: Optional[str], title: Optional[str], client: httpx.AsyncClient) -> Optional[str]:
        """Obtiene el abstract del paper de forma gratuita desde OpenAlex."""
        headers = {"User-Agent": f"mailto:{settings.CROSSREF_MAILTO}"}
        # Intento 1: Por DOI si existe
        if doi:
            try:
                resp = await client.get(f"{cls.OPENALEX_WORKS_URL}/https://doi.org/{doi}", headers=headers, timeout=6.0)
                if resp.status_code == 200:
                    inv = resp.json().get("abstract_inverted_index") or {}
                    if inv:
                        words = sorted([(pos, word) for word, positions in inv.items() for pos in positions])
                        return " ".join([w for _, w in words])
            except Exception:
                pass

        # Intento 2: Por Título (para preprints de arXiv o conferencias)
        if title:
            try:
                # Quitar caracteres especiales para OpenAlex search filter
                clean_title = re.sub(r"[^\w\s]", " ", title)
                params = {"filter": f"title.search:{clean_title[:100]}", "per_page": 1}
                resp = await client.get(cls.OPENALEX_WORKS_URL, params=params, headers=headers, timeout=6.0)
                if resp.status_code == 200:
                    results = resp.json().get("results", [])
                    if results:
                        inv = results[0].get("abstract_inverted_index") or {}
                        if inv:
                            words = sorted([(pos, word) for word, positions in inv.items() for pos in positions])
                            return " ".join([w for _, w in words])
            except Exception:
                pass

        return None

    @classmethod
    async def _check_crossref_by_doi(
        cls, doi: str, original_text: str, client: httpx.AsyncClient
    ) -> Optional[CitaAuditada]:
        url = f"{cls.CROSSREF_URL}/{doi}"
        headers = {"User-Agent": f"TesisAuditorBot/1.0 (mailto:{settings.CROSSREF_MAILTO})"}
        try:
            resp = await client.get(url, headers=headers)
            if resp.status_code == 200:
                item = resp.json().get("message", {})
                title = item.get("title", [""])[0] if item.get("title") else "Sin título"
                year = None
                date_parts = item.get("published", {}).get("date-parts", [[]])[0]
                if date_parts:
                    year = date_parts[0]

                abstract = await cls._fetch_abstract_openalex(doi, title, client)

                return CitaAuditada(
                    cita_original=original_text,
                    estado_verificacion=EstadoCita.VERIFICADA,
                    doi=doi,
                    titulo_encontrado=title,
                    autor_detectado=None,
                    fuente_verificacion="Crossref (DOI)",
                    coincidencia_autores=True,
                    anio_encontrado=year,
                    abstract_encontrado=abstract
                )
        except Exception:
            pass
        return None

    @classmethod
    async def _search_crossref(
        cls, citation: Dict[str, Any], client: httpx.AsyncClient
    ) -> Optional[CitaAuditada]:
        headers = {"User-Agent": f"TesisAuditorBot/1.0 (mailto:{settings.CROSSREF_MAILTO})"}
        query = citation.get("titulo_estimado") or citation["texto_completo"][:100]
        params = {"query.bibliographic": query, "rows": 1}

        try:
            resp = await client.get(cls.CROSSREF_URL, params=params, headers=headers)
            if resp.status_code == 200:
                items = resp.json().get("message", {}).get("items", [])
                if items:
                    item = items[0]
                    title_list = item.get("title")
                    if not title_list or not isinstance(title_list, list) or len(title_list) == 0:
                        return None
                    
                    found_title = title_list[0] or ""
                    if not found_title.strip():
                        return None

                    # Validación de coincidencia léxica
                    query_words = set(re.findall(r"\w{4,}", query.lower()))
                    found_words = set(re.findall(r"\w{4,}", found_title.lower()))

                    if not query_words:
                        return None

                    overlap = query_words.intersection(found_words)
                    similarity = len(overlap) / len(query_words)

                    if similarity >= 0.35:
                        doi = item.get("DOI")
                        year = None
                        date_parts = item.get("published", {}).get("date-parts", [[]])[0]
                        if date_parts:
                            year = date_parts[0]

                        abstract = await cls._fetch_abstract_openalex(doi, found_title, client)

                        return CitaAuditada(
                            cita_original=citation["texto_completo"],
                            estado_verificacion=EstadoCita.VERIFICADA,
                            doi=doi,
                            titulo_encontrado=found_title,
                            autor_detectado=citation.get("autor_principal"),
                            fuente_verificacion="Crossref",
                            coincidencia_autores=True,
                            anio_encontrado=year,
                            abstract_encontrado=abstract
                        )
        except Exception:
            pass
        return None

    @classmethod
    async def _search_semantic_scholar(
        cls, citation: Dict[str, Any], client: httpx.AsyncClient
    ) -> Optional[CitaAuditada]:
        query = citation.get("titulo_estimado") or citation["texto_completo"][:100]
        params = {"query": query, "limit": 1, "fields": "title,year,externalIds,authors"}
        try:
            resp = await client.get(cls.SEMANTIC_URL, params=params)
            if resp.status_code == 200:
                data = resp.json().get("data", [])
                if data:
                    item = data[0]
                    title = item.get("title", "")
                    if not title:
                        return None

                    query_words = set(re.findall(r"\w{4,}", query.lower()))
                    found_words = set(re.findall(r"\w{4,}", title.lower()))
                    if not query_words:
                        return None

                    overlap = query_words.intersection(found_words)
                    if len(overlap) / len(query_words) >= 0.35:
                        year = item.get("year")
                        doi = item.get("externalIds", {}).get("DOI")
                        return CitaAuditada(
                            cita_original=citation["texto_completo"],
                            estado_verificacion=EstadoCita.VERIFICADA,
                            doi=doi,
                            titulo_encontrado=title,
                            autor_detectado=citation.get("autor_principal"),
                            fuente_verificacion="Semantic Scholar",
                            coincidencia_autores=True,
                            anio_encontrado=year
                        )
        except Exception:
            pass
        return None

    @classmethod
    async def _search_openlibrary_book(
        cls, citation: Dict[str, Any], client: httpx.AsyncClient
    ) -> Optional[CitaAuditada]:
        """Busca libros físicos o clásicos que no tienen DOI en OpenLibrary."""
        query_title = citation.get("titulo_estimado")
        if not query_title or len(query_title.strip()) < 5:
            query_title = citation["texto_completo"][:80]

        # Aislar título principal si incluye subtítulo ("Título: Subtítulo")
        main_title = query_title.split(":")[0].strip()

        author = citation.get("autor_principal")
        params = {"title": main_title, "limit": 3}
        if author:
            params["author"] = author

        try:
            resp = await client.get("https://openlibrary.org/search.json", params=params, timeout=12.0)
            if resp.status_code == 200:
                docs = resp.json().get("docs", [])
                if docs:
                    doc = docs[0]
                    found_title = doc.get("title", "")
                    if not found_title:
                        return None

                    query_words = set(re.findall(r"\w{3,}", main_title.lower()))
                    found_words = set(re.findall(r"\w{3,}", found_title.lower()))
                    if not query_words or not found_words:
                        return None

                    overlap = query_words.intersection(found_words)
                    min_words = min(len(query_words), len(found_words))
                    similarity = len(overlap) / max(1, min_words)

                    if similarity >= 0.50:
                        first_year = doc.get("first_publish_year")
                        return CitaAuditada(
                            cita_original=citation["texto_completo"],
                            estado_verificacion=EstadoCita.VERIFICADA_SIN_DOI,
                            fuente_verificacion="OpenLibrary (Catálogo Bibliográfico)",
                            titulo_encontrado=found_title,
                            autor_detectado=author,
                            coincidencia_autores=True,
                            anio_encontrado=first_year,
                            motivo="Obra física/clásica localizada en catálogo bibliográfico internacional sin identificador digital DOI."
                        )
        except Exception:
            pass
        return None

    @classmethod
    async def _check_cache(cls, clave_hash: str) -> Optional[Dict[str, Any]]:
        async with get_db_connection() as db:
            async with db.execute(
                "SELECT resultado_json FROM cache_citas WHERE clave_hash = ?", (clave_hash,)
            ) as cursor:
                row = await cursor.fetchone()
                if row:
                    return json.loads(row[0])
        return None

    @classmethod
    async def _save_to_cache(cls, clave_hash: str, texto: str, resultado: Dict[str, Any]):
        async with get_db_connection() as db:
            await db.execute(
                "INSERT OR REPLACE INTO cache_citas (clave_hash, cita_texto, resultado_json) VALUES (?, ?, ?)",
                (clave_hash, texto, json.dumps(resultado)),
            )
            await db.commit()
