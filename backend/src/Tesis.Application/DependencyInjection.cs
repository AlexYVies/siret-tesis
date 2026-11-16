using Microsoft.Extensions.DependencyInjection;
using Tesis.Application.Abstracciones;
using Tesis.Application.Servicios;

namespace Tesis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<ITesisService, TesisService>();

        return services;
    }
}