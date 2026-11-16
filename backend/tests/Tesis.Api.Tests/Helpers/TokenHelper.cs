using Microsoft.Extensions.DependencyInjection;
using Tesis.Api.Core.Servicios;
using Tesis.Domain.Entities;

namespace Tesis.Api.Tests.Helpers;

public static class TokenHelper
{
    public static string Generar(
        FabricacionApi fabricacion,
        int idUsuario,
        string correo,
        params string[] roles)
    {
        using var scope = fabricacion.Services.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var usuario = new Usuario
        {
            Id = idUsuario,
            UserName = correo,
            Email = correo,
        };

        return servicio.Generar(usuario, roles);
    }
}