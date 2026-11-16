using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Api.Modelos.Consultas;

public static class MapearConsulta
{
    public static TesisListadoDto Listado(EntidadTesis t)
    {
        return new TesisListadoDto(
            t.Folio,
            t.Titulo,
            t.Nivel,
            t.Programa,
            t.Version,
            t.Estado,
            t.FechaReg,
            t.Alumno?.Nombre ?? string.Empty,
            t.Asesor?.Nombre ?? string.Empty,
            t.CoAsesor?.Nombre);
    }

    public static TesisDetalleDto Detalle(EntidadTesis t)
    {
        return new TesisDetalleDto(
            t.Folio,
            t.Titulo,
            t.Nivel,
            t.Programa,
            t.Version,
            t.Estado,
            t.FechaReg,
            t.FechaExamenGrado,
            t.TieneCoAsesor,
            t.IdAlumno,
            t.Alumno?.Nombre ?? string.Empty,
            t.IdAsesor,
            t.Asesor?.Nombre ?? string.Empty,
            t.IdCoAsesor,
            t.CoAsesor?.Nombre,
            t.Confirmaciones
                .OrderBy(c => c.FechaReg)
                .Select(c => new ConfirmacionDto(
                    c.IdConfirmacion,
                    c.Tipo,
                    c.Estado,
                    c.Fecha))
                .ToList(),
            t.Documentos
                .OrderByDescending(d => d.FechaReg)
                .Select(d => new DocumentoDto(d.IdDocumento, d.Version, d.Nombre, d.FechaReg))
                .ToList(),
            t.Articulos
                .OrderByDescending(a => a.FechaReg)
                .Select(a => new ArticuloDto(
                    a.IdArticulo,
                    a.Nombre,
                    a.Url,
                    a.Doi,
                    a.IdTipoArticulo,
                    a.TipoArticulo?.Nombre,
                    a.FechaReg))
                .ToList(),
            t.RevisionesIA
                .OrderBy(r => r.Fecha)
                .Select(r => new RevisionIADto(r.IdRevision, r.Version, r.Resultado, r.Porcentaje, r.Fecha, r.ReporteJson))
                .ToList(),
            t.RevisionesAsesor
                .OrderBy(r => r.Fecha)
                .Select(r => new RevisionAsesorDto(
                    r.IdRevisionAsesor,
                    r.Version,
                    r.EsUltimaVersion,
                    r.Decision,
                    r.Observaciones,
                    r.Fecha))
                .ToList(),
            t.Dictamenes
                .OrderBy(d => d.Fecha)
                .Select(d => new DictamenDto(d.IdDictamen, d.Resultado, d.Observaciones, d.Fecha))
                .ToList());
    }

    public static AlumnoDto Alumno(Alumno a) =>
        new(a.IdAlumno, a.Nombre, a.NumeroControl, a.Correo);

    public static ArticuloDto Articulo(Articulo a) =>
        new(
            a.IdArticulo,
            a.Nombre,
            a.Url,
            a.Doi,
            a.IdTipoArticulo,
            a.TipoArticulo?.Nombre,
            a.FechaReg);

    public static PosgradoDto Posgrado(Posgrado p) =>
        new(p.IdPosgrado, p.Nivel, p.Nombre, p.Activo);


    public static ProfesorDto Profesor(Profesor p) =>
        new(
            p.IdProfesor,
            p.Nombre,
            p.Activo,
            p.ProgramasPosgrado
                .Select(pp => new ProfesorProgramaDto(pp.Nivel, pp.Programa))
                .ToList(),
            p.Usuario?.Email,
            p.Usuario?.EmailConfirmed ?? false);

    public static RepositorioEntradaDto Repositorio(EntidadTesis t) =>
        new(
            t.Folio,
            t.Titulo,
            t.Nivel,
            t.Programa,
            t.Alumno?.Nombre ?? string.Empty,
            t.Asesor?.Nombre,
            t.Dictamenes.OrderByDescending(d => d.Fecha).FirstOrDefault()?.Fecha);
}