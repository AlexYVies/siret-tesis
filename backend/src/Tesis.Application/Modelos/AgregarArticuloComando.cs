namespace Tesis.Application.Modelos;

public record AgregarArticuloComando(
    Guid Folio,
    string Nombre,
    string Url,
    string? Doi,
    int? IdTipoArticulo);