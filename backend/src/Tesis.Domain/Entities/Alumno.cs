using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class Alumno : EntidadAuditable
{
    public int IdAlumno { get; set; }
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NumeroControl { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    public Usuario? Usuario { get; set; }
    public List<Tesis> Tesis { get; } = new();
}