namespace Tesis.Application.Modelos;

public sealed record ReferenciaDetectadaDto(
    string Nombre,
    string Url,
    string? Doi,
    int? IdTipoArticulo,
    string? TipoArticulo,
    string CitaOriginal,
    bool YaRegistrado,
    bool EsSospechosa = false,
    string? MotivoSospecha = null,
    string? EstadoVerificacion = null,
    bool? CoincideContenido = null,
    bool? CumpleApa7 = null,
    string? ObservacionApa7 = null);

public sealed record ImportarReferenciasItemRequest(
    string Nombre,
    string Url,
    string? Doi,
    int? IdTipoArticulo);

public sealed record ImportarReferenciasRequest(
    IReadOnlyList<ImportarReferenciasItemRequest> Referencias);

