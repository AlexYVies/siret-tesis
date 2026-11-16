using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Application.Tests.Fakes;

public sealed class RepositorioProfesoresEnMemoria : IProfesorRepository
{
    private readonly Dictionary<int, Profesor> _porIdProfesor = new();
    private readonly Dictionary<int, Profesor> _porIdUsuario = new();
    private int _siguienteId = 1000;

    private RepositorioProfesoresEnMemoria()
    {
    }

    public static RepositorioProfesoresEnMemoria Sembrado()
    {
        var repositorio = new RepositorioProfesoresEnMemoria();
        repositorio.Agregar(new Profesor
        {
            IdProfesor = 101,
            IdUsuario = 2002,
            Nombre = "Dr. Juan Pérez",
            Activo = true,
        });

        repositorio._porIdProfesor[101].ProgramasPosgrado.Add(new ProfesorPosgrado
        {
            IdProfesorPosgrado = 1,
            IdProfesor = 101,
            Nivel = NivelPosgrado.Maestria,
            Programa = "Maestría en Tecnologías de la Información",
        });

        repositorio.Agregar(new Profesor
        {
            IdProfesor = 102,
            IdUsuario = 2003,
            Nombre = "Dra. Ana Gómez",
            Activo = true,
        });

        repositorio._porIdProfesor[102].ProgramasPosgrado.Add(new ProfesorPosgrado
        {
            IdProfesorPosgrado = 2,
            IdProfesor = 102,
            Nivel = NivelPosgrado.Maestria,
            Programa = "Maestría en Tecnologías de la Información",
        });

        return repositorio;
    }

    public Task<Profesor?> ObtenerConPosgradosAsync(int idProfesor, CancellationToken ct = default) =>
        Task.FromResult(_porIdProfesor.TryGetValue(idProfesor, out var profesor) ? profesor : null);

    public Task<Profesor?> ObtenerPorIdUsuarioAsync(int idUsuario, CancellationToken ct = default) =>
        Task.FromResult(_porIdUsuario.TryGetValue(idUsuario, out var profesor) ? profesor : null);

    public Task<List<Profesor>> ObtenerActivosAsync(CancellationToken ct = default) =>
        Task.FromResult(_porIdProfesor.Values.Where(p => p.Activo).ToList());

    public Task AgregarAsync(Profesor profesor, CancellationToken ct = default)
    {
        Agregar(profesor);
        return Task.CompletedTask;
    }

    public Task RemoverPosgradoDeProfesoresAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default)
    {
        foreach (var prof in _porIdProfesor.Values)
        {
            prof.ProgramasPosgrado.RemoveAll(pp => pp.Nivel == nivel && pp.Programa.Equals(programa, StringComparison.OrdinalIgnoreCase));
        }
        return Task.CompletedTask;
    }

    public Task ActualizarProgramaEnProfesoresAsync(
        NivelPosgrado nivelAnterior,
        string programaAnterior,
        NivelPosgrado nuevoNivel,
        string nuevoPrograma,
        CancellationToken ct = default)
    {
        foreach (var prof in _porIdProfesor.Values)
        {
            foreach (var pp in prof.ProgramasPosgrado)
            {
                if (pp.Nivel == nivelAnterior && pp.Programa.Equals(programaAnterior, StringComparison.OrdinalIgnoreCase))
                {
                    pp.Nivel = nuevoNivel;
                    pp.Programa = nuevoPrograma;
                }
            }
        }
        return Task.CompletedTask;
    }


    private void Agregar(Profesor profesor)
    {
        if (profesor.IdProfesor == 0)
        {
            profesor.IdProfesor = _siguienteId++;
        }

        _porIdProfesor[profesor.IdProfesor] = profesor;
        if (profesor.IdUsuario.HasValue)
        {
            _porIdUsuario[profesor.IdUsuario.Value] = profesor;
        }
    }
}