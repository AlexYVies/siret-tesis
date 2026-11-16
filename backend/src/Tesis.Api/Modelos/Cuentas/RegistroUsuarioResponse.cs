namespace Tesis.Api.Modelos.Cuentas;

public record RegistroUsuarioResponse(
    int IdUsuario,
    string? TokenValidacion,
    string? Token = null,
    DateTime? ExpiraEn = null,
    string? Correo = null,
    string[]? Roles = null,
    string? Nombre = null);