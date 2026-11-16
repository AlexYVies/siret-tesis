using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tesis.Api.Tests.Helpers;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class FavoritosControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public FavoritosControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task PostFavorito_SinTokenJwt_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();

        var respuesta = await cliente.PostAsync(
            $"/api/repositorio/{Guid.NewGuid()}/favorito",
            null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostFavorito_FolioInexistente_Devuelve404()
    {
        var cliente = ClienteAlumno();

        var respuesta = await cliente.PostAsync(
            $"/api/repositorio/{Guid.NewGuid()}/favorito",
            null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostFavorito_TesisNoAceptada_Devuelve403()
    {
        var cliente = ClienteAlumno();
        var folio = await RegistrarTesisAsync(cliente);

        var respuesta = await cliente.PostAsync(
            $"/api/repositorio/{folio}/favorito",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("OPERACION_NO_PERMITIDA", cuerpo.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostFavorito_TesisAceptada_Devuelve201()
    {
        var cliente = ClienteAlumno();
        var folio = await LlevarAceptadaAsync();

        var respuesta = await cliente.PostAsync(
            $"/api/repositorio/{folio}/favorito",
            null);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostFavorito_Repetido_Devuelve200()
    {
        var cliente = ClienteAlumno();
        var folio = await LlevarAceptadaAsync();

        await cliente.PostAsync($"/api/repositorio/{folio}/favorito", null);
        var segunda = await cliente.PostAsync($"/api/repositorio/{folio}/favorito", null);

        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
    }

    [Fact]
    public async Task GetFavoritos_DeOtroUsuario_Devuelve403()
    {
        var cliente = ClienteAlumno();

        var respuesta = await cliente.GetAsync(
            $"/api/usuarios/{FabricacionApi.IdUsuarioDirector}/favoritos");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task GetFavoritos_LocalizaElFavorito()
    {
        var cliente = ClienteAlumno();
        var folio = await LlevarAceptadaAsync();
        await cliente.PostAsync($"/api/repositorio/{folio}/favorito", null);

        var respuesta = await cliente.GetAsync(
            $"/api/usuarios/{FabricacionApi.IdUsuarioAlumno}/favoritos");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var folios = cuerpo.RootElement
            .EnumerateArray()
            .Select(e => e.GetProperty("folio").GetGuid())
            .ToList();
        Assert.Contains(Guid.Parse(folio), folios);
    }

    [Fact]
    public async Task DeleteFavorito_SinTokenJwt_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();

        var respuesta = await cliente.DeleteAsync(
            $"/api/repositorio/{Guid.NewGuid()}/favorito");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task DeleteFavorito_SinFavoritoPrevio_Devuelve200()
    {
        var cliente = ClienteAlumno();

        var respuesta = await cliente.DeleteAsync(
            $"/api/repositorio/{Guid.NewGuid()}/favorito");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task DeleteFavorito_QuitaElFavorito()
    {
        var cliente = ClienteAlumno();
        var folio = await LlevarAceptadaAsync();
        await cliente.PostAsync($"/api/repositorio/{folio}/favorito", null);

        var respuesta = await cliente.DeleteAsync($"/api/repositorio/{folio}/favorito");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var listado = await cliente.GetAsync(
            $"/api/usuarios/{FabricacionApi.IdUsuarioAlumno}/favoritos");
        using var cuerpo = JsonDocument.Parse(await listado.Content.ReadAsStringAsync());
        var folios = cuerpo.RootElement
            .EnumerateArray()
            .Select(e => e.GetProperty("folio").GetGuid())
            .ToList();
        Assert.DoesNotContain(Guid.Parse(folio), folios);
    }

    private HttpClient ClienteAlumno()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", "Alumno"));
        return cliente;
    }

    private async Task<string> RegistrarTesisAsync(HttpClient clienteAlumno)
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

        var respuesta = await clienteAlumno.PostAsync("/api/tesis", contenido);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return cuerpo.RootElement.GetProperty("folio").GetString()!;
    }

    private async Task<string> LlevarAceptadaAsync()
    {
        var alumno = ClienteAlumno();
        var folio = await RegistrarTesisAsync(alumno);

        var docente = ClienteDocente();
        await ConfirmarAsesoriaAsync(docente, folio);

        await alumno.PostAsync(
            $"/api/tesis/{folio}/documento",
            CrearArchivoAdjunto());

        await EjecutarRevisionesAsync(docente, folio);

        var director = ClienteDirector();
        await EmitirDictamenAsync(director, folio);

        return folio;
    }

    private HttpClient ClienteDocente()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDocente, "juan.perez@upt.edu.mx", "Docente"));
        return cliente;
    }

    private HttpClient ClienteDirector()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDirector, "rector@upt.edu.mx", "Director de posgrado"));
        return cliente;
    }

    private async Task ConfirmarAsesoriaAsync(HttpClient docente, string folio)
    {
        var confirmacion = new StringContent(
            JsonSerializer.Serialize(new
            {
                tipo = "Asesor",
                decision = "Confirmada",
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await docente.PostAsync($"/api/tesis/{folio}/confirmaciones", confirmacion);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private async Task EjecutarRevisionesAsync(HttpClient docente, string folio)
    {
        var revisionIa = new StringContent(
            JsonSerializer.Serialize(new { esUltimaVersion = true }, _json),
            Encoding.UTF8,
            "application/json");
        var respuesta = await docente.PostAsync($"/api/tesis/{folio}/revision-ia", revisionIa);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var revisionAsesor = new StringContent(
            JsonSerializer.Serialize(new
            {
                esUltimaVersion = true,
                decision = "Aceptada",
                observaciones = (string?)null,
            }, _json),
            Encoding.UTF8,
            "application/json");
        respuesta = await docente.PostAsync($"/api/tesis/{folio}/revision-asesor", revisionAsesor);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private async Task EmitirDictamenAsync(HttpClient director, string folio)
    {
        var dictamen = new StringContent(
            JsonSerializer.Serialize(new
            {
                resultado = "Aceptada",
                observaciones = (string?)null,
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await director.PostAsync($"/api/tesis/{folio}/dictamen", dictamen);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
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