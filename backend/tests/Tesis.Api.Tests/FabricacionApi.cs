using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tesis.Application.Abstracciones;
using Tesis.Application.Servicios;
using Tesis.Application.Tests.Fakes;
using Tesis.Domain.Entities;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Api.Tests;

public sealed class FabricacionApi : WebApplicationFactory<Program>
{
    public const int IdUsuarioAlumno = 1001;
    public const int IdUsuarioDocente = 2002;
    public const int IdUsuarioDocente2 = 2003;
    public const int IdUsuarioDirector = 3003;
    public const int IdAlumno = 501;
    public const int IdProfesorAsesor = 101;
    public const int IdProfesorCoAsesor = 102;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TesisDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<TesisDbContext>(opciones =>
                opciones.UseInMemoryDatabase($"TesisUptTest_{Guid.NewGuid():N}"));

            services.AddSingleton<ITesisRepository, RepositorioTesisEnMemoria>();
            services.AddSingleton<IProfesorRepository>(_ => RepositorioProfesoresEnMemoria.Sembrado());
            services.AddSingleton<IPosgradoRepository, RepositorioPosgradosEnMemoria>();
            services.AddSingleton<IAlumnoRepository>(_ => new RepositorioAlumnosEnMemoria(
                new Alumno { IdAlumno = IdAlumno, IdUsuario = IdUsuarioAlumno, Nombre = "María López", NumeroControl = "U2026001", Correo = "maria.lopez@upt.edu.mx" }));
            services.AddSingleton<ITipoArticuloRepository, RepositorioTiposArticuloEnMemoria>();
            services.AddSingleton<IUnitOfWork, UnidadDeTrabajoNop>();
            services.AddSingleton<IServicioRevisionIA, ServicioRevisionIAEstable>();
            services.AddSingleton<IFavoritoRepository>(sp =>
                new FavoritosEnMemoria(sp.GetRequiredService<ITesisRepository>()));

            services.AddScoped<INotificadorAsesoria, NotificadorAsesoriaNulo>();
            services.AddScoped<ITesisService, TesisService>();
        });

        builder.UseEnvironment("Development");
    }

    public void ReiniciarTesis()
    {
        if (Services.GetService<ITesisRepository>() is RepositorioTesisEnMemoria repo)
        {
            repo.Limpiar();
        }
    }
}