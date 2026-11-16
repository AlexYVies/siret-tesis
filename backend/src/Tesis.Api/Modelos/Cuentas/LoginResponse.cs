namespace Tesis.Api.Modelos.Cuentas;

public record LoginResponse(
    string Token,
    DateTime ExpiraEn,
    int IdUsuario,
    string Correo,
    IReadOnlyList<string> Roles,
    string? Nombre = null,
    string? NombreCorto = null,
    string? InicialApellido = null);