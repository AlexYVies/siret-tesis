using Tesis.Application.Abstracciones;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class UnidadDeTrabajoEf : IUnitOfWork
{
    private readonly TesisDbContext _contexto;

    public UnidadDeTrabajoEf(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _contexto.SaveChangesAsync(ct);
}