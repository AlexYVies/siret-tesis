using System.Text.Json;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Application.Servicios;

public sealed class TesisService : ITesisService
{
    private readonly ITesisRepository _tesisRepository;
    private readonly IProfesorRepository _profesorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificadorAsesoria _notificador;
    private readonly IServicioRevisionIA _servicioRevisionIA;
    private readonly IExtractorReferenciasPdf _extractorReferencias;

    public TesisService(
        ITesisRepository tesisRepository,
        IProfesorRepository profesorRepository,
        IUnitOfWork unitOfWork,
        INotificadorAsesoria notificador,
        IServicioRevisionIA servicioRevisionIA,
        IExtractorReferenciasPdf extractorReferencias)
    {
        _tesisRepository = tesisRepository;
        _profesorRepository = profesorRepository;
        _unitOfWork = unitOfWork;
        _notificador = notificador;
        _servicioRevisionIA = servicioRevisionIA;
        _extractorReferencias = extractorReferencias;
    }

    public async Task<RegistrarTesisResultado> RegistrarTesisAsync(
        RegistrarTesisComando comando,
        int idAlumno,
        CancellationToken ct = default)
    {
        var tesisDelAlumno = await _tesisRepository.ObtenerPorAlumnoAsync(idAlumno, ct);
        var tesisEnProceso = tesisDelAlumno.FirstOrDefault(t => !t.EsTerminal);
        if (tesisEnProceso is not null)
        {
            throw new ConflictoDeNegocioException(
                $"Ya cuentas con un proyecto de tesis en proceso ('{tesisEnProceso.Titulo}'). Solo puedes tener 1 tesis en proceso; únicamente podrás registrar una nueva si la actual es aceptada o cancelada.");
        }

        var asesor = await _profesorRepository.ObtenerConPosgradosAsync(comando.IdAsesor, ct)
            ?? throw new EntidadNoEncontradaException($"No existe el profesor con id {comando.IdAsesor}.");
        AsegurarAsesorValido(asesor, comando.Nivel, comando.Programa);

        Profesor? coAsesor = null;
        if (comando.IdCoAsesor.HasValue)
        {
            coAsesor = await _profesorRepository.ObtenerConPosgradosAsync(comando.IdCoAsesor.Value, ct)
                ?? throw new EntidadNoEncontradaException($"No existe el profesor con id {comando.IdCoAsesor}.");
            AsegurarAsesorValido(coAsesor, comando.Nivel, comando.Programa);
        }

        var tesis = new EntidadTesis(
            idAlumno,
            comando.Titulo,
            comando.Nivel,
            comando.Programa,
            comando.IdAsesor,
            tieneCoAsesor: comando.IdCoAsesor.HasValue,
            idCoAsesor: comando.IdCoAsesor);

        tesis.AgregarConfirmacion(new ConfirmacionAsesoria
        {
            Folio = tesis.Folio,
            IdProfesor = asesor.IdProfesor,
            Tipo = TipoConfirmacion.Asesor,
        });

        if (coAsesor is not null)
        {
            tesis.AgregarConfirmacion(new ConfirmacionAsesoria
            {
                Folio = tesis.Folio,
                IdProfesor = coAsesor.IdProfesor,
                Tipo = TipoConfirmacion.CoAsesor,
            });
        }

        await _tesisRepository.AgregarAsync(tesis, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var profesoresAvisar = coAsesor is null
            ? new int[] { asesor.IdProfesor }
            : new[] { asesor.IdProfesor, coAsesor.IdProfesor };
        await _notificador.NotificarSolicitudConfirmacionAsync(tesis.Folio, profesoresAvisar, ct);

        return new RegistrarTesisResultado(tesis.Folio, tesis.Estado);
    }

    public async Task ProcesarConfirmacionAsync(
        ProcesarConfirmacionComando comando,
        int idProfesor,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(comando.Folio, ct);

        var confirmacion = tesis.Confirmaciones.FirstOrDefault(c =>
                c.Tipo == comando.Tipo && c.IdProfesor == idProfesor)
            ?? throw new OperacionNoPermitidaException(
                "Solo el asesor o co-asesor asignado puede responder la confirmación de asesoría.");

        if (confirmacion.Estado != EstadoConfirmacion.Pendiente)
        {
            throw new ConflictoDeNegocioException("La confirmación de asesoría ya fue respondida.");
        }

        if (comando.Decision == EstadoConfirmacion.Confirmada)
        {
            confirmacion.Confirmar();
        }
        else if (comando.Decision == EstadoConfirmacion.Rechazada)
        {
            confirmacion.Rechazar();
        }
        else
        {
            throw new SolicitudInvalidaException("La decisión debe ser Confirmada o Rechazada.");
        }

        if (confirmacion.Estado == EstadoConfirmacion.Confirmada && TieneTodasLasConfirmaciones(tesis))
        {
            EjecutarConflicto(tesis.ConfirmarAsesoria);
        }

        await PersistirAsync(tesis, ct);
    }

    public async Task SubirDocumentoAsync(
        SubirDocumentoComando comando,
        int idAlumno,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(comando.Folio, ct);
        if (tesis.IdAlumno != idAlumno)
        {
            throw new OperacionNoPermitidaException(
                "Solo el alumno titular puede subir documentos de la tesis.");
        }

        EjecutarConflicto(() => tesis.SubirDocumento(new DocumentoTesis
        {
            Nombre = comando.NombreArchivo,
        }));

        if (tesis.Estado == TesisEstado.CambiosSolicitados)
        {
            EjecutarConflicto(tesis.ReenviarTesis);
        }

        await PersistirAsync(tesis, ct);
    }

    public async Task AgregarArticuloAsync(
        AgregarArticuloComando comando,
        int idAlumno,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(comando.Folio, ct);
        if (tesis.IdAlumno != idAlumno)
        {
            throw new OperacionNoPermitidaException(
                "Solo el alumno titular puede agregar artículos a la tesis.");
        }

        var articulo = EjecutarValidacion(() =>
            new Articulo(comando.Nombre, comando.Url, comando.Doi, comando.IdTipoArticulo));

        tesis.Articulos.Add(articulo);
        await PersistirAsync(tesis, ct);
    }

    public async Task<IReadOnlyList<ReferenciaDetectadaDto>> ObtenerSugerenciasArticulosPdfAsync(
        Guid folio,
        CancellationToken ct = default)
    {
        var tesis = await _tesisRepository.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        // Si la tesis ya cuenta con reporte IA pero sus artículos no habían sido auto-poblados, poblarlos ahora
        var ultimaRevision = tesis.RevisionesIA
            .OrderByDescending(r => r.Version)
            .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.ReporteJson));

