using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesis.Application.Modelos;
using Tesis.Domain.Enums;

namespace Tesis.Infrastructure.Servicios;

public static class GeneradorReporteIASimulado
{
    public static RevisionIAResultado Generar(Guid folio, string titulo, int version, string? rutaArchivo = null)
    {
        var texto = string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo)
            ? string.Empty
            : ExtraerTexto(rutaArchivo);

        var tieneSeccionReferencias = Regex.IsMatch(texto, @"(?i)\b(referencias\s+bibliogr[aá]ficas|referencias|bibliograf[ií]a|bibliography|fuentes\s+consultadas|literatura\s+citada)\b");
        var citasApa = Regex.Matches(texto, @"(?<![A-Za-z0-9_])\([A-ZÁÉÍÓÚÑ][a-záéíóúñ]+(?:\s+et\s+al\.)?,\s*(?:19|20)\d{2}\)");
        var citasIeee = Regex.Matches(texto, @"(?<![A-Za-z0-9_$-])\[([1-9]\d{0,2})\](?![0-9-])");
        var tieneDois = Regex.Matches(texto, @"(?i)10\.\d{4,9}/[-._;()/:A-Za-z0-9]+");

        var tieneCitas = tieneSeccionReferencias || citasApa.Count >= 2 || (citasIeee.Count >= 2 && tieneSeccionReferencias) || tieneDois.Count > 0;

        object[] auditoriaCitas;
        int totalCitas = 0;
        int citasVerificadas = 0;
        int citasSospechosas = 0;
        string semaforo;
        string dictamenSugerido;
        ResultadoRevisionIA resultado;
        decimal puntajeCoherencia;
        decimal puntajeOrtografia;
        decimal puntajeApa7;
        decimal porcentaje;
        List<object> auditoriaRedaccion = new();
        List<string> observacionesAlumno = new();

        if (!tieneCitas)
        {
            // El documento carece totalmente de citas o bibliografía (caso como el plan UX/UI de prueba)
            totalCitas = 0;
            citasVerificadas = 0;
            citasSospechosas = 0;
            auditoriaCitas = Array.Empty<object>();
            semaforo = "AMARILLO";
            dictamenSugerido = "CON OBSERVACIONES: Sin citas ni referencias bibliográficas en formato APA 7";
            resultado = ResultadoRevisionIA.ConObservaciones;
            puntajeCoherencia = 7.5m;
            puntajeOrtografia = 8.5m;
            puntajeApa7 = 6.0m;
            porcentaje = 75.0m;

            auditoriaRedaccion.Add(new
            {
                seccion = "Aparato Crítico y Citas",
                tipo = "COHERENCIA",
                texto_afectado = "N/A",
                observacion = "El documento no cuenta con apartado de referencias bibliográficas ni citas académicas en el cuerpo del texto.",
                sugerencia = "Incorporar las fuentes de consulta en formato APA 7ma edición conforme a los lineamientos institucionales."
            });

            auditoriaRedaccion.Add(new
            {
                seccion = "Normas APA 7ma Edición",
                tipo = "FORMATO_APA7",
                texto_afectado = "N/A",
                observacion = "No se detectaron citas parentéticas (Autor, Año) ni lista de referencias finales estructurada bajo estándares APA 7.",
                sugerencia = "Adaptar el aparato crítico a las normas APA 7ma edición (citas en el texto y bibliografía con sangría francesa)."
            });

            observacionesAlumno.Add("No se detectaron citas ni sección de bibliografía en el documento subido.");
            observacionesAlumno.Add("Es indispensable presentar las fuentes de consulta bajo las normas APA 7ma edición para su aprobación.");
        }
        else
        {
            // El documento sí cuenta con citas o sección de referencias
            totalCitas = Math.Max(citasApa.Count + citasIeee.Count, 12);
            citasVerificadas = totalCitas - 1;
            citasSospechosas = 1;
            semaforo = "VERDE";
            dictamenSugerido = "APROBABLE";
            resultado = ResultadoRevisionIA.Valido;
            puntajeCoherencia = 9.2m;
            puntajeOrtografia = 9.4m;
            puntajeApa7 = 9.1m;
            porcentaje = 92.0m;

            auditoriaCitas = new object[]
            {
                new
                {
                    cita_original = "Hernández-Sampieri, R., Fernández-Collado, C., & Baptista-Lucio, P. (2018). Metodología de la investigación. McGraw-Hill.",
                    estado_verificacion = "VERIFICADA_SIN_DOI",
                    doi = (string?)null,
                    motivo = (string?)null
                },
                new
                {
                    cita_original = "Goodfellow, I., Bengio, Y., & Courville, A. (2016). Deep Learning. MIT Press.",
                    estado_verificacion = "VERIFICADA",
                    doi = "10.1007/s10710-017-9314-z",
                    motivo = (string?)null
                },
                new
                {
                    cita_original = "Russell, S., & Norvig, P. (2020). Artificial Intelligence: A Modern Approach. Pearson.",
                    estado_verificacion = "VERIFICADA",
                    doi = "10.1145/3411764.3445532",
                    motivo = (string?)null
                },
                new
                {
                    cita_original = "Referencia complementaria sin identificador de catálogo institucional.",
                    estado_verificacion = "SOSPECHOSA",
                    doi = (string?)null,
                    motivo = "No se localizó en índices de OpenAlex / Crossref. Se sugiere verificar año y autor."
                }
            };

            auditoriaRedaccion.Add(new
            {
                seccion = "Introducción",
                tipo = "ESTILO",
                texto_afectado = "En este trabajo pretendemos mostrar...",
                observacion = "Se recomienda el uso consistente de la tercera persona impersonal en estilo académico.",
                sugerencia = "En el presente trabajo se aborda..."
            });

            auditoriaRedaccion.Add(new
            {
                seccion = "Marco Teórico",
                tipo = "COHERENCIA",
                texto_afectado = "N/A",
                observacion = $"Excelente fundamentación teórica y concordancia conceptual con la temática: \"{titulo}\".",
                sugerencia = "Conservar la coherencia de variables en el desarrollo de los resultados."
            });

            observacionesAlumno.Add("Verificar la ortografía puntual y acentuación en subtítulos del capítulo 3.");
            observacionesAlumno.Add("Corroborar los metadatos o DOI de la cita bibliográfica catalogada con observación.");
            observacionesAlumno.Add("Mantener la formalidad académica en la redacción de las conclusiones.");
        }

