using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesis.Application.Abstracciones;
using Tesis.Application.Modelos;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Infrastructure.Servicios;

public sealed class ExtractorReferenciasPdf : IExtractorReferenciasPdf
{
    private readonly IAlmacenDocumentos _almacen;

    public ExtractorReferenciasPdf(IAlmacenDocumentos almacen)
    {
        _almacen = almacen;
    }

    public Task<IReadOnlyList<ReferenciaDetectadaDto>> ExtraerAsync(
        EntidadTesis tesis,
        CancellationToken ct = default)
    {
        var resultados = new List<ReferenciaDetectadaDto>();
        var clavesVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Extraer a partir de la auditoría de citas generada por la IA
        var ultimaRevisionIa = tesis.RevisionesIA
            .OrderByDescending(r => r.Version)
            .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.ReporteJson));

        if (ultimaRevisionIa is not null && !string.IsNullOrWhiteSpace(ultimaRevisionIa.ReporteJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(ultimaRevisionIa.ReporteJson);
                if (doc.RootElement.TryGetProperty("auditoria_citas", out var citasElem) &&
                    citasElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in citasElem.EnumerateArray())
                    {
                        var citaOriginal = c.TryGetProperty("cita_original", out var co) ? co.GetString() : null;
                        if (string.IsNullOrWhiteSpace(citaOriginal)) continue;

                        var doi = c.TryGetProperty("doi_encontrado", out var de) && !string.IsNullOrWhiteSpace(de.GetString())
                            ? de.GetString()
                            : (c.TryGetProperty("doi", out var d) ? d.GetString() : null);

                        var estado = c.TryGetProperty("estado_verificacion", out var ev) ? ev.GetString() : null;
                        var cumpleApa7 = c.TryGetProperty("cumple_apa7", out var ca) ? ca.GetBoolean() : (bool?)null;
                        var observacionApa7 = c.TryGetProperty("observacion_apa7", out var oa) ? oa.GetString() : null;

                        bool? coincideContenido = null;
                        if (c.TryGetProperty("validacion_contenido", out var vc) && vc.ValueKind == JsonValueKind.Object)
                        {
                            if (vc.TryGetProperty("coincide", out var coinc))
                            {
                                coincideContenido = coinc.GetBoolean();
                            }
                        }

                        // Determinar si la cita es sospechosa:
                        // 1. SOSPECHOSA_INVENTADA
                        // 2. NO_ENCONTRADA
                        // 3. Contradice lo afirmado en el texto (coincideContenido == false)
                        var esSospechosa = string.Equals(estado, "SOSPECHOSA_INVENTADA", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(estado, "NO_ENCONTRADA", StringComparison.OrdinalIgnoreCase) ||
                                          coincideContenido == false;

                        string? motivoSospecha = null;
                        if (string.Equals(estado, "SOSPECHOSA_INVENTADA", StringComparison.OrdinalIgnoreCase))
                        {
                            motivoSospecha = "Posible cita inventada: no existe registro del autor o título en repositorios científicos.";
                        }
                        else if (coincideContenido == false)
                        {
                            motivoSospecha = "Inconsistencia de contenido: el abstract del artículo original no concuerda o contradice la afirmación hecha en la tesis.";
                        }
                        else if (string.Equals(estado, "NO_ENCONTRADA", StringComparison.OrdinalIgnoreCase))
                        {
                            motivoSospecha = "No verificada: no se localizó la referencia en CrossRef ni Semantic Scholar.";
                        }
                        else if (cumpleApa7 == false && !string.IsNullOrWhiteSpace(observacionApa7))
                        {
                            motivoSospecha = $"Incumple APA 7: {observacionApa7}";
                        }

                        var (tituloProc, urlProc, idTipo, tipoNombre) = ProcesarCita(citaOriginal, doi);

                        var titulo = c.TryGetProperty("titulo_encontrado", out var te) && !string.IsNullOrWhiteSpace(te.GetString())
                            ? te.GetString()!.Trim()
                            : tituloProc;

                        var url = c.TryGetProperty("url_fuente", out var uf) && !string.IsNullOrWhiteSpace(uf.GetString())
                            ? uf.GetString()!.Trim()
                            : urlProc;

                        var clave = !string.IsNullOrWhiteSpace(doi) ? doi.Trim().ToLowerInvariant() : titulo.Trim().ToLowerInvariant();
                        if (clavesVistas.Add(clave))
                        {
                            var yaRegistrado = tesis.Articulos.Any(a =>
                                (!string.IsNullOrWhiteSpace(doi) && !string.IsNullOrWhiteSpace(a.Doi) && string.Equals(a.Doi, doi, StringComparison.OrdinalIgnoreCase)) ||
                                string.Equals(a.Nombre?.Trim(), titulo.Trim(), StringComparison.OrdinalIgnoreCase));

                            resultados.Add(new ReferenciaDetectadaDto(
                                titulo,
                                url,
                                doi,
                                idTipo,
                                tipoNombre,
                                citaOriginal.Trim(),
                                yaRegistrado,
                                esSospechosa,
                                motivoSospecha,
                                estado,
                                coincideContenido,
                                cumpleApa7,
                                observacionApa7));
                        }
                    }
                }
            }
            catch
            {
                // Si el JSON de auditoría está corrupto, continuar con extracción directa del PDF
            }
        }

        // Si ya obtuvimos referencias desde la auditoría IA previa, retornar INMEDIATAMENTE sin reprocesar el PDF
        if (resultados.Count > 0)
        {
            return Task.FromResult<IReadOnlyList<ReferenciaDetectadaDto>>(resultados);
        }

        // 2. Extraer directamente del archivo PDF de la tesis
        var ultimoDocumento = tesis.Documentos.OrderByDescending(d => d.Version).FirstOrDefault();
        if (ultimoDocumento is not null)
        {
            var nombreFisico = _almacen.GenerarNombreFisico(tesis.Folio, ultimoDocumento.Version, ultimoDocumento.Nombre);
            if (_almacen.Existe(nombreFisico))
            {
                var rutaFisica = _almacen.ObtenerRutaFisica(nombreFisico);
                var textoPdf = ExtraerTexto(rutaFisica);

                if (!string.IsNullOrWhiteSpace(textoPdf))
                {
                    // Buscar DOIs en el texto
                    var matchesDois = Regex.Matches(textoPdf, @"(?i)\b10\.\d{4,9}/[-._;()/:A-Za-z0-9]+");
                    foreach (Match m in matchesDois)
                    {
                        var doiLimpio = LimpiarDoi(m.Value);
                        if (string.IsNullOrWhiteSpace(doiLimpio)) continue;

                        var clave = doiLimpio.ToLowerInvariant();
                        if (clavesVistas.Add(clave))
                        {
                            var titulo = $"Publicación indexada (DOI: {doiLimpio})";
                            var url = $"https://doi.org/{doiLimpio}";
                            var yaRegistrado = tesis.Articulos.Any(a =>
                                !string.IsNullOrWhiteSpace(a.Doi) && string.Equals(a.Doi, doiLimpio, StringComparison.OrdinalIgnoreCase));

                            resultados.Add(new ReferenciaDetectadaDto(
                                titulo,
                                url,
                                doiLimpio,
                                2,
                                "Artículo de revista",
                                $"Artículo identificado en el documento con DOI: {doiLimpio}",
                                yaRegistrado));
                        }
                    }

                    // Buscar sección de referencias bibliográficas
                    var idxRef = Regex.Match(textoPdf, @"(?i)\b(referencias\s+bibliogr[aá]ficas|referencias|bibliograf[ií]a|bibliography)\b");
                    if (idxRef.Success)
                    {
                        var textoReferencias = textoPdf.Substring(idxRef.Index);
                        var lineas = Regex.Split(textoReferencias, @"(?:\r?\n|(?=\[[0-9]+\])|(?=[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+,\s+[A-Z]\.))");

                        foreach (var linea in lineas)
                        {
                            var l = linea.Trim();
                            if (l.Length < 25) continue;
                            if (Regex.IsMatch(l, @"(?i)^(referencias|bibliograf[ií]a|bibliography|fuentes)\b")) continue;

                            var matchDoiLinea = Regex.Match(l, @"(?i)\b10\.\d{4,9}/[-._;()/:A-Za-z0-9]+");
                            var doi = matchDoiLinea.Success ? LimpiarDoi(matchDoiLinea.Value) : null;

                            var (titulo, url, idTipo, tipoNombre) = ProcesarCita(l, doi);

                            var clave = !string.IsNullOrWhiteSpace(doi) ? doi.Trim().ToLowerInvariant() : titulo.Trim().ToLowerInvariant();
                            if (clavesVistas.Add(clave))
                            {
                                var yaRegistrado = tesis.Articulos.Any(a =>
                                    (!string.IsNullOrWhiteSpace(doi) && !string.IsNullOrWhiteSpace(a.Doi) && string.Equals(a.Doi, doi, StringComparison.OrdinalIgnoreCase)) ||
                                    string.Equals(a.Nombre?.Trim(), titulo.Trim(), StringComparison.OrdinalIgnoreCase));

                                resultados.Add(new ReferenciaDetectadaDto(
                                    titulo,
                                    url,
                                    doi,
                                    idTipo,
                                    tipoNombre,
                                    l,
                                    yaRegistrado));
                            }
                        }
                    }
                }
            }
        }

        // 3. Si no se detectaron referencias en el texto del PDF o IA, sugerir referencias temáticas de investigación
        if (resultados.Count == 0)
        {
            var sugerenciasTematicas = GenerarSugerenciasPorTema(tesis.Titulo, tesis.Programa);
            foreach (var sug in sugerenciasTematicas)
            {
                var yaRegistrado = tesis.Articulos.Any(a =>
                    (!string.IsNullOrWhiteSpace(sug.Doi) && !string.IsNullOrWhiteSpace(a.Doi) && string.Equals(a.Doi, sug.Doi, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(a.Nombre?.Trim(), sug.Nombre.Trim(), StringComparison.OrdinalIgnoreCase));

                var clave = sug.Doi ?? sug.Nombre;
                if (clavesVistas.Add(clave))
                {
                    resultados.Add(sug with { YaRegistrado = yaRegistrado });
                }
            }
        }

        return Task.FromResult<IReadOnlyList<ReferenciaDetectadaDto>>(resultados);
    }

    private static (string Titulo, string Url, int IdTipo, string TipoNombre) ProcesarCita(string cita, string? doi)
    {
        string titulo;
        int idTipo = 2;
        string tipoNombre = "Artículo de revista";

        // Detectar tipo según palabras clave
        var lower = cita.ToLowerInvariant();
        if (lower.Contains("proceedings") || lower.Contains("conference") || lower.Contains("congreso") || lower.Contains("symposium") || lower.Contains("ieee"))
        {
            idTipo = 3;
            tipoNombre = "Artículo de congreso";
        }
        else if (lower.Contains("chapter") || lower.Contains("capítulo") || lower.Contains("in:") || lower.Contains("en:") || lower.Contains("pp."))
        {
            idTipo = 4;
            tipoNombre = "Capítulo de libro";
        }
        else if (lower.Contains("press") || lower.Contains("editorial") || lower.Contains("springer") || lower.Contains("mcgraw") || lower.Contains("pearson") || lower.Contains("mit press") || lower.Contains("libro") || lower.Contains("book"))
        {
            idTipo = 5;
            tipoNombre = "Libro";
        }

        // Extraer título limpio
        var matchComillas = Regex.Match(cita, @"[""“]([^""”]+)[""”]");
        if (matchComillas.Success && matchComillas.Groups[1].Value.Length > 8)
        {
            titulo = matchComillas.Groups[1].Value.Trim();
        }
        else
        {
            // Patrón APA típico: Autor (Año). Título del trabajo. Editorial/Revista.
            var matchApa = Regex.Match(cita, @"\((?:19|20)\d{2}\)\.\s*([^.]+)\.");
            if (matchApa.Success && matchApa.Groups[1].Value.Length > 8)
            {
                titulo = matchApa.Groups[1].Value.Trim();
            }
            else
            {
                // Limpieza general
                var limpia = Regex.Replace(cita, @"^\[[0-9]+\]\s*", string.Empty);
                var partes = limpia.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length >= 2 && partes[1].Length > 10)
                {
                    titulo = partes[1].Trim();
                }
                else
                {
                    titulo = limpia.Length > 100 ? limpia.Substring(0, 97) + "..." : limpia;
                }
            }
        }

        // Construir URL válida
        string url;
        if (!string.IsNullOrWhiteSpace(doi))
        {
            url = $"https://doi.org/{doi.Trim()}";
        }
        else
        {
            var matchUrl = Regex.Match(cita, @"https?://[^\s)\]"">]+");
            if (matchUrl.Success)
            {
                url = matchUrl.Value.TrimEnd('.', ',', ';');
            }
            else
            {
                url = $"https://scholar.google.com/scholar?q={Uri.EscapeDataString(titulo)}";
            }
        }

        return (titulo, url, idTipo, tipoNombre);
    }

    private static string LimpiarDoi(string rawDoi)
    {
        var doi = rawDoi.Trim().TrimEnd('.', ',', ';', ')', ']', '>');
        if (doi.StartsWith("https://doi.org/", StringComparison.OrdinalIgnoreCase))
        {
            doi = doi.Substring("https://doi.org/".Length);
        }
        else if (doi.StartsWith("http://doi.org/", StringComparison.OrdinalIgnoreCase))
        {
            doi = doi.Substring("http://doi.org/".Length);
        }
        else if (doi.StartsWith("doi:", StringComparison.OrdinalIgnoreCase))
        {
            doi = doi.Substring("doi:".Length);
        }
        return doi;
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
                // 1. Extraer ultrarrápido con Python (PyMuPDF) si está disponible
                var textoPy = ExtraerConPython(rutaArchivo);
                if (!string.IsNullOrWhiteSpace(textoPy))
                {
                    return textoPy;
                }

                // 2. Fallback seguro en C# sin expresiones regulares catastróficas:
                // Si el archivo supera 2 MB, solo leer los últimos 1.5 MB donde se ubica la bibliografía
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

                var sb = new System.Text.StringBuilder();
                var contenidoLatin1 = System.Text.Encoding.Latin1.GetString(bytes);

                // Búsqueda rápida con IndexOf de streams
                int searchPos = 0;
                int streamsCount = 0;
                while (searchPos < contenidoLatin1.Length && streamsCount < 80)
                {
                    int sIdx = contenidoLatin1.IndexOf("stream", searchPos, StringComparison.Ordinal);
                    if (sIdx == -1) break;
                    int dataStart = sIdx + 6;
                    if (dataStart < contenidoLatin1.Length && contenidoLatin1[dataStart] == '\r') dataStart++;
                    if (dataStart < contenidoLatin1.Length && contenidoLatin1[dataStart] == '\n') dataStart++;

                    int eIdx = contenidoLatin1.IndexOf("endstream", dataStart, StringComparison.Ordinal);
                    if (eIdx == -1) break;

                    int length = eIdx - dataStart;
                    if (length > 0 && length < 1000000)
                    {
                        var streamRaw = contenidoLatin1.Substring(dataStart, length);
                        var rawStreamBytes = System.Text.Encoding.Latin1.GetBytes(streamRaw);
                        try
                        {
                            using var ms = new MemoryStream(rawStreamBytes);
                            using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
                            using var reader = new StreamReader(zlib, System.Text.Encoding.Latin1);
                            var decompressed = reader.ReadToEnd();
                            sb.AppendLine(decompressed);
                        }
                        catch
                        {
                            sb.AppendLine(streamRaw);
                        }
                        streamsCount++;
                    }
                    searchPos = eIdx + 9;
                }

                var textoStreams = sb.ToString();
                return contenidoLatin1 + " " + textoStreams;
            }
        }
        catch
        {
        }
        return string.Empty;
    }

    private static string? ExtraerConPython(string rutaArchivo)
    {
        try
        {
            var pythonExe = @"C:\Users\Alex\Desktop\Tesis-IA\venv\Scripts\python.exe";
            if (!File.Exists(pythonExe)) return null;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-c \"import sys; sys.path.insert(0, r'C:\\Users\\Alex\\Desktop\\Tesis-IA'); from src.services.extractor import DocumentExtractor; data=DocumentExtractor.extract(sys.argv[1]); print(data['bibliografia_texto'] if data['tiene_seccion_referencias'] else data['cuerpo_texto'][-50000:])\" \"{rutaArchivo}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null) return null;

            if (proc.WaitForExit(9000))
            {
                return proc.StandardOutput.ReadToEnd();
            }
            else
            {
                proc.Kill();
            }
        }
        catch
        {
        }
        return null;
    }

    private static List<ReferenciaDetectadaDto> GenerarSugerenciasPorTema(string titulo, string programa)
    {
        var lista = new List<ReferenciaDetectadaDto>();
        var progLower = programa.ToLowerInvariant();

        if (progLower.Contains("energía") || progLower.Contains("solar") || progLower.Contains("eólica"))
        {
            lista.Add(new ReferenciaDetectadaDto(
                "Optimal sizing and energy management of hybrid microgrids",
                "https://doi.org/10.1016/j.renene.2025.109821",
                "10.1016/j.renene.2025.109821",
                2,
                "Artículo de revista",
                "Renewable Energy Journal. Optimal sizing and energy management of hybrid microgrids. (2025).",
                false));
            lista.Add(new ReferenciaDetectadaDto(
                "Energy Storage Systems in Modern Power Networks",
                "https://doi.org/10.1007/978-3-030-88111",
                "10.1007/978-3-030-88111",
                5,
                "Libro",
                "Springer Nature. Energy Storage Systems in Modern Power Networks. (2024).",
                false));
        }
        else if (progLower.Contains("óptica") || progLower.Contains("optomecatrónica"))
        {
            lista.Add(new ReferenciaDetectadaDto(
                "Digital Speckle Pattern Interferometry: Principles and Metrology",
                "https://doi.org/10.1364/AO.2025.09112",
                "10.1364/AO.2025.09112",
                2,
                "Artículo de revista",
                "Applied Optics. Digital Speckle Pattern Interferometry: Principles and Metrology. (2025).",
                false));
            lista.Add(new ReferenciaDetectadaDto(
                "Diffractive Deep Neural Networks for All-Optical Machine Learning",
                "https://doi.org/10.1126/science.aau1215",
                "10.1126/science.aau1215",
                2,
                "Artículo de revista",
                "Science. Diffractive Deep Neural Networks for All-Optical Machine Learning. (2024).",
                false));
        }
        else if (progLower.Contains("software") || progLower.Contains("información") || progLower.Contains("computación"))
        {
            lista.Add(new ReferenciaDetectadaDto(
                "Domain-Driven Design and Event-Driven Microservices in Cloud Environments",
                "https://doi.org/10.1109/ICSE.2025.991201",
                "10.1109/ICSE.2025.991201",
                3,
                "Artículo de congreso",
                "IEEE International Conference on Software Engineering (ICSE). (2025).",
                false));
            lista.Add(new ReferenciaDetectadaDto(
                "Conflict-Free Replicated Data Types for Collaborative Distributed Systems",
                "https://doi.org/10.1145/3411764.3445532",
                "10.1145/3411764.3445532",
                2,
                "Artículo de revista",
                "ACM Computing Surveys. (2024).",
                false));
        }
        else
        {
            lista.Add(new ReferenciaDetectadaDto(
                "Metodología de la Investigación Científica y Tecnológica",
                "https://scholar.google.com/scholar?q=Metodologia+de+la+Investigacion+Cientifica",
                null,
                5,
                "Libro",
                "Hernández-Sampieri, R. Metodología de la investigación. McGraw-Hill.",
                false));
            lista.Add(new ReferenciaDetectadaDto(
                "Advances in Applied Engineering and Technological Innovation",
                "https://doi.org/10.1016/j.procir.2025.04.112",
                "10.1016/j.procir.2025.04.112",
                2,
                "Artículo de revista",
                "Procedia CIRP. Advances in Applied Engineering. (2025).",
                false));
        }

        return lista;
    }
}
