using Tesis.Domain.Enums;

namespace Tesis.Api.Modelos.Consultas;

public sealed record ConfirmacionDto(
    int IdConfirmacion,
    TipoConfirmacion Tipo,
    EstadoConfirmacion Estado,
    DateTime Fecha);

public sealed record ArticuloDto(
    int IdArticulo,
    string Nombre,
    string Url,
    string? Doi,
    int? IdTipoArticulo,
    string? TipoArticulo,
    DateTime FechaReg);

public sealed record DocumentoDto(int IdDocumento, int Version, string Nombre, DateTime FechaReg);

public sealed record RevisionIADto(
    int IdRevision,
    int Version,
    ResultadoRevisionIA Resultado,
    decimal Porcentaje,
    DateTime Fecha,
    string? ReporteJson = null);

public sealed record RevisionAsesorDto(
    int IdRevisionAsesor,
    int Version,
    bool EsUltimaVersion,
    DecisionRevisionAsesor Decision,
    string? Observaciones,
    DateTime Fecha);

public sealed record DictamenDto(
    int IdDictamen,
    ResultadoDictamen Resultado,
    string? Observaciones,
    DateTime Fecha);

public sealed record TesisListadoDto(
    Guid Folio,
    string Titulo,
    NivelPosgrado Nivel,
    string Programa,
    int Version,
    TesisEstado Estado,
    DateTime FechaReg,
    string Alumno,
    string Asesor,
    string? CoAsesor);

public sealed record TesisDetalleDto(
    Guid Folio,
    string Titulo,
    NivelPosgrado Nivel,
    string Programa,
    int Version,
    TesisEstado Estado,
    DateTime FechaReg,
    DateTime? FechaExamenGrado,
    bool TieneCoAsesor,
    int IdAlumno,
    string Alumno,
    int IdAsesor,
    string Asesor,
    int? IdCoAsesor,
    string? CoAsesor,
    IReadOnlyList<ConfirmacionDto> Confirmaciones,
    IReadOnlyList<DocumentoDto> Documentos,
    IReadOnlyList<ArticuloDto> Articulos,
    IReadOnlyList<RevisionIADto> RevisionesIA,
    IReadOnlyList<RevisionAsesorDto> RevisionesAsesor,
    IReadOnlyList<DictamenDto> Dictamenes);

public sealed record ProfesorProgramaDto(NivelPosgrado Nivel, string Programa);

public sealed record ProfesorDto(
    int IdProfesor,
    string Nombre,
    bool Activo,
    IReadOnlyList<ProfesorProgramaDto> Programas,
    string? Correo = null,
    bool CuentaActiva = false);

public sealed record TipoArticuloDto(int IdTipoArticulo, string Nombre, string? Descripcion);

public sealed record AlumnoDto(int IdAlumno, string Nombre, string NumeroControl, string Correo);

public sealed record SolicitudAlumnoRequest(string Nombre, string NumeroControl, string? Correo);

public sealed record SolicitudProfesorProgramaRequest(NivelPosgrado Nivel, string Programa);

public sealed record ActualizarPosgradosProfesorRequest(
    IReadOnlyList<SolicitudProfesorProgramaRequest> Posgrados);

public sealed record PosgradoDto(int IdPosgrado, NivelPosgrado Nivel, string Nombre, bool Activo);

public sealed record SolicitudPosgradoRequest(NivelPosgrado Nivel, string Nombre);

public sealed record ActualizarPosgradoRequest(NivelPosgrado Nivel, string Nombre, bool? Activo = null);

public sealed record ResultadoPosgradoAccionDto(string Mensaje, PosgradoDto? Posgrado = null);


public sealed record SolicitudProfesorRequest(
    string Nombre,
    IReadOnlyList<SolicitudProfesorProgramaRequest> Posgrados,
    string? Correo = null);

public sealed record SolicitudTipoArticuloRequest(string Nombre, string? Descripcion);

public sealed record RepositorioEntradaDto(
    Guid Folio,
    string Titulo,
    NivelPosgrado Nivel,
    string Programa,
    string Autor,
    string? Asesor,
    DateTime? FechaAceptacion);

public sealed record RepositorioResultadoDto(
    IReadOnlyList<RepositorioEntradaDto> Data,
    int Total,
    int Pagina,
    int Tamano);

public sealed record IndicadorProgramaDto(
    string Programa,
    NivelPosgrado Nivel,
    int Total,
    int Aceptadas,
    int EnTramite,
    int Rechazadas);

public sealed record IndicadorNivelDto(
    NivelPosgrado Nivel,
    int Total,
    int Aceptadas,
    int EnTramite = 0,
    int Rechazadas = 0,
    IReadOnlyList<IndicadorProgramaDto>? Programas = null);

public sealed record IndicadorTipoArticuloDto(string Tipo, int Cantidad);

public sealed record IndicadoresDto(
    int Total,
    int Aceptadas,
    int Rechazadas,
    int EnTramite,
    IReadOnlyList<IndicadorNivelDto> PorNivel,
    IReadOnlyList<IndicadorTipoArticuloDto> PorTipoArticulo,
    IReadOnlyList<IndicadorProgramaDto>? PorPrograma = null);
