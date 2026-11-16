using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Application.Tests.Fakes;

public sealed class RepositorioPosgradosEnMemoria : IPosgradoRepository
{
    private readonly Dictionary<int, Posgrado> _posgrados = new();
    private int _siguienteId = 1;

    public RepositorioPosgradosEnMemoria()
    {
        Agregar(new Posgrado
        {
            IdPosgrado = _siguienteId++,
            Nivel = NivelPosgrado.Maestria,
            Nombre = "Maestría en Tecnologías de la Información",
            Activo = true,
        });

        Agregar(new Posgrado
        {
            IdPosgrado = _siguienteId++,
            Nivel = NivelPosgrado.Doctorado,
            Nombre = "Doctorado en Ciencias de la Ingeniería",
            Activo = true,
        });
    }

    public Task<List<Posgrado>> ObtenerTodosAsync(CancellationToken ct = default) =>
        Task.FromResult(_posgrados.Values.OrderBy(p => p.Nivel).ThenBy(p => p.Nombre).ToList());

    public Task<List<Posgrado>> ObtenerActivosAsync(CancellationToken ct = default) =>
        Task.FromResult(_posgrados.Values.Where(p => p.Activo).OrderBy(p => p.Nivel).ThenBy(p => p.Nombre).ToList());

    public Task<Posgrado?> ObtenerPorIdAsync(int idPosgrado, CancellationToken ct = default) =>
        Task.FromResult(_posgrados.TryGetValue(idPosgrado, out var p) ? p : null);

    public Task<Posgrado?> ObtenerPorNombreYNivelAsync(string nombre, NivelPosgrado nivel, CancellationToken ct = default) =>
        Task.FromResult(_posgrados.Values.FirstOrDefault(p =>
            p.Nivel == nivel && p.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AgregarAsync(Posgrado posgrado, CancellationToken ct = default)
    {
        Agregar(posgrado);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Posgrado posgrado, CancellationToken ct = default)
    {
        _posgrados.Remove(posgrado.IdPosgrado);
        return Task.CompletedTask;
    }

    private void Agregar(Posgrado posgrado)
    {
        if (posgrado.IdPosgrado == 0)
        {
            posgrado.IdPosgrado = _siguienteId++;
        }
        _posgrados[posgrado.IdPosgrado] = posgrado;
    }
}
