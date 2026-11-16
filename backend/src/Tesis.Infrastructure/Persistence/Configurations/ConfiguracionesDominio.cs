using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tesis.Domain.Entities;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Infrastructure.Persistence.Configurations;

public sealed class AlumnoConfiguracion : IEntityTypeConfiguration<Alumno>
{
    public void Configure(EntityTypeBuilder<Alumno> builder)
    {
        builder.ToTable("Alumnos");
        builder.HasKey(a => a.IdAlumno);
        builder.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(a => a.NumeroControl).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Correo).HasMaxLength(320).IsRequired();

        builder.HasOne(a => a.Usuario)
            .WithOne(u => u.Alumno)
            .HasForeignKey<Alumno>(a => a.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Tesis)
            .WithOne(t => t.Alumno)
            .HasForeignKey(t => t.IdAlumno)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProfesorConfiguracion : IEntityTypeConfiguration<Profesor>
{
    public void Configure(EntityTypeBuilder<Profesor> builder)
    {
        builder.ToTable("Profesores");
        builder.HasKey(p => p.IdProfesor);
        builder.Property(p => p.Nombre).HasMaxLength(150).IsRequired();

        builder.HasOne(p => p.Usuario)
            .WithOne(u => u.Profesor)
            .HasForeignKey<Profesor>(p => p.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.ProgramasPosgrado)
            .WithOne(pp => pp.Profesor)
            .HasForeignKey(pp => pp.IdProfesor)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProfesorPosgradoConfiguracion : IEntityTypeConfiguration<ProfesorPosgrado>
{
    public void Configure(EntityTypeBuilder<ProfesorPosgrado> builder)
    {
        builder.ToTable("ProfesorPosgrado");
        builder.HasKey(pp => pp.IdProfesorPosgrado);
        builder.Property(pp => pp.Programa).HasMaxLength(150).IsRequired();
    }
}

public sealed class TesisConfiguracion : IEntityTypeConfiguration<EntidadTesis>
{
    public void Configure(EntityTypeBuilder<EntidadTesis> builder)
    {
        builder.ToTable("Tesis");
        builder.HasKey(t => t.Folio);
        builder.Property(t => t.Titulo).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Programa).HasMaxLength(150).IsRequired();

        builder.HasOne(t => t.Alumno)
            .WithMany(a => a.Tesis)
            .HasForeignKey(t => t.IdAlumno)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Asesor)
            .WithMany(p => p.TesisAsesoradas)
            .HasForeignKey(t => t.IdAsesor)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CoAsesor)
            .WithMany(p => p.TesisCoAsesoradas)
            .HasForeignKey(t => t.IdCoAsesor)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConfirmacionAsesoriaConfiguracion : IEntityTypeConfiguration<ConfirmacionAsesoria>
{
    public void Configure(EntityTypeBuilder<ConfirmacionAsesoria> builder)
    {
        builder.ToTable("ConfirmacionesAsesoria");
        builder.HasKey(c => c.IdConfirmacion);

        builder.HasOne(c => c.Tesis)
            .WithMany(t => t.Confirmaciones)
            .HasForeignKey(c => c.Folio)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Profesor)
            .WithMany(p => p.Confirmaciones)
            .HasForeignKey(c => c.IdProfesor)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentoTesisConfiguracion : IEntityTypeConfiguration<DocumentoTesis>
{
    public void Configure(EntityTypeBuilder<DocumentoTesis> builder)
    {
        builder.ToTable("DocumentosTesis");
        builder.HasKey(d => d.IdDocumento);
        builder.Property(d => d.Nombre).HasMaxLength(255).IsRequired();

        builder.HasOne(d => d.Tesis)
            .WithMany(t => t.Documentos)
            .HasForeignKey(d => d.Folio)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ArticuloConfiguracion : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> builder)
    {
        builder.ToTable("Articulos");
        builder.HasKey(a => a.IdArticulo);
        builder.Property(a => a.Nombre).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Url).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Doi).HasMaxLength(200);
        builder.Property(a => a.Issn).HasMaxLength(50);

        builder.HasOne(a => a.Tesis)
            .WithMany(t => t.Articulos)
            .HasForeignKey(a => a.Folio)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.TipoArticulo)
            .WithMany(t => t.Articulos)
            .HasForeignKey(a => a.IdTipoArticulo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TipoArticuloConfiguracion : IEntityTypeConfiguration<TipoArticulo>
{
    public void Configure(EntityTypeBuilder<TipoArticulo> builder)
    {
        builder.ToTable("TiposArticulo");
        builder.HasKey(t => t.IdTipoArticulo);
        builder.Property(t => t.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Descripcion).HasMaxLength(500);
    }
}

public sealed class RevisionIAConfiguracion : IEntityTypeConfiguration<RevisionIA>
{
    public void Configure(EntityTypeBuilder<RevisionIA> builder)
    {
        builder.ToTable("RevisionesIA");
        builder.HasKey(r => r.IdRevision);
        builder.Property(r => r.Porcentaje).HasPrecision(5, 2);
        builder.Property(r => r.ReporteJson).HasColumnType("nvarchar(max)");

        builder.HasOne(r => r.Tesis)
            .WithMany(t => t.RevisionesIA)
            .HasForeignKey(r => r.Folio)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RevisionAsesorConfiguracion : IEntityTypeConfiguration<RevisionAsesor>
{
    public void Configure(EntityTypeBuilder<RevisionAsesor> builder)
    {
        builder.ToTable("RevisionesAsesor");
        builder.HasKey(r => r.IdRevisionAsesor);
        builder.Property(r => r.Observaciones).HasMaxLength(1000);

        builder.HasOne(r => r.Tesis)
            .WithMany(t => t.RevisionesAsesor)
            .HasForeignKey(r => r.Folio)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Profesor)
            .WithMany()
            .HasForeignKey(r => r.IdProfesor)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DictamenConfiguracion : IEntityTypeConfiguration<Dictamen>
{
    public void Configure(EntityTypeBuilder<Dictamen> builder)
    {
        builder.ToTable("Dictamenes");
        builder.HasKey(d => d.IdDictamen);
        builder.Property(d => d.Observaciones).HasMaxLength(1000);

        builder.HasOne(d => d.Tesis)
            .WithMany(t => t.Dictamenes)
            .HasForeignKey(d => d.Folio)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FavoritoConfiguracion : IEntityTypeConfiguration<Favorito>
{
    public void Configure(EntityTypeBuilder<Favorito> builder)
    {
        builder.ToTable("Favoritos");
        builder.HasKey(f => f.IdFavorito);
        builder.HasIndex(f => new { f.IdUsuario, f.Folio }).IsUnique();

        builder.HasOne(f => f.Usuario)
            .WithMany()
            .HasForeignKey(f => f.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Tesis)
            .WithMany(t => t.Favoritos)
            .HasForeignKey(f => f.Folio)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class HistorialEstadoTesisConfiguracion : IEntityTypeConfiguration<HistorialEstadoTesis>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoTesis> builder)
    {
        builder.ToTable("HistorialEstadoTesis");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Observacion).HasMaxLength(1000);

        builder.HasOne(h => h.Tesis)
            .WithMany(t => t.HistorialEstados)
            .HasForeignKey(h => h.Folio)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.IdUsuario)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TesisVersionConfiguracion : IEntityTypeConfiguration<TesisVersion>
{
    public void Configure(EntityTypeBuilder<TesisVersion> builder)
    {
        builder.ToTable("TesisVersiones");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.RutaDocumento).HasMaxLength(500).IsRequired();

        builder.HasOne(v => v.Tesis)
            .WithMany(t => t.Versiones)
            .HasForeignKey(v => v.Folio)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Usuario)
            .WithMany()
            .HasForeignKey(v => v.IdUsuario)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class PosgradoConfiguracion : IEntityTypeConfiguration<Posgrado>
{
    public void Configure(EntityTypeBuilder<Posgrado> builder)
    {
        builder.ToTable("Posgrados");
        builder.HasKey(p => p.IdPosgrado);
        builder.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Nivel).IsRequired();
        builder.Property(p => p.Activo).IsRequired();
        builder.HasIndex(p => new { p.Nombre, p.Nivel }).IsUnique();
    }
}

