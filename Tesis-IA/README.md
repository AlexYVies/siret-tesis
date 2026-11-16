# Tesis-IA: Microservicio Auditor de Tesis con IA Local y Verificación Académica

Microservicio de evaluación académica automática para la plataforma de tesis universitaria. Diseñado para procesar lotes de 50 a 180 tesis por cuatrimestre a **$0 MXN de costo operativo**.

---

## Características Principales

1. **Cola de Procesamiento Asíncrona (FIFO):**
   - Cuando el alumno sube la tesis (PDF/Word), el endpoint responde de inmediato (`202 Accepted`) con un ticket de seguimiento.
   - La cola despacha las tesis ordenadamente sin bloquear la plataforma ni sobrecargar la memoria.
2. **Anti-Alucinación y Verificación de Citas ($0 MXN):**
   - Extrae el bloque de referencias bibliográficas.
   - Consulta el *Polite Pool* de **Crossref** y **Semantic Scholar** en internet.
   - Detecta si las citas existen realmente, valida autores, año y DOI.
   - Marca explícitamente en **ROJO** citas inventadas o no indexadas.
   - Almacena en caché local SQLite (`cache_citas`) para responder en 0 ms si dos alumnos citan la misma fuente.
3. **Auditoría de Redacción con Modelo Local:**
   - Analiza coherencia entre párrafos, afirmaciones sin sustento y faltas ortográficas mediante el modelo local de **Ollama** (`qwen2.5-coder:7b`).
   - Genera sugerencias exactas de corrección para el alumno.
4. **Semáforo y Dictamen para el Docente:**
   - **VERDE:** Aprobable sin observaciones críticas.
   - **AMARILLO:** Requiere correcciones menores de forma u ortografía.
   - **ROJO:** Requiere revisión mayor por citas sospechosas o incoherencias graves.

---

## Cómo Iniciar el Servicio

### 1. Activar el Entorno Virtual
```powershell
.\venv\Scripts\Activate.ps1
```

### 2. Arrancar el Servidor FastAPI
```powershell
$env:PYTHONPATH="."
python main.py
```
El servicio estará disponible en: `http://localhost:8000`  
Documentación interactiva Swagger: `http://localhost:8000/docs`

---

## Integración con .NET y Angular

### Encolar Tesis al Subirse (C# / .NET Web API)
```http
POST http://localhost:8000/api/v1/auditorias
Content-Type: multipart/form-data

archivo: [archivo binario .pdf o .docx]
tesis_id: "101"
titulo: "Optimización de Algoritmos"
alumno_id: "alumno_45"
webhook_url: "http://localhost:5000/api/tesis/101/reporte-ia"  (opcional)
```

### Consultar Estado y Reporte (Angular / .NET)
```http
GET http://localhost:8000/api/v1/auditorias/{ticket_id}
```
Devuelve el JSON estructurado con el desglose de citas, observaciones y semáforo.
