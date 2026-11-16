using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;

namespace Tesis.Application.Tests.Fakes;

public sealed class RepositorioAlumnosEnMemoria : IAlumnoRepository
{
    private readonly Alumno _alumno;

    public RepositorioAlumnosEnMemoria(Alumno alumno)
    {
        _alumno = alumno;
    }

    public Task<Alumno?> ObtenerPorIdUsuarioAsync(int idUsuario, CancellationToken ct = default) =>
        Task.FromResult(_alumno.IdUsuario == idUsuario ? _alumno : null);

    public Task AgregarAsync(Alumno alumno, CancellationToken ct = default)
    {
        if (_alumno.IdAlumno == 0 && _alumno.IdUsuario == 0 && alumno.IdUsuario != 0)
        {
            _alumno.IdUsuario = alumno.IdUsuario;
            _alumno.FechaReg = alumno.FechaReg;
            _alumno.Nombre = alumno.Nombre;
            _alumno.NumeroControl = alumno.NumeroControl;
            _alumno.Correo = alumno.Correo;
        }

        return Task.CompletedTask;
    }
}