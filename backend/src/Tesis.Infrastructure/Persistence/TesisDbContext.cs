using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tesis.Domain.Common;
using Tesis.Domain.Entities;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Infrastructure.Persistence;

public class TesisDbContext : IdentityDbContext<
    Usuario,
    Rol,
    int,
    IdentityUserClaim<int>,
    UsuarioRol,
    IdentityUserLogin<int>,
    IdentityRoleClaim<int>,
    IdentityUserToken<int>>
{
    public TesisDbContext(DbContextOptions<TesisDbContext> options)
        : base(options)
    {
    }

    public DbSet<Alumno> Alumnos => Set<Alumno>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<ProfesorPosgrado> ProfesoresPosgrado => Set<ProfesorPosgrado>();
    public DbSet<Posgrado> Posgrados => Set<Posgrado>();
    public DbSet<EntidadTesis> Tesis => Set<EntidadTesis>();
    public DbSet<ConfirmacionAsesoria> ConfirmacionesAsesoria => Set<ConfirmacionAsesoria>();
    public DbSet<DocumentoTesis> DocumentosTesis => Set<DocumentoTesis>();
    public DbSet<TesisVersion> TesisVersiones => Set<TesisVersion>();
    public DbSet<HistorialEstadoTesis> HistorialEstadosTesis => Set<HistorialEstadoTesis>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<TipoArticulo> TiposArticulo => Set<TipoArticulo>();
    public DbSet<RevisionIA> RevisionesIA => Set<RevisionIA>();
    public DbSet<RevisionAsesor> RevisionesAsesor => Set<RevisionAsesor>();
    public DbSet<Dictamen> Dictamenes => Set<Dictamen>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();

    public override int SaveChanges()
    {
        ApplyAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TesisDbContext).Assembly);
    }

    private void ApplyAudit()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.FechaReg == default)
                    {
                        entry.Entity.FechaReg = now;
                    }

                    entry.Entity.FechaMod = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.FechaMod = now;
                    break;
            }
        }
    }
}