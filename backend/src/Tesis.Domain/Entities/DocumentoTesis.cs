using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class DocumentoTesis : EntidadAuditable
{
    public int IdDocumento { get; set; }
    public Guid Folio { get; set; }
    public int Version { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Tesis? Tesis { get; set; }
}