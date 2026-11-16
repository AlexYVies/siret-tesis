namespace Tesis.Application.Abstracciones;

public interface IAlmacenDocumentos
{
    Task<string> GuardarAsync(string nombreFisico, Stream contenido, CancellationToken ct = default);

    string ObtenerRutaFisica(string nombreFisico);

    bool Existe(string nombreFisico);

    Stream Abrir(string nombreFisico);

    string GenerarNombreFisico(Guid folio, int version, string nombreOriginal);
}