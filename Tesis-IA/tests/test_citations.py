import asyncio
import os
from src.services.citation_parser import CitationParser
from src.services.citation_verifier import CitationVerifier
from src.db.database import init_db

# Simulación de un bloque real de referencias con:
# 1. Paper famoso con DOI explícito
# 2. Paper sin DOI pero con título real de alto impacto
# 3. Referencia inventada / falsa (alucinación)
TEXTO_REFERENCIAS_PRUEBA = """
Vaswani, A., Shazeer, N., Parmar, N., Uszkoreit, J., Jones, L., Gomez, A. N., Kaiser, Ł., & Polosukhin, I. (2017). Attention is all you need. Advances in Neural Information Processing Systems, 30. https://doi.org/10.48550/arXiv.1706.03762

Goodfellow, I., Pouget-Abadie, J., Mirza, M., Xu, B., Warde-Farley, D., Ozair, S., Courville, A., & Bengio, Y. Generative adversarial nets. Advances in neural information processing systems, 2014.

Pérez-Gómez, J. M., & Robotnik, E. (2025). Algoritmos cuánticos para teleportación de tesis en universidades cósmicas. Revista Intergaláctica de Computación, 99(1), 1-10.
"""

async def test_citations():
    print("Iniciando prueba de verificación de citas...")
    await init_db()
    
    # 1. Parsear
    citas_parseadas = CitationParser.parse_references_block(TEXTO_REFERENCIAS_PRUEBA)
    print(f"Citas detectadas por el parser: {len(citas_parseadas)}")
    for i, c in enumerate(citas_parseadas, 1):
        print(f"[{i}] Título estimado: '{c['titulo_estimado']}' | DOI: {c['doi']}")

    # 2. Verificar contra Crossref / Semantic Scholar
    print("\nConsultando APIs académicas (Crossref / Semantic Scholar)...")
    resultados = await CitationVerifier.verify_citations(citas_parseadas)

    print("\n--- RESULTADOS DE LA AUDITORÍA DE CITAS ---")
    for r in resultados:
        print(f"\nCita: {r.cita_original[:70]}...")
        print(f"Estado: {r.estado_verificacion.value}")
        if r.doi:
            print(f"DOI encontrado: {r.doi}")
        if r.titulo_encontrado:
            print(f"Título validado: {r.titulo_encontrado}")
        if r.motivo:
            print(f"Motivo: {r.motivo}")

if __name__ == "__main__":
    asyncio.run(test_citations())
