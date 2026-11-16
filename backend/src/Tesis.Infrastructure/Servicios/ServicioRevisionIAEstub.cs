using Microsoft.Extensions.Configuration;
using Tesis.Application.Abstracciones;
using Tesis.Application.Modelos;

namespace Tesis.Infrastructure.Servicios;

public sealed class ServicioRevisionIAEstub : IServicioRevisionIA
{
    private readonly IConfiguration _configuracion;
    private readonly IAlmacenDocumentos? _almacen;
    private readonly ITesisRepository? _tesisRepository;

    public ServicioRevisionIAEstub(
        IConfiguration configuracion,
        IAlmacenDocumentos? almacen = null,
        ITesisRepository? tesisRepository = null)
    {
        _configuracion = configuracion;
        _almacen = almacen;
        _tesisRepository = tesisRepository;
    }

    public async Task<RevisionIAResultado> RevisarAsync(Guid folio, int version, CancellationToken ct = default)
    {
        string? rutaFisica = null;
        string titulo = "Tesis UPT";

        if (_tesisRepository is not null && _almacen is not null)
        {
            var tesis = await _tesisRepository.ObtenerPorFolioAsync(folio, ct);
            if (tesis is not null)
            {
                titulo = tesis.Titulo;
                var doc = tesis.Documentos.OrderByDescending(d => d.Version).FirstOrDefault();
                if (doc is not null)
                {
                    var nombreFisico = _almacen.GenerarNombreFisico(folio, doc.Version, doc.Nombre);
                    if (_almacen.Existe(nombreFisico))
                    {
                        rutaFisica = _almacen.ObtenerRutaFisica(nombreFisico);
                    }
                }
            }
        }

        return GeneradorReporteIASimulado.Generar(folio, titulo, version, rutaFisica);
    }
}
