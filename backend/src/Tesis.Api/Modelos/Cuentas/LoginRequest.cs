namespace Tesis.Api.Modelos.Cuentas;

public record LoginRequest(string Correo, string Contrasena, string? RolEsperado = null);

public sealed record LoginInvitadoRequest(string? Correo = null);