using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tesis.Application.Abstracciones;
using Tesis.Infrastructure.Persistence;
using Tesis.Infrastructure.Persistence.Repositorios;
using Tesis.Infrastructure.Servicios;

namespace Tesis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TesisDb");
        services.AddDbContext<TesisDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ITesisRepository, RepositorioTesis>();
        services.AddScoped<IProfesorRepository, RepositorioProfesores>();
        services.AddScoped<IAlumnoRepository, RepositorioAlumnos>();
        services.AddScoped<IPosgradoRepository, RepositorioPosgrados>();
        services.AddScoped<ITipoArticuloRepository, RepositorioTipoArticulo>();
        services.AddScoped<IFavoritoRepository, RepositorioFavoritos>();
        services.AddScoped<IUnitOfWork, UnidadDeTrabajoEf>();
        services.AddScoped<ICorreoService, CorreoServiceSmtp>();
        services.AddScoped<IAlmacenDocumentos, AlmacenDocumentosLocal>();
        services.AddScoped<INotificadorAsesoria, NotificadorAsesoriaSmtp>();
        services.AddScoped<IExtractorReferenciasPdf, ExtractorReferenciasPdf>();

        var modoStub = bool.TryParse(configuration["RevisionIA:ModoStub"], out var valor) && valor;
        if (modoStub)
        {
            services.AddScoped<IServicioRevisionIA, ServicioRevisionIAEstub>();
        }
        else
        {
            services.AddScoped<IServicioRevisionIA, ServicioRevisionIAHttp>();
            services.AddSingleton(_ => new HttpClient());
        }

        return services;
    }
}