        var reporte = new
        {
            ticket_id = $"aud_sim_{folio.ToString("N")[..8]}_{version}",
            tesis_id = folio.ToString(),
            estado = "COMPLETADO",
            progreso_porcentaje = 100,
            mensaje = $"Auditoría académica completada para la versión {version}.",
            resumen_ejecutivo = new
            {
                dictamen_sugerido = dictamenSugerido,
                semaforo = semaforo,
                total_citas = totalCitas,
                citas_verificadas = citasVerificadas,
                citas_sospechosas = citasSospechosas,
                puntaje_coherencia = puntajeCoherencia,
                puntaje_ortografia = puntajeOrtografia,
                puntaje_apa7 = puntajeApa7
            },
            auditoria_citas = auditoriaCitas,
            auditoria_redaccion = auditoriaRedaccion,
            observaciones_para_alumno = observacionesAlumno,
            error = (string?)null
        };

        var json = JsonSerializer.Serialize(reporte, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        });

        return new RevisionIAResultado(resultado, porcentaje, json);
    }

    private static string ExtraerTexto(string rutaArchivo)
    {
        try
        {
            var ext = Path.GetExtension(rutaArchivo).ToLowerInvariant();
            if (ext == ".docx")
            {
                using var zip = ZipFile.OpenRead(rutaArchivo);
                var entry = zip.GetEntry("word/document.xml");
                if (entry is not null)
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var xml = reader.ReadToEnd();
                    return Regex.Replace(xml, "<[^>]+>", " ");
                }
            }
            else if (ext == ".txt")
            {
                return File.ReadAllText(rutaArchivo);
            }
            else if (ext == ".pdf")
            {
                byte[] bytes;
                var fileInfo = new FileInfo(rutaArchivo);
                if (fileInfo.Length > 2 * 1024 * 1024)
                {
                    using var fs = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    long bytesToRead = Math.Min(fileInfo.Length, 1500 * 1024);
                    fs.Seek(fileInfo.Length - bytesToRead, SeekOrigin.Begin);
                    bytes = new byte[bytesToRead];
                    fs.Read(bytes, 0, (int)bytesToRead);
                }
                else
                {
                    bytes = File.ReadAllBytes(rutaArchivo);
                }
                var contenido = System.Text.Encoding.Latin1.GetString(bytes);
                return contenido;
            }
        }
        catch
        {
            // Ignorar fallos de descompresión o formato y retornar texto vacío
        }
        return string.Empty;
    }
}
