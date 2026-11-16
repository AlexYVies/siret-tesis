import asyncio
from src.services.llm_auditor import ThesisAuditor

TEXTO_TESIS_PRUEBA = """
Capítulo 1: Introducción
El presente trabajo de investigacion aborda el desarrollo de sistemas inteligentes aplicados a la educación superior.
En la actualidad, muchas universidades carecen de metodos automatizados para revisar la autenticidad de las tesis.
Por lo tanto se diseño un prototipo. La inteligencia artificial no sirve para nada pero a la vez es el futuro del universo y por eso todos los alumnos deben usarla obligatoriamente sin importar lo que diga el reglamento.

Capítulo 2: Metodología
Se utilizo un enfoque mixto con tecnicas cualitativas y cuantitativas. Los datos fueron recolectados a traves de encuestas digitales. Sin embargo, no se verifico la muestra estadística, pero los resultados son 100% ciertos porque nosotros lo comprobamos en una tarde de pruebas.
"""

async def test_llm():
    print("Probando auditoría de redacción con Ollama local...")
    res = await ThesisAuditor.audit_text_chunks(TEXTO_TESIS_PRUEBA)
    print("\n--- RESULTADOS AUDITORÍA DE REDACCIÓN (LLM) ---")
    print(f"Puntaje Coherencia: {res['puntaje_coherencia']}/10")
    print(f"Puntaje Ortografía: {res['puntaje_ortografia']}/10")
    print(f"\nObservaciones detectadas ({len(res['observaciones'])}):")
    for o in res["observaciones"]:
        print(f"\n[{o.tipo.value}] En {o.seccion}:")
        print(f"  Texto: \"{o.texto_afectado}\"")
        print(f"  Problema: {o.observacion}")
        print(f"  Sugerencia: {o.sugerencia}")
    print("\nConsejos para el alumno:")
    for c in res["consejos_alumno"]:
        print(f" - {c}")

if __name__ == "__main__":
    asyncio.run(test_llm())
