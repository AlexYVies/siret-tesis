using Tesis.Application.Abstracciones;

namespace Tesis.Application.Tests.Fakes;

public sealed class UnidadDeTrabajoNop : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        Task.FromResult(1);
}