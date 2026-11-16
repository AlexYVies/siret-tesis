using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tesis.Api.Tests.Helpers;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class TesisControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public TesisControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task PostDocumento_SinTokenJwt_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();

        var respuesta = await cliente.PostAsync(
            $"/api/tesis/{Guid.NewGuid()}/documento",
            CrearArchivoAdjunto());

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostDocumento_ConRolIncorrecto_Devuelve403()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDirector, "rector@upt.edu.mx", "Director de posgrado"));

        var respuesta = await cliente.PostAsync(
            $"/api/tesis/{Guid.NewGuid()}/documento",
            CrearArchivoAdjunto());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostDocumento_SinConfirmacionesPrevias_Devuelve409()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", "Alumno"));

        var folio = await RegistrarTesisAsync(cliente);

        var respuesta = await cliente.PostAsync(
            $"/api/tesis/{folio}/documento",
            CrearArchivoAdjunto());

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("CONFLICTO_DE_NEGOCIO", cuerpo.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostRegistrarTesis_Devuelve201()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", "Alumno"));

        var folio = await RegistrarTesisAsync(cliente);

        Assert.False(string.IsNullOrWhiteSpace(folio));
    }

    [Fact]
    public async Task PostRegistrarTesis_CuandoYaTieneTesisEnProceso_Devuelve409()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", "Alumno"));

        await RegistrarTesisAsync(cliente);

        // Intentar registrar una segunda tesis sin haber terminado la primera
        var contenido = new StringContent(
            JsonSerializer.Serialize(new
            {
                titulo = "Segunda tesis intento",
                nivel = "Maestría",
                programa = "Maestría en Tecnologías de la Información",
                idAsesor = FabricacionApi.IdProfesorAsesor,
                idCoAsesor = (int?)null,
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/tesis", contenido);
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    private async Task<string> RegistrarTesisAsync(HttpClient cliente)
    {
        _fabricacion.ReiniciarTesis();
        var contenido = new StringContent(
            JsonSerializer.Serialize(new
            {
                titulo = "Sistemas embebidos para monitoreo ambiental",
                nivel = "Maestría",
                programa = "Maestría en Tecnologías de la Información",
                idAsesor = FabricacionApi.IdProfesorAsesor,
                idCoAsesor = (int?)null,
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/tesis", contenido);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return cuerpo.RootElement.GetProperty("folio").GetString()!;
    }

    private static MultipartFormDataContent CrearArchivoAdjunto()
    {
        var contenido = new MultipartFormDataContent();
        var archivo = new ByteArrayContent("contenido de la tesis"u8.ToArray());
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        contenido.Add(archivo, "archivo", "tesis.pdf");
        return contenido;
    }
}