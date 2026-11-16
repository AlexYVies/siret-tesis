using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class RevisionIA : EntidadAuditable
{
    public int IdRevision { get; set; }
    public Guid Folio { get; set; }
    public int Version { get; set; }
    public ResultadoRevisionIA Resultado { get; set; }
    public decimal Porcentaje { get; set; }
    public DateTime Fecha { get; set; }
    public string? ReporteJson { get; set; }

    public Tesis? Tesis { get; set; }
}