using Microsoft.EntityFrameworkCore;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Infrastructure.Persistencia;

public class ContextoTickets(DbContextOptions<ContextoTickets> opciones) : DbContext(opciones)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Localidad> Localidades => Set<Localidad>();
    public DbSet<Sistema> Sistemas => Set<Sistema>();
    public DbSet<Subsistema> Subsistemas => Set<Subsistema>();
    public DbSet<TipoProblema> TiposProblema => Set<TipoProblema>();
    public DbSet<Criticidad> Criticidades => Set<Criticidad>();
    public DbSet<GrupoSoporte> GruposSoporte => Set<GrupoSoporte>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Localidad>(e =>
        {
            e.ToTable("Localidades");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelo.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.NombreUsuario).HasMaxLength(30);
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.HashContrasena).HasMaxLength(200);
            e.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Cargo).HasMaxLength(100);
            e.HasIndex(x => x.NombreUsuario).IsUnique();
            e.HasOne<Localidad>().WithMany().HasForeignKey(x => x.LocalidadId).OnDelete(DeleteBehavior.Restrict);
        });

        Catalogo<Sistema>(modelo, "Sistemas");
        Catalogo<TipoProblema>(modelo, "TiposProblema");
        Catalogo<Criticidad>(modelo, "Criticidades");
        Catalogo<GrupoSoporte>(modelo, "GruposSoporte");
        Catalogo<Subsistema>(modelo, "Subsistemas")
            .HasOne<Sistema>().WithMany().HasForeignKey(x => x.SistemaId).OnDelete(DeleteBehavior.Restrict);
    }

    private static Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> Catalogo<T>(ModelBuilder modelo, string tabla)
        where T : ItemCatalogo
    {
        var e = modelo.Entity<T>();
        e.ToTable(tabla);
        e.Property(x => x.Codigo).HasMaxLength(60);
        e.Property(x => x.Nombre).HasMaxLength(100);
        e.HasIndex(x => x.Codigo).IsUnique();
        return e;
    }
}
