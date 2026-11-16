using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Tesis.Application.Abstracciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Enums;

namespace Tesis.Infrastructure.Servicios;

public sealed class ServicioRevisionIAHttp : IServicioRevisionIA
{
    private readonly HttpClient _cliente;
    private readonly IConfiguration _configuracion;
    private readonly IAlmacenDocumentos _almacen;
    private readonly ITesisRepository _tesisRepository;

    public ServicioRevisionIAHttp(
        HttpClient cliente,
        IConfiguration configuracion,
        IAlmacenDocumentos almacen,
        ITesisRepository tesisRepository)
    {
        _cliente = cliente;
        _configuracion = configuracion;
        _almacen = almacen;
        _tesisRepository = tesisRepository;
    }

    public async Task<RevisionIAResultado> RevisarAsync(Guid folio, int version, CancellationToken ct = default)
    {
        var endpoint = _configuracion["RevisionIA:Endpoint"] ?? "http://localhost:8000/api/v1/auditorias";

        var tesis = await _tesisRepository.ObtenerPorFolioAsync(folio, ct)
            ?? throw new InvalidOperationException($"No se encontró la tesis con folio {folio}.");

        var documento = tesis.Documentos.OrderByDescending(d => d.Version).FirstOrDefault();
        if (documento is null)
        {
            return GeneradorReporteIASimulado.Generar(folio, tesis.Titulo, version);
        }

        var nombreFisico = _almacen.GenerarNombreFisico(folio, documento.Version, documento.Nombre);
        if (!_almacen.Existe(nombreFisico))
        {
            return GeneradorReporteIASimulado.Generar(folio, tesis.Titulo, version);
        }

        try
        {
            using var stream = _almacen.Abrir(nombreFisico);
            using var formData = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            formData.Add(fileContent, "archivo", documento.Nombre);
            formData.Add(new StringContent(folio.ToString()), "tesis_id");
            formData.Add(new StringContent(tesis.Titulo), "titulo");
            formData.Add(new StringContent(tesis.IdAlumno.ToString()), "alumno_id");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15)); // Tiempo límite razonable antes de fallback

            var respPost = await _cliente.PostAsync(endpoint, formData, cts.Token);
            respPost.EnsureSuccessStatusCode();

            using var docTicket = JsonDocument.Parse(await respPost.Content.ReadAsStringAsync(cts.Token));
            var ticketId = docTicket.RootElement.GetProperty("ticket_id").GetString();
            if (string.IsNullOrWhiteSpace(ticketId))
            {
                throw new HttpRequestException("No se recibió ticket_id del servicio auditor de tesis.");
            }

            var statusEndpoint = endpoint.TrimEnd('/') + "/" + ticketId;
            string? rawJsonReport = null;
            ResultadoRevisionIA resultadoFinal = ResultadoRevisionIA.Valido;
            decimal porcentajeFinal = 90m;

            // Esperar procesamiento de la cola (hasta 3 minutos)
            var maxReintentos = 90;
            for (var i = 0; i < maxReintentos; i++)
            {
                await Task.Delay(2000, ct);
                var respStatus = await _cliente.GetAsync(statusEndpoint, ct);
                respStatus.EnsureSuccessStatusCode();

                var contenidoStatus = await respStatus.Content.ReadAsStringAsync(ct);
                using var docStatus = JsonDocument.Parse(contenidoStatus);
                var estado = docStatus.RootElement.GetProperty("estado").GetString();

                if (estado == "COMPLETADO")
                {
                    rawJsonReport = contenidoStatus;
                    if (docStatus.RootElement.TryGetProperty("resumen_ejecutivo", out var resumen) && resumen.ValueKind == JsonValueKind.Object)
                    {
                        var semaforo = resumen.TryGetProperty("semaforo", out var s) ? s.GetString()?.ToUpperInvariant() : "VERDE";
                        resultadoFinal = semaforo switch
                        {
                            "VERDE" => ResultadoRevisionIA.Valido,
                            "AMARILLO" => ResultadoRevisionIA.ConObservaciones,
                            _ => ResultadoRevisionIA.NoValido,
                        };

                        var pCoh = resumen.TryGetProperty("puntaje_coherencia", out var coh) ? coh.GetDecimal() : 8m;
                        var pOrt = resumen.TryGetProperty("puntaje_ortografia", out var ort) ? ort.GetDecimal() : 8m;
                        porcentajeFinal = Math.Clamp(Math.Round((pCoh + pOrt) * 5, 2), 0m, 100m);
                    }
                    break;
                }

                if (estado == "ERROR")
                {
                    var errorDetalle = docStatus.RootElement.TryGetProperty("error", out var err) ? err.GetString() : "Error durante la auditoría.";
                    throw new HttpRequestException($"La auditoría de IA falló: {errorDetalle}");
                }
            }

            if (rawJsonReport == null)
            {
                throw new HttpRequestException("El tiempo de espera para la revisión de IA se agotó.");
            }

            return new RevisionIAResultado(resultadoFinal, porcentajeFinal, rawJsonReport);
        }
        catch (Exception)
        {
            // Fallback: Si el microservicio externo (puerto 8000) no responde o está apagado,
            // generar el informe analizando el contenido real del archivo subido.
            var rutaFisica = _almacen.ObtenerRutaFisica(nombreFisico);
            return GeneradorReporteIASimulado.Generar(folio, tesis.Titulo, version, rutaFisica);
        }
    }
}
