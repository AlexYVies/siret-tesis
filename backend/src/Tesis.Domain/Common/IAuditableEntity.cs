namespace Tesis.Domain.Common;

public interface IAuditableEntity
{
    DateTime FechaReg { get; set; }
    DateTime FechaMod { get; set; }
}