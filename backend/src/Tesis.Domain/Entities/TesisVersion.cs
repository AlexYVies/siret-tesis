namespace Tesis.Domain.Entities;

public class TesisVersion
{
    public int Id { get; set; }
    public Guid Folio { get; set; }
    public int Version { get; set; }
    public string RutaDocumento { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? IdUsuario { get; set; }

    public Tesis? Tesis { get; set; }
    public Usuario? Usuario { get; set; }
}
