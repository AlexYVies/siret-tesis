using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tesis.Api.Modelos.Cuentas;
using Tesis.Api.Tests.Helpers;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class DirectoresControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private int _siguienteId = 7000;

    public DirectoresControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task GetDirectores_SinAutenticacion_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();
        var respuesta = await cliente.GetAsync("/api/usuarios/directores");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task GetDirectores_ConRolAlumno_Devuelve403()
    {
        var cliente = Cliente(++_siguienteId, "alumno@upt.edu.mx", "Alumno");
        var respuesta = await cliente.GetAsync("/api/usuarios/directores");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task GetDirectores_ConRolAdministrador_Devuelve200()
    {
        var cliente = Cliente(++_siguienteId, "admin@upt.edu.mx", "Administrador");
        var respuesta = await cliente.GetAsync("/api/usuarios/directores");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostDirector_ConCorreoInvalido_Devuelve400()
    {
        var cliente = Cliente(++_siguienteId, "admin@upt.edu.mx", "Administrador");
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new { correo = "director@gmail.com", nombre = "Dr. Prueba" }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/usuarios/directores", cuerpo);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostDirector_ConDatosValidos_Devuelve201()
    {
        var cliente = Cliente(++_siguienteId, "admin@upt.edu.mx", "Administrador");
        var correoNuevo = $"director.nuevo.{Guid.NewGuid():N}@upt.edu.mx";
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new { correo = correoNuevo, nombre = "Dr. Roberto Mendoza", contrasena = "Admin.123" }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/usuarios/directores", cuerpo);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var json = await respuesta.Content.ReadAsStringAsync();
        var creado = JsonSerializer.Deserialize<DirectorDto>(json, _json);
        Assert.NotNull(creado);
        Assert.Equal(correoNuevo, creado.Correo);
        Assert.Equal("Dr. Roberto Mendoza", creado.Nombre);
    }

    private HttpClient Cliente(int idUsuario, string correo, string rol)
    {
        var token = TokenHelper.Generar(_fabricacion, idUsuario, correo, rol);
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}
