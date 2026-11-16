using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class TipoArticulo : EntidadAuditable
{
    public int IdTipoArticulo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public List<Articulo> Articulos { get; } = new();
}