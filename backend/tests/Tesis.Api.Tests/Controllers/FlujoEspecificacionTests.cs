using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tesis.Api.Core;
using Tesis.Api.Tests.Helpers;
using Tesis.Domain.Enums;
using Xunit;

namespace Tesis.Api.Tests.Controllers;

public class FlujoEspecificacionTests : IClassFixture<FabricacionApi>
{
    private readonly FabricacionApi _fabricacion;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public FlujoEspecificacionTests(FabricacionApi fabricacion)
    {
        _fabricacion = fabricacion;
    }

    [Fact]
    public async Task PR_01_AlumnoConCorreoNoInstitucional_Rechazo()
    {
        var cliente = _fabricacion.CreateClient();
        var contenido = new StringContent(
            JsonSerializer.Serialize(new
            {
                correo = "alumno.prueba@gmail.com",
                contrasena = "Alumno1234!",
                rol = ConstantesRol.Alumno,
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/usuarios/registro", contenido);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("CORREO_NO_INSTITUCIONAL", cuerpo.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PR_02_InvitadoConCorreoNoInstitucional_Permitido()
    {
        var cliente = _fabricacion.CreateClient();
        var correoAleatorio = $"invitado.{Guid.NewGuid():N}@gmail.com";
        var contenido = new StringContent(
            JsonSerializer.Serialize(new
            {
                correo = correoAleatorio,
                contrasena = "Invitado1234!",
                rol = ConstantesRol.Invitado,
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/usuarios/registro", contenido);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.True(cuerpo.RootElement.TryGetProperty("token", out var token) && !string.IsNullOrWhiteSpace(token.GetString()));
    }

    [Fact]
    public async Task PR_03_AltaDeAlumno_Creado()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, 9999, "nuevo.alumno@upt.edu.mx", ConstantesRol.Alumno));

        var contenido = new StringContent(
            JsonSerializer.Serialize(new
            {
                nombre = "Carlos Santana",
                numeroControl = "2026CS01",
                correo = "nuevo.alumno@upt.edu.mx",
            }, _json),
            Encoding.UTF8,
            "application/json");

        var respuesta = await cliente.PostAsync("/api/alumnos", contenido);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task PR_04_RegistroDeTesis_FolioUnicoYRegistrada()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", ConstantesRol.Alumno));

        var folio = await RegistrarTesisAsync(cliente, tieneCoAsesor: false);
        Assert.True(Guid.TryParse(folio, out _));

        var detalle = await cliente.GetAsync($"/api/tesis/{folio}");
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);

        using var cuerpo = JsonDocument.Parse(await detalle.Content.ReadAsStringAsync());
        Assert.Equal("Registrada", cuerpo.RootElement.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task PR_05_CargaSinConfirmaciones_Bloqueada()
    {
        var cliente = _fabricacion.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", ConstantesRol.Alumno));

        var folio = await RegistrarTesisAsync(cliente, tieneCoAsesor: false);

        var respuesta = await cliente.PostAsync($"/api/tesis/{folio}/documento", CrearArchivoAdjunto());
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task PR_06_AsesorYCoAsesorConfirman_CargaHabilitada()
    {
        var cliente = _fabricacion.CreateClient();
        var tokenAlumno = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", ConstantesRol.Alumno);
        var tokenAsesor = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDocente, "juan.perez@upt.edu.mx", ConstantesRol.Docente);
        var tokenCoAsesor = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDocente2, "ana.gomez@upt.edu.mx", ConstantesRol.Docente);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        var folio = await RegistrarTesisAsync(cliente, tieneCoAsesor: true);

        // Asesor confirma
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        var respAsesor = await cliente.PostAsync(
            $"/api/tesis/{folio}/confirmaciones",
            CrearJsonContent(new { tipo = TipoConfirmacion.Asesor.ToString(), decision = EstadoConfirmacion.Confirmada.ToString() }));
        Assert.Equal(HttpStatusCode.OK, respAsesor.StatusCode);

        // Co-asesor confirma
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCoAsesor);
        var respCoAsesor = await cliente.PostAsync(
            $"/api/tesis/{folio}/confirmaciones",
            CrearJsonContent(new { tipo = TipoConfirmacion.CoAsesor.ToString(), decision = EstadoConfirmacion.Confirmada.ToString() }));
        Assert.Equal(HttpStatusCode.OK, respCoAsesor.StatusCode);

        // Carga de documento ahora permitida para alumno
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        var respDoc = await cliente.PostAsync($"/api/tesis/{folio}/documento", CrearArchivoAdjunto());
        Assert.Equal(HttpStatusCode.OK, respDoc.StatusCode);
    }

    [Fact]
    public async Task PR_07_AsesorConfirmaVersionYEjecutaIA_SoloElPuede()
    {
        var cliente = _fabricacion.CreateClient();
        var tokenAlumno = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", ConstantesRol.Alumno);
        var tokenAsesor = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDocente, "juan.perez@upt.edu.mx", ConstantesRol.Docente);
        var tokenDirector = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDirector, "director@upt.edu.mx", ConstantesRol.DirectorDePosgrado);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        var folio = await RegistrarTesisAsync(cliente, tieneCoAsesor: false);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/confirmaciones",
            CrearJsonContent(new { tipo = TipoConfirmacion.Asesor.ToString(), decision = EstadoConfirmacion.Confirmada.ToString() }));

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        await cliente.PostAsync($"/api/tesis/{folio}/documento", CrearArchivoAdjunto());

        // Director no puede ejecutar IA (solo el asesor de la tesis)
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDirector);
        var respDirector = await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-ia",
            CrearJsonContent(new { esUltimaVersion = true }));
        Assert.Equal(HttpStatusCode.Forbidden, respDirector.StatusCode);

        // Asesor ejecuta confirmando última versión -> éxito
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        var respAsesor = await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-ia",
            CrearJsonContent(new { esUltimaVersion = true }));
        Assert.Equal(HttpStatusCode.OK, respAsesor.StatusCode);
    }

    [Fact]
    public async Task PR_08_AsesorSolicitaCambios_CambiosSolicitados()
    {
        var (cliente, folio, tokenAsesor, _) = await PrepararTesisEnRevisionAsesorAsync();

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        var respuesta = await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-asesor",
            CrearJsonContent(new
            {
                esUltimaVersion = true,
                decision = "Rechazada",
                observaciones = "Favor de corregir marco teórico y conclusiones."
            }));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var detalle = await cliente.GetAsync($"/api/tesis/{folio}");
        using var cuerpo = JsonDocument.Parse(await detalle.Content.ReadAsStringAsync());
        Assert.Equal("Cambios solicitados", cuerpo.RootElement.GetProperty("estado").GetString());

        // Intentar rechazar nuevamente debe fallar según la regla de rechazo único
        var segundoRechazo = await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-asesor",
            CrearJsonContent(new
            {
                esUltimaVersion = true,
                decision = "Rechazada",
                observaciones = "Segundo intento de rechazo."
            }));
        Assert.Equal(HttpStatusCode.Conflict, segundoRechazo.StatusCode);
    }

    [Fact]
    public async Task PR_09_AlumnoReenviaYAsesorAcepta_EnDictamen()
    {
        var (cliente, folio, tokenAsesor, tokenAlumno) = await PrepararTesisEnRevisionAsesorAsync();

        // Asesor solicita cambios
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-asesor",
            CrearJsonContent(new { esUltimaVersion = true, decision = "Rechazada", observaciones = "Corregir formato." }));

        // Alumno sube versión corregida (reenvío automático a EnRevisionIA)
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        var respSubir = await cliente.PostAsync($"/api/tesis/{folio}/documento", CrearArchivoAdjunto());
        Assert.Equal(HttpStatusCode.OK, respSubir.StatusCode);

        // Asesor ejecuta IA en la nueva versión
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-ia",
            CrearJsonContent(new { esUltimaVersion = true }));

        // Asesor acepta
        var respAceptar = await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-asesor",
            CrearJsonContent(new { esUltimaVersion = true, decision = "Aceptada" }));
        Assert.Equal(HttpStatusCode.OK, respAceptar.StatusCode);

        var detalle = await cliente.GetAsync($"/api/tesis/{folio}");
        using var cuerpo = JsonDocument.Parse(await detalle.Content.ReadAsStringAsync());
        Assert.Equal("En dictamen", cuerpo.RootElement.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task PR_10_DirectorAcepta_AceptadaYPublicada()
    {
        var (cliente, folio, tokenAsesor, _) = await PrepararTesisEnRevisionAsesorAsync();

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-asesor",
            CrearJsonContent(new { esUltimaVersion = true, decision = "Aceptada" }));

        // Director emite dictamen Aceptada
        var tokenDirector = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDirector, "director@upt.edu.mx", ConstantesRol.DirectorDePosgrado);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDirector);
        var respDictamen = await cliente.PostAsync(
            $"/api/tesis/{folio}/dictamen",
            CrearJsonContent(new { resultado = "Aceptada", observaciones = "Aprobada por unanimidad." }));
        Assert.Equal(HttpStatusCode.OK, respDictamen.StatusCode);

        var detalle = await cliente.GetAsync($"/api/tesis/{folio}");
        using var cuerpo = JsonDocument.Parse(await detalle.Content.ReadAsStringAsync());
        Assert.Equal("Aceptada", cuerpo.RootElement.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task PR_11_BusquedaPorAutor_SoloTesisAceptadas()
    {
        var (cliente, folio, tokenAsesor, _) = await PrepararTesisEnRevisionAsesorAsync();

        // Antes de ser aceptada, NO debe aparecer en el repositorio público
        var respAntes = await cliente.GetAsync("/api/repositorio?autor=María López");
        Assert.Equal(HttpStatusCode.OK, respAntes.StatusCode);
        using var cuerpoAntes = JsonDocument.Parse(await respAntes.Content.ReadAsStringAsync());
        Assert.Equal(0, cuerpoAntes.RootElement.GetProperty("total").GetInt32());

        // Asesor acepta
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-asesor",
            CrearJsonContent(new { esUltimaVersion = true, decision = "Aceptada" }));

        // Director dictamina Aceptada
        var tokenDirector = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDirector, "director@upt.edu.mx", ConstantesRol.DirectorDePosgrado);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDirector);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/dictamen",
            CrearJsonContent(new { resultado = "Aceptada", observaciones = "Aprobada." }));

        // Ahora DEBE aparecer públicamente
        var respDespues = await cliente.GetAsync("/api/repositorio?autor=María López");
        Assert.Equal(HttpStatusCode.OK, respDespues.StatusCode);
        using var cuerpoDespues = JsonDocument.Parse(await respDespues.Content.ReadAsStringAsync());
        Assert.True(cuerpoDespues.RootElement.GetProperty("total").GetInt32() >= 1);
    }

    private async Task<(HttpClient cliente, string folio, string tokenAsesor, string tokenAlumno)> PrepararTesisEnRevisionAsesorAsync()
    {
        var cliente = _fabricacion.CreateClient();
        var tokenAlumno = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioAlumno, "maria.lopez@upt.edu.mx", ConstantesRol.Alumno);
        var tokenAsesor = TokenHelper.Generar(_fabricacion, FabricacionApi.IdUsuarioDocente, "juan.perez@upt.edu.mx", ConstantesRol.Docente);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        var folio = await RegistrarTesisAsync(cliente, tieneCoAsesor: false);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/confirmaciones",
            CrearJsonContent(new { tipo = TipoConfirmacion.Asesor.ToString(), decision = EstadoConfirmacion.Confirmada.ToString() }));

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlumno);
        await cliente.PostAsync($"/api/tesis/{folio}/documento", CrearArchivoAdjunto());

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAsesor);
        await cliente.PostAsync(
            $"/api/tesis/{folio}/revision-ia",
            CrearJsonContent(new { esUltimaVersion = true }));

        return (cliente, folio, tokenAsesor, tokenAlumno);
    }

    private async Task<string> RegistrarTesisAsync(HttpClient cliente, bool tieneCoAsesor)
    {
        _fabricacion.ReiniciarTesis();
        var idCoAsesor = tieneCoAsesor ? (int?)FabricacionApi.IdProfesorCoAsesor : null;
        var contenido = CrearJsonContent(new
        {
            titulo = "Sistemas embebidos para monitoreo ambiental",
            nivel = "Maestría",
            programa = "Maestría en Tecnologías de la Información",
            idAsesor = FabricacionApi.IdProfesorAsesor,
            idCoAsesor,
        });

        var respuesta = await cliente.PostAsync("/api/tesis", contenido);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return cuerpo.RootElement.GetProperty("folio").GetString()!;
    }

    private StringContent CrearJsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

    private static MultipartFormDataContent CrearArchivoAdjunto()
    {
        var archivo = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 de prueba"));
        archivo.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        return new MultipartFormDataContent
        {
            { archivo, "archivo", "tesis.pdf" }
        };
    }
}
