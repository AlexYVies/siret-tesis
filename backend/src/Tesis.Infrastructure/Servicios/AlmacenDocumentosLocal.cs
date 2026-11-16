using Microsoft.Extensions.Configuration;
using Tesis.Application.Abstracciones;

namespace Tesis.Infrastructure.Servicios;

public sealed class AlmacenDocumentosLocal : IAlmacenDocumentos
{
    private readonly string _directorio;

    public AlmacenDocumentosLocal(IConfiguration configuracion)
    {
        var raiz = configuracion["Almacenamiento:Ruta"] ?? "App_Data/Documentos";
        if (!Path.IsPathRooted(raiz))
        {
            raiz = Path.Combine(AppContext.BaseDirectory, raiz);
        }
        _directorio = Path.GetFullPath(raiz);
        Directory.CreateDirectory(_directorio);
    }

    public string GenerarNombreFisico(Guid folio, int version, string nombreOriginal)
    {
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10)
        {
            extension = ".bin";
        }

        return $"{folio:N}_{version:D3}{extension}";
    }

    public async Task<string> GuardarAsync(string nombreFisico, Stream contenido, CancellationToken ct = default)
    {
        var ruta = ObtenerRutaFisica(nombreFisico);
        await using var destino = File.Create(ruta);
        await contenido.CopyToAsync(destino, ct);
        return ruta;
    }

    public string ObtenerRutaFisica(string nombreFisico)
    {
        var nombre = Path.GetFileName(nombreFisico);
        return Path.Combine(_directorio, nombre);
    }

    public bool Existe(string nombreFisico) =>
        File.Exists(ObtenerRutaFisica(nombreFisico));

    public Stream Abrir(string nombreFisico) =>
        File.OpenRead(ObtenerRutaFisica(nombreFisico));
}