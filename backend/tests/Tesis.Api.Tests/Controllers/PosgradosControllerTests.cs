using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tesis.Api.Modelos.Consultas;
using Tesis.Api.Serializacion;
using Tesis.Api.Tests.Helpers;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;
using Xunit;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Api.Tests.Controllers;

public class PosgradosControllerTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json;

    public PosgradosControllerTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        foreach (var convertidor in SerializacionEnumsEspanol.ConstruirConvertidores())
        {
            _json.Converters.Add(convertidor);
        }
    }


    [Fact]
    public async Task Listar_SinAutenticacion_DevuelveSoloActivos()
    {
        var cliente = _fabricacion.CreateClient();

        var respuesta = await cliente.GetAsync("/api/posgrados");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var posgrados = await JsonSerializer.DeserializeAsync<List<PosgradoDto>>(
            await respuesta.Content.ReadAsStreamAsync(), _json);
        Assert.NotNull(posgrados);
        Assert.All(posgrados, p => Assert.True(p.Activo));
    }

    [Fact]
    public async Task Registrar_ComoAdmin_CreaPosgradoExitosamente()
    {
        var cliente = ClienteAdmin();
        var nombre = $"Maestría en Robótica {Guid.NewGuid():N}";
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new { nivel = "Maestría", nombre }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/posgrados", cuerpo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await JsonSerializer.DeserializeAsync<PosgradoDto>(
            await respuesta.Content.ReadAsStreamAsync(), _json);
        Assert.NotNull(creado);
        Assert.Equal(nombre, creado.Nombre);
        Assert.Equal(NivelPosgrado.Maestria, creado.Nivel);
        Assert.True(creado.Activo);
    }

    [Fact]
    public async Task Registrar_Duplicado_Devuelve409()
    {
        var cliente = ClienteAdmin();
        var nombre = $"Doctorado en IA {Guid.NewGuid():N}";
        var cuerpo = new StringContent(
            JsonSerializer.Serialize(new { nivel = "Doctorado", nombre }, _json),
            Encoding.UTF8,
            "application/json");

        var r1 = await cliente.PostAsync("/api/posgrados", cuerpo);
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);

        var r2 = await cliente.PostAsync("/api/posgrados", cuerpo);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task Modificar_ComoAdmin_ActualizaNombreYNivel()
    {
        var cliente = ClienteAdmin();
        var nombreOriginal = $"Posgrado Original {Guid.NewGuid():N}";
        var posgrado = await CrearPosgradoDirecto(NivelPosgrado.Maestria, nombreOriginal);

        var nuevoNombre = $"Maestría Actualizada {Guid.NewGuid():N}";
        var cuerpoMod = new StringContent(
            JsonSerializer.Serialize(new { nivel = "Maestría", nombre = nuevoNombre }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PutAsync($"/api/posgrados/{posgrado.IdPosgrado}", cuerpoMod);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var actualizado = await JsonSerializer.DeserializeAsync<PosgradoDto>(
            await respuesta.Content.ReadAsStreamAsync(), _json);
        Assert.NotNull(actualizado);
        Assert.Equal(nuevoNombre, actualizado.Nombre);
    }

    [Fact]
    public async Task DarDeBaja_ConTesisEnCurso_BloqueaBajaDevuelve409()
    {
        var cliente = ClienteAdmin();
        var nombre = $"Maestría con Tesis En Curso {Guid.NewGuid():N}";
        var posgrado = await CrearPosgradoDirecto(NivelPosgrado.Maestria, nombre);

        // Crear una tesis en curso (Registrada)
        var tesisRepo = _fabricacion.Services.GetRequiredService<ITesisRepository>();
        var tesis = new EntidadTesis(
            FabricacionApi.IdAlumno,
            "Tesis de prueba en curso",
            NivelPosgrado.Maestria,
            nombre,
            FabricacionApi.IdProfesorAsesor,
            false,
            null);
        await tesisRepo.AgregarAsync(tesis);

        // Intentar dar de baja
        var respuesta = await cliente.DeleteAsync($"/api/posgrados/{posgrado.IdPosgrado}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var jsonRespuesta = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("tesis en curso", jsonRespuesta, StringComparison.OrdinalIgnoreCase);

        // Comprobar que sigue activo
        var posgradoRepo = _fabricacion.Services.GetRequiredService<IPosgradoRepository>();
        var consultado = await posgradoRepo.ObtenerPorIdAsync(posgrado.IdPosgrado);
        Assert.NotNull(consultado);
        Assert.True(consultado.Activo);
    }

    [Fact]
    public async Task DarDeBaja_ConTesisAceptada_DaDeBajaYQuitaDeProfesores()
    {
        var cliente = ClienteAdmin();
        var nombre = $"Maestría con Tesis Aceptada {Guid.NewGuid():N}";
        var posgrado = await CrearPosgradoDirecto(NivelPosgrado.Maestria, nombre);

        // Asignar el posgrado a un profesor
        var profesorRepo = _fabricacion.Services.GetRequiredService<IProfesorRepository>();
        var profesor = await profesorRepo.ObtenerConPosgradosAsync(FabricacionApi.IdProfesorAsesor);
        Assert.NotNull(profesor);
        profesor.ProgramasPosgrado.Add(new ProfesorPosgrado
        {
            IdProfesor = profesor.IdProfesor,
            Nivel = NivelPosgrado.Maestria,
            Programa = nombre,
        });

        // Crear una tesis y marcarla como Aceptada (publicada, no en curso)
        var tesisRepo = _fabricacion.Services.GetRequiredService<ITesisRepository>();
        var tesis = new EntidadTesis(
            FabricacionApi.IdAlumno,
            "Tesis aprobada",
            NivelPosgrado.Maestria,
            nombre,
            FabricacionApi.IdProfesorAsesor,
            false,
            null);
        var conf = new ConfirmacionAsesoria { IdProfesor = FabricacionApi.IdProfesorAsesor, Tipo = TipoConfirmacion.Asesor };
        conf.Confirmar();
        tesis.AgregarConfirmacion(conf);
        tesis.ConfirmarAsesoria();
        tesis.SubirDocumento(new DocumentoTesis { Nombre = "tesis.pdf" });
        tesis.RegistrarRevisionIA(ResultadoRevisionIA.Valido, 95m);
        tesis.AceptarRevision(FabricacionApi.IdProfesorAsesor);
        tesis.EmitirDictamen(FabricacionApi.IdUsuarioDirector, ResultadoDictamen.Aceptada, "Aprobada");
        await tesisRepo.AgregarAsync(tesis);

        // Dar de baja
        var respuesta = await cliente.DeleteAsync($"/api/posgrados/{posgrado.IdPosgrado}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = await JsonSerializer.DeserializeAsync<ResultadoPosgradoAccionDto>(
            await respuesta.Content.ReadAsStreamAsync(), _json);
        Assert.NotNull(resultado);
        Assert.NotNull(resultado.Posgrado);
        Assert.False(resultado.Posgrado.Activo); // Se dio de baja

        // Verificar que el posgrado se eliminó de los profesores
        var profActualizado = await profesorRepo.ObtenerConPosgradosAsync(FabricacionApi.IdProfesorAsesor);
        Assert.NotNull(profActualizado);
        Assert.DoesNotContain(profActualizado.ProgramasPosgrado, p => p.Programa == nombre);
    }

    [Fact]
    public async Task Eliminar_SinTesis_EliminaCompletamenteYQuitaDeProfesores()
    {
        var cliente = ClienteAdmin();
        var nombre = $"Posgrado Sin Tesis {Guid.NewGuid():N}";
        var posgrado = await CrearPosgradoDirecto(NivelPosgrado.Doctorado, nombre);

        // Asignar al profesor
        var profesorRepo = _fabricacion.Services.GetRequiredService<IProfesorRepository>();
        var profesor = await profesorRepo.ObtenerConPosgradosAsync(FabricacionApi.IdProfesorCoAsesor);
        Assert.NotNull(profesor);
        profesor.ProgramasPosgrado.Add(new ProfesorPosgrado
        {
            IdProfesor = profesor.IdProfesor,
            Nivel = NivelPosgrado.Doctorado,
            Programa = nombre,
        });

        // Eliminar
        var respuesta = await cliente.DeleteAsync($"/api/posgrados/{posgrado.IdPosgrado}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        // Verificar que ya no existe en la base de datos
        var posgradoRepo = _fabricacion.Services.GetRequiredService<IPosgradoRepository>();
        var consultado = await posgradoRepo.ObtenerPorIdAsync(posgrado.IdPosgrado);
        Assert.Null(consultado);

        // Verificar que se quitó del profesor
        var profActualizado = await profesorRepo.ObtenerConPosgradosAsync(FabricacionApi.IdProfesorCoAsesor);
        Assert.NotNull(profActualizado);
        Assert.DoesNotContain(profActualizado.ProgramasPosgrado, p => p.Programa == nombre);
    }

    private async Task<Posgrado> CrearPosgradoDirecto(NivelPosgrado nivel, string nombre)
    {
        var repo = _fabricacion.Services.GetRequiredService<IPosgradoRepository>();
        var p = new Posgrado
        {
            Nivel = nivel,
            Nombre = nombre,
            Activo = true,
        };
        await repo.AgregarAsync(p);
        return p;
    }

    private HttpClient ClienteAdmin()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, 3003, "admin@upt.edu.mx", "Administrador"));
        return cliente;
    }
}
