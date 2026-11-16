namespace Tesis.Api.Modelos.Tesis;

public record AgregarArticuloRequest(
    string Nombre,
    string Url,
    string? Doi,
    int? IdTipoArticulo);