import os
import sys
from pathlib import Path

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8")

# Ajustar PYTHONPATH
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.services.apa7_document_auditor import APA7DocumentAuditor
from src.models.schemas import AuditoriaFormatoDocumentoAPA7

def test_audit_pdf_format():
    pdf_path = Path(__file__).parent / "tesis_ejemplo.pdf"
    if not pdf_path.exists():
        print("PDF de prueba no encontrado en:", pdf_path)
        return

    resultado = APA7DocumentAuditor.audit_document(str(pdf_path))
    print("\n--- Resultado Auditoría PDF Formato APA 7 ---")
    print(f"Puntaje: {resultado['puntaje_formato']}/10")
    print(f"Resumen: {resultado['resumen']}")
    print(f"Detalles Técnicos: {resultado['detalles_tecnicos']}")
    for item in resultado["items"]:
        estado = "✓" if item["cumple"] else "✗"
        print(f"[{estado}] {item['criterio']}: {item['detalle_detectado']}")
        if item.get("sugerencia"):
            print(f"    -> Sugerencia: {item['sugerencia']}")

    assert "puntaje_formato" in resultado
    assert isinstance(resultado["items"], list)
    assert len(resultado["items"]) >= 4

    # Validar que mapee a nuestro schema de Pydantic
    parsed = AuditoriaFormatoDocumentoAPA7(**resultado)
    assert parsed.puntaje_formato >= 0.0

def test_audit_docx_format(tmp_path):
    from docx import Document
    from docx.shared import Inches, Pt

    doc = Document()
    # Configurar márgenes 1 pulgada (2.54 cm)
    for section in doc.sections:
        section.top_margin = Inches(1)
        section.bottom_margin = Inches(1)
        section.left_margin = Inches(1)
        section.right_margin = Inches(1)
        section.page_width = Inches(8.5)
        section.page_height = Inches(11)

    p1 = doc.add_paragraph("Capítulo 1: Introducción a la Investigación")
    p1.paragraph_format.line_spacing = 2.0
    p1.paragraph_format.first_line_indent = Inches(0.5)
    run1 = p1.add_run("Este es un párrafo de prueba formateado con Times New Roman 12 y sangría de primera línea.")
    run1.font.name = "Times New Roman"
    run1.font.size = Pt(12)

    docx_path = tmp_path / "tesis_test.docx"
    doc.save(str(docx_path))

    resultado = APA7DocumentAuditor.audit_document(str(docx_path))
    print("\n--- Resultado Auditoría DOCX Formato APA 7 ---")
    print(f"Puntaje: {resultado['puntaje_formato']}/10")
    print(f"Resumen: {resultado['resumen']}")
    for item in resultado["items"]:
        estado = "✓" if item["cumple"] else "✗"
        print(f"[{estado}] {item['criterio']}: {item['detalle_detectado']}")

    assert resultado["puntaje_formato"] >= 8.0
    assert any(it["criterio"] == "Tamaño de Papel" and it["cumple"] for it in resultado["items"])
    assert any(it["criterio"] == "Márgenes de Página" and it["cumple"] for it in resultado["items"])

if __name__ == "__main__":
    test_audit_pdf_format()
    import tempfile
    with tempfile.TemporaryDirectory() as td:
        test_audit_docx_format(Path(td))
    print("\n✓ Todas las pruebas de auditoría de maquetación APA 7 pasaron correctamente.")
