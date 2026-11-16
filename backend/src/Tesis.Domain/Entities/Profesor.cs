using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class Profesor : EntidadAuditable
{
    public int IdProfesor { get; set; }
    public int? IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }
    public List<ProfesorPosgrado> ProgramasPosgrado { get; } = new();
    public List<Tesis> TesisAsesoradas { get; } = new();
    public List<Tesis> TesisCoAsesoradas { get; } = new();
    public List<ConfirmacionAsesoria> Confirmaciones { get; } = new();
}