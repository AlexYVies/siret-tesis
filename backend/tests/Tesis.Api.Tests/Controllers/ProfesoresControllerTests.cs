using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tesis.Api.Tests.Helpers;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class ProfesoresControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private int _siguienteId = 2500;

    public ProfesoresControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task PostProfesor_SinTokenJwt_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();

        var respuesta = await cliente.PostAsync("/api/profesores", Cuerpo("Dra. Ana Torres"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostProfesor_ConRolAlumno_Devuelve403()
    {
        var cliente = Cliente(IdUsuarioAlumno(), "maria.lopez@upt.edu.mx", "Alumno");

        var respuesta = await cliente.PostAsync("/api/profesores", Cuerpo("Dra. Ana Torres"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostProfesor_ConRolDocente_SinNombre_Devuelve400()
    {
        var cliente = ClienteDocente();

        var respuesta = await cliente.PostAsync("/api/profesores", Cuerpo(""));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostProfesor_ConRolDocente_ProgramaVacio_Devuelve400()
    {
        var cliente = ClienteDocente();

        var respuesta = await cliente.PostAsync(
            "/api/profesores",
            Cuerpo("Dra. Ana Torres", programaVacio: true));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostProfesor_ConRolDocente_Duplicado_Devuelve409()
    {
        var cliente = ClienteDocente();

        await cliente.PostAsync("/api/profesores", Cuerpo("Dra. Ana Torres"));
        var segunda = await cliente.PostAsync("/api/profesores", Cuerpo("Dra. Ana Torres"));

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task PostProfesor_ConRolDocente_Valido_Devuelve201()
    {
        var cliente = ClienteDocente();

        var respuesta = await cliente.PostAsync("/api/profesores", Cuerpo("Dra. Ana Torres"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.True(cuerpo.RootElement.GetProperty("idProfesor").GetInt32() > 0);
        Assert.Equal("Dra. Ana Torres", cuerpo.RootElement.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task PostProfesor_ConRolAdministrador_Valido_Devuelve201()
    {
        var cliente = Cliente(3003, "rector@upt.edu.mx", "Administrador");

        var respuesta = await cliente.PostAsync("/api/profesores", Cuerpo("Mtro. Luis Gómez"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task PutPosgradosProfesor_SinToken_Devuelve401()
    {
        var cliente = _fabricacion.CreateClient();
        var cuerpo = new StringContent("{}", Encoding.UTF8, "application/json");

        var respuesta = await cliente.PutAsync("/api/profesores/101/posgrados", cuerpo);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task PutPosgradosProfesor_ComoAlumno_Devuelve403()
    {
        var cliente = Cliente(IdUsuarioAlumno(), "maria.lopez@upt.edu.mx", "Alumno");
        var cuerpo = new StringContent("{}", Encoding.UTF8, "application/json");

        var respuesta = await cliente.PutAsync("/api/profesores/101/posgrados", cuerpo);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task PutPosgradosProfesor_ComoAdmin_ActualizaPosgradosExitosamente()
    {
        var cliente = Cliente(3003, "admin@upt.edu.mx", "Administrador");
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new
            {
                posgrados = new object[]
                {
                    new { nivel = "Maestría", programa = "Maestría en Tecnologías de la Información" },
                    new { nivel = "Doctorado", programa = "Doctorado en Ciencias de la Ingeniería" },
                },
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PutAsync($"/api/profesores/{FabricacionApi.IdProfesorAsesor}/posgrados", cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var programas = doc.RootElement.GetProperty("programas");
        Assert.Equal(2, programas.GetArrayLength());
    }

    [Fact]
    public async Task PutPosgradosProfesor_ConListaVacia_Devuelve400()
    {
        var cliente = Cliente(3003, "admin@upt.edu.mx", "Administrador");
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new
            {
                posgrados = Array.Empty<object>(),
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PutAsync($"/api/profesores/{FabricacionApi.IdProfesorAsesor}/posgrados", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PutPosgradosProfesor_ConDuplicados_Devuelve400()
    {
        var cliente = Cliente(3003, "admin@upt.edu.mx", "Administrador");
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new
            {
                posgrados = new object[]
                {
                    new { nivel = "Maestría", programa = "Maestría en TI" },
                    new { nivel = "Maestría", programa = "Maestría en TI" },
                },
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PutAsync($"/api/profesores/{FabricacionApi.IdProfesorAsesor}/posgrados", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }


    private HttpClient ClienteDocente()
    {
        var idUsuario = _siguienteId++;
        return Cliente(idUsuario, $"docente{idUsuario}@upt.edu.mx", "Docente");
    }

    private int IdUsuarioAlumno() => FabricacionApi.IdUsuarioAlumno;

    private HttpClient Cliente(int idUsuario, string correo, string rol)
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, idUsuario, correo, rol));
        return cliente;
    }

    private StringContent Cuerpo(string nombre, bool programaVacio = false)
    {
        return new StringContent(
            JsonSerializer.Serialize(new
            {
                nombre,
                posgrados = new object[]
                {
                    new
                    {
                        nivel = "Maestría",
                        programa = programaVacio ? "" : "Maestría en Tecnologías de la Información",
                    },
                },
            }, _json),
            Encoding.UTF8,
            "application/json");
    }
}