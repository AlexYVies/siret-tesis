using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class Articulo : EntidadAuditable
{
    public int IdArticulo { get; set; }
    public Guid Folio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Doi { get; set; }
    public string? Issn { get; set; }
    public int? IdTipoArticulo { get; set; }

    public Tesis? Tesis { get; set; }
    public TipoArticulo? TipoArticulo { get; set; }

    public Articulo() { }

    public Articulo(string nombre, string url, string? doi, int? idTipoArticulo, string? issn = null)
    {
        Guardia.Exigir(!string.IsNullOrWhiteSpace(nombre), "El nombre del artículo es obligatorio.");
        var esUrlValida = Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                          (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        Guardia.Exigir(esUrlValida, "La URL del artículo no es válida.");

        Nombre = nombre.Trim();
        Url = url;
        Doi = doi;
        Issn = issn;
        IdTipoArticulo = idTipoArticulo;
    }
}