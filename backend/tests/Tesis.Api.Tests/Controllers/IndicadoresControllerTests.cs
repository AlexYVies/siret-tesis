using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Tesis.Api.Tests.Helpers;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class IndicadoresControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;

    public IndicadoresControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task Obtener_ConRolAlumno_Devuelve403()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", "Alumno"));

        var respuesta = await cliente.GetAsync("/api/indicadores");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Obtener_ConRolAdministrador_Devuelve403()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, 9999, "admin@upt.edu.mx", "Administrador"));

        var respuesta = await cliente.GetAsync("/api/indicadores");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Obtener_ConRolDirector_Devuelve200()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDirector, "director@upt.edu.mx", "Director de posgrado"));

        var respuesta = await cliente.GetAsync("/api/indicadores");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.True(cuerpo.RootElement.GetProperty("total").GetInt32() >= 0);
        Assert.True(cuerpo.RootElement.TryGetProperty("porNivel", out var porNivel));
        Assert.True(cuerpo.RootElement.TryGetProperty("porPrograma", out var porPrograma));
    }
}