using Microsoft.AspNetCore.Identity;
using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class Usuario : IdentityUser<int>, IAuditableEntity
{
    public DateTime FechaReg { get; set; }
    public DateTime FechaMod { get; set; }

    public List<UsuarioRol> UsuarioRoles { get; } = new();

    public Alumno? Alumno { get; set; }
    public Profesor? Profesor { get; set; }
}