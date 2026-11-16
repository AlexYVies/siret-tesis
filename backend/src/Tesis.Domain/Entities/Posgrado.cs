using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class Posgrado : EntidadAuditable
{
    public int IdPosgrado { get; set; }
    public NivelPosgrado Nivel { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
