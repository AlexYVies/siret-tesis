using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class ProfesorPosgrado : EntidadAuditable
{
    public int IdProfesorPosgrado { get; set; }
    public int IdProfesor { get; set; }
    public NivelPosgrado Nivel { get; set; }
    public string Programa { get; set; } = string.Empty;

    public Profesor? Profesor { get; set; }
}