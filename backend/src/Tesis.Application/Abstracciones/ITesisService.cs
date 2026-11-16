using Tesis.Application.Modelos;

namespace Tesis.Application.Abstracciones;

public interface ITesisService
{
    Task<RegistrarTesisResultado> RegistrarTesisAsync(
        RegistrarTesisComando comando,
        int idAlumno,
        CancellationToken ct = default);

    Task ProcesarConfirmacionAsync(
        ProcesarConfirmacionComando comando,
        int idProfesor,
        CancellationToken ct = default);

    Task SubirDocumentoAsync(
        SubirDocumentoComando comando,
        int idAlumno,
        CancellationToken ct = default);

    Task AgregarArticuloAsync(
        AgregarArticuloComando comando,
        int idAlumno,
        CancellationToken ct = default);

    Task<IReadOnlyList<ReferenciaDetectadaDto>> ObtenerSugerenciasArticulosPdfAsync(
        Guid folio,
        CancellationToken ct = default);

    Task<IReadOnlyList<Tesis.Domain.Entities.Articulo>> ImportarReferenciasArticulosAsync(
        Guid folio,
        IEnumerable<ImportarReferenciasItemRequest> referencias,
        int? idUsuario,
        CancellationToken ct = default);

    Task EjecutarRevisionIAAutomaticaAsync(
        Guid folio,
        CancellationToken ct = default);

    Task ProcesarRevisionIAAsync(
        RegistrarRevisionIAComando comando,
        int idProfesorAsesor,
        CancellationToken ct = default);

    Task ProcesarRevisionAsesorAsync(
        ProcesarRevisionAsesorComando comando,
        int idProfesorAsesor,
        CancellationToken ct = default);

    Task EmitirDictamenAsync(
        EmitirDictamenComando comando,
        int idDirector,
        CancellationToken ct = default);
}