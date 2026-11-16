using Microsoft.AspNetCore.Identity;

namespace Tesis.Api.Core;

/// <summary>
/// Provee mensajes descriptivos y amigables en español para los errores de ASP.NET Core Identity.
/// </summary>
public class MensajesIdentityEspanol : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Ocurrió un error inesperado al procesar la solicitud." };

    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = "El registro ha sido modificado previamente. Intente de nuevo." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "La contraseña ingresada es incorrecta." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "El enlace o código de seguridad no es válido o ya ha expirado. Por favor, solicita uno nuevo." };

    public override IdentityError LoginAlreadyAssociated() =>
        new() { Code = nameof(LoginAlreadyAssociated), Description = "Ya existe un usuario asociado a este inicio de sesión." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = $"El usuario o correo institucional '{userName}' no tiene un formato válido." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = $"El formato del correo institucional '{email}' no es válido." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"El correo institucional '{userName}' ya se encuentra registrado en el sistema." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"El correo institucional '{email}' ya se encuentra registrado en el sistema." };

    public override IdentityError InvalidRoleName(string? role) =>
        new() { Code = nameof(InvalidRoleName), Description = $"El rol '{role}' no es válido." };

    public override IdentityError DuplicateRoleName(string role) =>
        new() { Code = nameof(DuplicateRoleName), Description = $"El rol '{role}' ya existe." };

    public override IdentityError UserAlreadyHasPassword() =>
        new() { Code = nameof(UserAlreadyHasPassword), Description = "El usuario ya cuenta con una contraseña establecida." };

    public override IdentityError UserLockoutNotEnabled() =>
        new() { Code = nameof(UserLockoutNotEnabled), Description = "El bloqueo de seguridad no está habilitado para este usuario." };

    public override IdentityError UserAlreadyInRole(string role) =>
        new() { Code = nameof(UserAlreadyInRole), Description = $"El usuario ya tiene asignado el rol '{role}'." };

    public override IdentityError UserNotInRole(string role) =>
        new() { Code = nameof(UserNotInRole), Description = $"El usuario no tiene asignado el rol '{role}'." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe contener al menos {length} caracteres." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe incluir al menos un símbolo o carácter especial (ej. !@#$%&*)." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe incluir al menos un número (0-9)." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe incluir al menos una letra minúscula ('a'-'z')." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe incluir al menos una letra mayúscula ('A'-'Z')." };
}
