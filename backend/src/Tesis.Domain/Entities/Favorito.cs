using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class Favorito : EntidadAuditable
{
    public int IdFavorito { get; set; }
    public int IdUsuario { get; set; }
    public Guid Folio { get; set; }

    public Usuario? Usuario { get; set; }
    public Tesis? Tesis { get; set; }
}