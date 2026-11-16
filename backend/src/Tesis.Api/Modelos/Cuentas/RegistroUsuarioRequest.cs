namespace Tesis.Api.Modelos.Cuentas;

public record RegistroUsuarioRequest(
    string Correo,
    string? Contrasena = null,
    string Rol = "Alumno",
    string? Nombre = null,
    string? Matricula = null,
    string? Nombres = null,
    string? PrimerApellido = null,
    string? SegundoApellido = null);

public record InvitarDocenteRequest(
    string Correo,
    string Nombre,
    int? IdProfesor = null);

public record CompletarRegistroDocenteRequest(
    int IdUsuario,
    string Correo,
    string Nombre,
    string Contrasena,
    string Token,
    string? Nombres = null,
    string? PrimerApellido = null,
    string? SegundoApellido = null);

public record DirectorDto(
    int IdUsuario,
    string Correo,
    string Nombre,
    bool EmailConfirmed,
    DateTime? FechaRegistro);

public record SolicitudDirectorRequest(
    string Correo,
    string? Nombre = null,
    string? Contrasena = null);

public record CambiarContrasenaRequest(
    string ContrasenaActual,
    string NuevaContrasena);

public record MiPerfilResponse(
    int IdUsuario,
    string Correo,
    string NombreCompleto,
    string PrimerNombre,
    string PrimerApellido,
    string? SegundoApellido,
    string NombreCorto,
    string InicialApellido,
    string? NumeroControl,
    IReadOnlyList<string> Roles);
