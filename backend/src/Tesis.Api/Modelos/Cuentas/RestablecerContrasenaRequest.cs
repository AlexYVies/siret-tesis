namespace Tesis.Api.Modelos.Cuentas;

public record RestablecerContrasenaRequest(int IdUsuario, string Token, string NuevaContrasena);