using Microsoft.AspNetCore.Identity;
using Tesis.Domain.Common;

namespace Tesis.Domain.Entities;

public class UsuarioRol : IdentityUserRole<int>, IAuditableEntity
{
    public DateTime FechaReg { get; set; }
    public DateTime FechaMod { get; set; }

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
}