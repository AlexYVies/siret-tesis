using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tesis.Api.Tests.Helpers;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class TiposArticuloControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private int _contador;

    public TiposArticuloControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task PostTipoArticulo_SinTokenJwt_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();

        var respuesta = await cliente.PostAsync("/api/tipos-articulo", Cuerpo("Artículo de revista"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostTipoArticulo_ConRolDocente_Devuelve403()
    {
        var cliente = Cliente(2002, "juan.perez@upt.edu.mx", "Docente");

        var respuesta = await cliente.PostAsync("/api/tipos-articulo", Cuerpo("Artículo de revista"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostTipoArticulo_PorAdministrador_SinNombre_Devuelve400()
    {
        var cliente = ClienteAdministrador();

        var respuesta = await cliente.PostAsync("/api/tipos-articulo", Cuerpo(""));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostTipoArticulo_PorAdministrador_Duplicado_Devuelve409()
    {
        var cliente = ClienteAdministrador();
        var nombre = $"Artículo duplicado {_contador++}";

        await cliente.PostAsync("/api/tipos-articulo", Cuerpo(nombre));
        var segunda = await cliente.PostAsync("/api/tipos-articulo", Cuerpo(nombre));

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task PostTipoArticulo_PorAdministrador_Valido_Devuelve201()
    {
        var cliente = ClienteAdministrador();

        var respuesta = await cliente.PostAsync("/api/tipos-articulo", Cuerpo("Artículo de conferencia"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.True(cuerpo.RootElement.GetProperty("idTipoArticulo").GetInt32() > 0);
        Assert.Equal("Artículo de conferencia", cuerpo.RootElement.GetProperty("nombre").GetString());
    }

    private HttpClient ClienteAdministrador()
    {
        return Cliente(9999, "admin@upt.edu.mx", "Administrador");
    }

    private HttpClient Cliente(int idUsuario, string correo, string rol)
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, idUsuario, correo, rol));
        return cliente;
    }

    private static StringContent Cuerpo(string nombre)
    {
        return new StringContent(
            JsonSerializer.Serialize(new { nombre, descripcion = "Tipo de publicación académica." }),
            Encoding.UTF8,
            "application/json");
    }
}