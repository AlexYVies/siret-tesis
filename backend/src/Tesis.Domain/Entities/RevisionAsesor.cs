using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class RevisionAsesor : EntidadAuditable
{
    public int IdRevisionAsesor { get; set; }
    public Guid Folio { get; set; }
    public int IdProfesor { get; set; }
    public int Version { get; set; }
    public bool EsUltimaVersion { get; set; }
    public DecisionRevisionAsesor Decision { get; set; }
    public string? Observaciones { get; set; }
    public DateTime Fecha { get; set; }

    public Tesis? Tesis { get; set; }
    public Profesor? Profesor { get; set; }
}