        if (ultimaRevision is not null && tesis.Articulos.Count == 0)
        {
            var antes = tesis.Articulos.Count;
            PoblarArticulosVerificadosAutomaticamente(tesis, ultimaRevision.ReporteJson);
            if (tesis.Articulos.Count > antes)
            {
                await PersistirAsync(tesis, ct);
            }
        }

        return await _extractorReferencias.ExtraerAsync(tesis, ct);
    }

    public async Task<IReadOnlyList<Articulo>> ImportarReferenciasArticulosAsync(
        Guid folio,
        IEnumerable<ImportarReferenciasItemRequest> referencias,
        int? idUsuario,
        CancellationToken ct = default)
    {
        var tesis = await _tesisRepository.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        var nuevosArticulos = new List<Articulo>();

        foreach (var r in referencias)
        {
            if (string.IsNullOrWhiteSpace(r.Nombre) || string.IsNullOrWhiteSpace(r.Url))
            {
                continue;
            }

            var existe = tesis.Articulos.Any(a =>
                (!string.IsNullOrWhiteSpace(r.Doi) && !string.IsNullOrWhiteSpace(a.Doi) && string.Equals(a.Doi, r.Doi, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(a.Nombre?.Trim(), r.Nombre.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existe) continue;

            var articulo = EjecutarValidacion(() =>
                new Articulo(r.Nombre, r.Url, r.Doi, r.IdTipoArticulo));

            tesis.Articulos.Add(articulo);
            nuevosArticulos.Add(articulo);
        }

        if (nuevosArticulos.Count > 0)
        {
            await PersistirAsync(tesis, ct);
        }

        return nuevosArticulos;
    }

    public async Task EjecutarRevisionIAAutomaticaAsync(
        Guid folio,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(folio, ct);
        if (tesis.Estado != TesisEstado.EnRevisionIA && tesis.Estado != TesisEstado.EnRevisionDelAsesor)
        {
            return;
        }

        if (tesis.RevisionesIA.Any(r => r.Version == tesis.Version && !string.IsNullOrEmpty(r.ReporteJson)))
        {
            return;
        }

        RevisionIAResultado revision;
        try
        {
            revision = await _servicioRevisionIA.RevisarAsync(folio, tesis.Version, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return;
        }

        EjecutarConflicto(() => tesis.RegistrarRevisionIA(revision.Resultado, revision.Porcentaje, revision.ReporteJson));
        PoblarArticulosVerificadosAutomaticamente(tesis, revision.ReporteJson);
        await PersistirAsync(tesis, ct);
    }

    public async Task ProcesarRevisionIAAsync(
        RegistrarRevisionIAComando comando,
        int idProfesorAsesor,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(comando.Folio, ct);
        AsegurarAsesor(tesis, idProfesorAsesor);

        if (!comando.EsUltimaVersion)
        {
            throw new ConflictoDeNegocioException(
                "Debe confirmar que este documento corresponde a la última versión autorizada de la tesis antes de ejecutar la revisión por IA.");
        }

        if (tesis.RevisionesIA.Any(r => r.Version == tesis.Version && !string.IsNullOrEmpty(r.ReporteJson)) &&
            tesis.Estado == TesisEstado.EnRevisionDelAsesor)
        {
            return;
        }

        RevisionIAResultado revision;
        try
        {
            revision = await _servicioRevisionIA.RevisarAsync(comando.Folio, tesis.Version, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            throw new ServicioNoDisponibleException(
                "El servicio de revisión por IA no está disponible en este momento.");
        }

        EjecutarConflicto(() => tesis.RegistrarRevisionIA(revision.Resultado, revision.Porcentaje, revision.ReporteJson));
        PoblarArticulosVerificadosAutomaticamente(tesis, revision.ReporteJson);
        await PersistirAsync(tesis, ct);
    }

    private static void PoblarArticulosVerificadosAutomaticamente(EntidadTesis tesis, string? reporteJson)
    {
        if (string.IsNullOrWhiteSpace(reporteJson)) return;

        try
        {
            using var doc = JsonDocument.Parse(reporteJson);
            if (!doc.RootElement.TryGetProperty("auditoria_citas", out var citasElem) ||
                citasElem.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var c in citasElem.EnumerateArray())
            {
                var estado = c.TryGetProperty("estado_verificacion", out var ev) ? ev.GetString() : null;

                // Solo agregar citas verificadas legítimas (descartando sospechosas y no encontradas)
                if (!string.Equals(estado, "VERIFICADA", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Descartar si el análisis de contenido indica que contradice la tesis
                if (c.TryGetProperty("validacion_contenido", out var vc) && vc.ValueKind == JsonValueKind.Object)
                {
                    if (vc.TryGetProperty("coincide", out var coinc) && coinc.ValueKind == JsonValueKind.False)
                    {
                        continue;
                    }
                }

                var titulo = c.TryGetProperty("titulo_encontrado", out var te) && !string.IsNullOrWhiteSpace(te.GetString())
                    ? te.GetString()!.Trim()
                    : (c.TryGetProperty("cita_original", out var co) ? co.GetString()?.Trim() : null);

                if (string.IsNullOrWhiteSpace(titulo)) continue;

                var doi = c.TryGetProperty("doi_encontrado", out var de) && !string.IsNullOrWhiteSpace(de.GetString())
                    ? de.GetString()!.Trim()
                    : (c.TryGetProperty("doi", out var d) ? d.GetString()?.Trim() : null);

                var url = c.TryGetProperty("url_fuente", out var uf) && !string.IsNullOrWhiteSpace(uf.GetString())
                    ? uf.GetString()!.Trim()
                    : (!string.IsNullOrWhiteSpace(doi) ? $"https://doi.org/{doi}" : "https://scholar.google.com");

                if (titulo.Length > 500) titulo = titulo[..500];

                var yaExiste = tesis.Articulos.Any(a =>
                    (!string.IsNullOrWhiteSpace(doi) && !string.IsNullOrWhiteSpace(a.Doi) && string.Equals(a.Doi, doi, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(a.Nombre?.Trim(), titulo, StringComparison.OrdinalIgnoreCase));

                if (!yaExiste)
                {
                    try
                    {
                        var nuevoArticulo = new Articulo(titulo, url, doi, idTipoArticulo: 2); // 2 = Artículo de revista
                        tesis.Articulos.Add(nuevoArticulo);
                    }
                    catch
                    {
                        // Si falla validación de formato del dominio, continuar sin interrumpir
                    }
                }
            }
        }
        catch
        {
            // Ignorar errores de deserialización si el JSON está truncado
        }
    }

    public async Task ProcesarRevisionAsesorAsync(
        ProcesarRevisionAsesorComando comando,
        int idProfesorAsesor,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(comando.Folio, ct);
        AsegurarAsesor(tesis, idProfesorAsesor);

        if (!comando.EsUltimaVersion)
        {
            EjecutarConflicto(tesis.DevolverVersionIncorrecta);
        }
        else if (comando.Decision == DecisionRevisionAsesor.Aceptada)
        {
            EjecutarConflicto(() => tesis.AceptarRevision(idProfesorAsesor));
        }
        else if (comando.Decision == DecisionRevisionAsesor.Rechazada)
        {
            EjecutarConflicto(() => tesis.RechazarTesis(idProfesorAsesor, comando.Observaciones));
        }
        else
        {
            throw new SolicitudInvalidaException("La decisión debe ser Aceptada o Rechazada.");
        }

        await PersistirAsync(tesis, ct);
    }

    public async Task EmitirDictamenAsync(
        EmitirDictamenComando comando,
        int idDirector,
        CancellationToken ct = default)
    {
        var tesis = await ObtenerTesisAsync(comando.Folio, ct);
        EjecutarConflicto(() => tesis.EmitirDictamen(idDirector, comando.Resultado, comando.Observaciones));
        await PersistirAsync(tesis, ct);
    }

    private static bool TieneTodasLasConfirmaciones(EntidadTesis tesis)
    {
        var requeridas = tesis.TieneCoAsesor ? 2 : 1;
        var confirmadas = tesis.Confirmaciones.Count(c =>
            c.Estado == EstadoConfirmacion.Confirmada &&
            (c.Tipo == TipoConfirmacion.Asesor ||
             (tesis.TieneCoAsesor && c.Tipo == TipoConfirmacion.CoAsesor)));
        return confirmadas == requeridas;
    }

    private static void AsegurarAsesorValido(Profesor profesor, NivelPosgrado nivel, string programa)
    {
        if (!profesor.Activo)
        {
            throw new SolicitudInvalidaException($"El profesor '{profesor.Nombre}' está inactivo.");
        }

        var registrado = profesor.ProgramasPosgrado.Any(p => p.Nivel == nivel && p.Programa == programa);
        if (!registrado)
        {
            throw new SolicitudInvalidaException(
                $"El profesor '{profesor.Nombre}' no está registrado para el programa '{programa}' de nivel {nivel}.");
        }
    }

    private static void AsegurarAsesor(EntidadTesis tesis, int idProfesor)
    {
        if (tesis.IdAsesor != idProfesor)
        {
            throw new OperacionNoPermitidaException(
                "Solo el asesor asignado puede ejecutar esta acción.");
        }
    }

    private async Task<EntidadTesis> ObtenerTesisAsync(Guid folio, CancellationToken ct) =>
        await _tesisRepository.ObtenerPorFolioAsync(folio, ct)
        ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

    private async Task PersistirAsync(EntidadTesis tesis, CancellationToken ct)
    {
        await _tesisRepository.ActualizarAsync(tesis, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static void EjecutarConflicto(Action accion)
    {
        try
        {
            accion();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictoDeNegocioException(ex.Message);
        }
    }

    private static T EjecutarValidacion<T>(Func<T> fabrica)
    {
        try
        {
            return fabrica();
        }
        catch (InvalidOperationException ex)
        {
            throw new SolicitudInvalidaException(ex.Message);
        }
    }
}