using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class Tesis : EntidadAuditable
{
    public Guid Folio { get; private set; }
    public int IdAlumno { get; set; }
    public string Titulo { get; set; }
    public NivelPosgrado Nivel { get; set; }
    public string Programa { get; set; }
    public int IdAsesor { get; set; }
    public bool TieneCoAsesor { get; set; }
    public int? IdCoAsesor { get; set; }
    public int Version { get; private set; }
    public TesisEstado Estado { get; private set; }
    public DateTime? FechaExamenGrado { get; set; }

    public Alumno? Alumno { get; set; }
    public Profesor? Asesor { get; set; }
    public Profesor? CoAsesor { get; set; }
    public List<ConfirmacionAsesoria> Confirmaciones { get; } = new();
    public List<DocumentoTesis> Documentos { get; } = new();
    public List<TesisVersion> Versiones { get; } = new();
    public List<HistorialEstadoTesis> HistorialEstados { get; } = new();
    public List<Articulo> Articulos { get; } = new();
    public List<RevisionIA> RevisionesIA { get; } = new();
    public List<RevisionAsesor> RevisionesAsesor { get; } = new();
    public List<Dictamen> Dictamenes { get; } = new();
    public List<Favorito> Favoritos { get; } = new();

    private static readonly Dictionary<TesisEstado, TesisEstado[]> TransicionesPermitidas = new()
    {
        [TesisEstado.Registrada] = new[] { TesisEstado.Confirmada },
        [TesisEstado.Confirmada] = new[] { TesisEstado.EnRevisionIA },
        [TesisEstado.EnRevisionIA] = new[] { TesisEstado.EnRevisionDelAsesor },
        [TesisEstado.EnRevisionDelAsesor] = new[] { TesisEstado.EnRevisionIA, TesisEstado.EnDictamen, TesisEstado.CambiosSolicitados },
        [TesisEstado.CambiosSolicitados] = new[] { TesisEstado.EnRevisionIA },
        [TesisEstado.EnDictamen] = new[] { TesisEstado.Aceptada, TesisEstado.Rechazada },
        [TesisEstado.Aceptada] = Array.Empty<TesisEstado>(),
        [TesisEstado.Rechazada] = Array.Empty<TesisEstado>(),
        [TesisEstado.Inactiva] = Array.Empty<TesisEstado>(),
    };

    public Tesis(
        int idAlumno,
        string titulo,
        NivelPosgrado nivel,
        string programa,
        int idAsesor,
        bool tieneCoAsesor = false,
        int? idCoAsesor = null)
    {
        Guardia.Exigir(!string.IsNullOrWhiteSpace(titulo), "El título de la tesis es obligatorio.");
        Guardia.Exigir(titulo.Length <= 300, "El título de la tesis no puede exceder 300 caracteres.");
        Guardia.Exigir(!string.IsNullOrWhiteSpace(programa), "El programa de posgrado es obligatorio.");
        Guardia.Exigir(!tieneCoAsesor || idCoAsesor.HasValue, "Si la tesis declara co-asesor debe especificarlo.");
        Guardia.Exigir(!tieneCoAsesor || idCoAsesor != idAsesor, "El co-asesor debe ser distinto del asesor.");

        Folio = Guid.NewGuid();
        IdAlumno = idAlumno;
        Titulo = titulo.Trim();
        Nivel = nivel;
        Programa = programa;
        IdAsesor = idAsesor;
        TieneCoAsesor = tieneCoAsesor;
        IdCoAsesor = idCoAsesor;
        Version = 1;
        Estado = TesisEstado.Registrada;
    }

    public bool EsTerminal =>
        Estado is TesisEstado.Aceptada or TesisEstado.Rechazada or TesisEstado.Inactiva;

    public bool PuedeTransicionarA(TesisEstado nuevoEstado) =>
        !EsTerminal &&
        (nuevoEstado == TesisEstado.Inactiva || TransicionesPermitidas[Estado].Contains(nuevoEstado));

    public void AgregarConfirmacion(ConfirmacionAsesoria confirmacion)
    {
        Guardia.Exigir(
            Estado == TesisEstado.Registrada,
            "Las confirmaciones solo pueden declararse mientras la tesis está registrada.");
        Guardia.Exigir(
            !Confirmaciones.Any(c => c.Tipo == confirmacion.Tipo && c.IdProfesor == confirmacion.IdProfesor),
            "Ya existe una confirmación para este profesor y tipo.");

        switch (confirmacion.Tipo)
        {
            case TipoConfirmacion.Asesor:
                Guardia.Exigir(
                    confirmacion.IdProfesor == IdAsesor,
                    "La confirmación del asesor debe corresponder al asesor asignado.");
                break;
            case TipoConfirmacion.CoAsesor:
                Guardia.Exigir(
                    TieneCoAsesor && IdCoAsesor == confirmacion.IdProfesor,
                    "La tesis no declara este co-asesor.");
                break;
            default:
                throw new InvalidOperationException("Tipo de confirmación no reconocido.");
        }

        Confirmaciones.Add(confirmacion);
    }

    public void ConfirmarAsesoria()
    {
        Guardia.Exigir(
            Estado == TesisEstado.Registrada,
            "Solo una tesis registrada puede confirmar la asesoría.");

        var confirmacionesRequeridas = Confirmaciones
            .Where(c => c.Tipo == TipoConfirmacion.Asesor ||
                        (TieneCoAsesor && c.Tipo == TipoConfirmacion.CoAsesor))
            .ToList();
        var totalRequeridas = TieneCoAsesor ? 2 : 1;

        Guardia.Exigir(
            confirmacionesRequeridas.Count == totalRequeridas &&
            confirmacionesRequeridas.All(c => c.Estado == EstadoConfirmacion.Confirmada),
            "Todas las confirmaciones de asesoría requeridas deben estar confirmadas.");

        CambiarEstado(TesisEstado.Confirmada);
    }

    public void SubirDocumento(DocumentoTesis documento)
    {
        Guardia.Exigir(documento is not null, "El documento no puede ser nulo.");
        Guardia.Exigir(
            Estado == TesisEstado.Confirmada ||
            Estado == TesisEstado.EnRevisionIA ||
            Estado == TesisEstado.CambiosSolicitados,
            "Los documentos solo pueden subirse después de confirmar la asesoría o al atender cambios solicitados.");

        documento!.Folio = Folio;
        documento.Version = Version;
        Documentos.Add(documento);

        Versiones.Add(new TesisVersion
        {
            Folio = Folio,
            Version = Version,
            RutaDocumento = documento.Nombre,
            Fecha = DateTime.UtcNow
        });

        if (Estado == TesisEstado.Confirmada)
        {
            CambiarEstado(TesisEstado.EnRevisionIA);
        }
    }

    public void RegistrarRevisionIA(ResultadoRevisionIA resultado, decimal porcentaje, string? reporteJson = null)
    {
        Guardia.Exigir(
            Estado == TesisEstado.EnRevisionIA || Estado == TesisEstado.EnRevisionDelAsesor,
            "La revisión por IA solo puede ejecutarse mientras la tesis está en revisión IA o revisión del asesor.");
        Guardia.Exigir(porcentaje >= 0 && porcentaje <= 100, "El porcentaje de validez debe estar entre 0 y 100.");

        var revisionExistente = RevisionesIA.FirstOrDefault(r => r.Version == Version);
        if (revisionExistente is not null)
        {
            revisionExistente.Resultado = resultado;
            revisionExistente.Porcentaje = porcentaje;
            revisionExistente.ReporteJson = reporteJson;
            revisionExistente.Fecha = DateTime.UtcNow;
        }
        else
        {
            RevisionesIA.Add(new RevisionIA
            {
                Folio = Folio,
                Version = Version,
                Resultado = resultado,
                Porcentaje = porcentaje,
                ReporteJson = reporteJson,
                Fecha = DateTime.UtcNow,
            });
        }

        if (Estado == TesisEstado.EnRevisionIA && (resultado == ResultadoRevisionIA.Valido || resultado == ResultadoRevisionIA.ConObservaciones))
        {
            CambiarEstado(TesisEstado.EnRevisionDelAsesor);
        }
    }

    public void DevolverVersionIncorrecta()
    {
        Guardia.Exigir(
            Estado == TesisEstado.EnRevisionDelAsesor,
            "Solo puede devolverse una tesis que está en revisión del asesor.");
        CambiarEstado(TesisEstado.EnRevisionIA);
    }

    public void AceptarRevision(int idProfesor)
    {
        Guardia.Exigir(
            Estado == TesisEstado.EnRevisionDelAsesor,
            "La tesis solo puede aceptarse cuando está en revisión del asesor.");
        Guardia.Exigir(idProfesor == IdAsesor, "Solo el asesor asignado puede aceptar la revisión.");

        RevisionesAsesor.Add(new RevisionAsesor
        {
            Folio = Folio,
            IdProfesor = IdAsesor,
            Version = Version,
            EsUltimaVersion = true,
            Decision = DecisionRevisionAsesor.Aceptada,
            Fecha = DateTime.UtcNow,
        });

        CambiarEstado(TesisEstado.EnDictamen);
    }

    public void RechazarTesis(int idProfesor, string? observaciones)
    {
        Guardia.Exigir(
            Estado == TesisEstado.EnRevisionDelAsesor,
            "La tesis solo puede rechazarse cuando está en revisión del asesor.");
        Guardia.Exigir(idProfesor == IdAsesor, "Solo el asesor asignado puede rechazar la revisión.");
        Guardia.Exigir(
            !RevisionesAsesor.Any(r => r.Decision == DecisionRevisionAsesor.Rechazada),
            "El asesor solo puede solicitar cambios una sola vez.");

        RevisionesAsesor.Add(new RevisionAsesor
        {
            Folio = Folio,
            IdProfesor = IdAsesor,
            Version = Version,
            EsUltimaVersion = true,
            Decision = DecisionRevisionAsesor.Rechazada,
            Observaciones = observaciones,
            Fecha = DateTime.UtcNow,
        });

        CambiarEstado(TesisEstado.CambiosSolicitados);
    }

    public void ReenviarTesis()
    {
        Guardia.Exigir(
            Estado == TesisEstado.CambiosSolicitados,
            "La tesis solo puede reenviarse cuando se solicitaron cambios.");
        Version++;
        CambiarEstado(TesisEstado.EnRevisionIA);
    }

    public void EmitirDictamen(int idDirector, ResultadoDictamen resultado, string? observaciones)
    {
        Guardia.Exigir(
            Estado == TesisEstado.EnDictamen,
            "El dictamen solo puede emitirse cuando la tesis está en dictamen.");

        Dictamenes.Add(new Dictamen
        {
            Folio = Folio,
            IdDirector = idDirector,
            Resultado = resultado,
            Observaciones = observaciones,
            Fecha = DateTime.UtcNow,
        });

        CambiarEstado(resultado == ResultadoDictamen.Aceptada
            ? TesisEstado.Aceptada
            : TesisEstado.Rechazada);
    }

    public void Inactivar()
    {
        Guardia.Exigir(!EsTerminal, "Una tesis en estado terminal no puede inactivarse.");
        CambiarEstado(TesisEstado.Inactiva);
    }

    private void CambiarEstado(TesisEstado nuevoEstado, int? idUsuario = null, string? observacion = null)
    {
        if (!PuedeTransicionarA(nuevoEstado))
        {
            throw new InvalidOperationException(
                $"Transición no permitida: de '{Estado}' a '{nuevoEstado}'.");
        }

        var anterior = Estado;
        Estado = nuevoEstado;

        HistorialEstados.Add(new HistorialEstadoTesis
        {
            Folio = Folio,
            EstadoAnterior = anterior,
            EstadoNuevo = nuevoEstado,
            IdUsuario = idUsuario,
            Fecha = DateTime.UtcNow,
            Observacion = observacion
        });
    }
}