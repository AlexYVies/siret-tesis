namespace Tesis.Domain.Common;

public abstract class EntidadAuditable : IAuditableEntity
{
    public DateTime FechaReg { get; set; }
    public DateTime FechaMod { get; set; }
}