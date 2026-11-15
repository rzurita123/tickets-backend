using Microsoft.EntityFrameworkCore;
using SistemaTickets.Domain.Auditoria;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Notificaciones;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Tickets;
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
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Solucion> Soluciones => Set<Solucion>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<ParametroSugerencia> ParametrosSugerencia => Set<ParametroSugerencia>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuracion) =>
        configuracion.Properties<DateTime>().HaveConversion<FechaUtc>();

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

        modelo.Entity<Ticket>(e =>
        {
            e.ToTable("Tickets");
            e.Property(x => x.Titulo).HasMaxLength(200);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Numero).IsUnique();
            e.HasOne<Localidad>().WithMany().HasForeignKey(x => x.LocalidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Sistema>().WithMany().HasForeignKey(x => x.SistemaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Subsistema>().WithMany().HasForeignKey(x => x.SubsistemaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Criticidad>().WithMany().HasForeignKey(x => x.CriticidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TipoProblema>().WithMany().HasForeignKey(x => x.TipoProblemaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<GrupoSoporte>().WithMany().HasForeignKey(x => x.ElevadoAGrupoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.OperadorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ElevadoAUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Solucion>().WithMany().HasForeignKey(x => x.SolucionAplicadaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Actividades).WithOne().HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Adjuntos).WithOne().HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Actividad>(e =>
        {
            e.ToTable("Actividades");
            e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EstadoNuevo).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.OperadorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.DestinoUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<GrupoSoporte>().WithMany().HasForeignKey(x => x.DestinoGrupoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelo.Entity<Adjunto>(e =>
        {
            e.ToTable("Adjuntos", t => t.HasCheckConstraint(
                "CK_Adjuntos_Pertenencia",
                "([TicketId] IS NOT NULL AND [SolucionId] IS NULL) OR ([TicketId] IS NULL AND [SolucionId] IS NOT NULL)"));
            e.Property(x => x.Origen).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Clave).HasMaxLength(60);
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.TipoContenido).HasMaxLength(100);
            e.HasOne(x => x.Actividad).WithMany().HasForeignKey(x => x.ActividadId).OnDelete(DeleteBehavior.ClientCascade);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SubidoPorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelo.Entity<Solucion>(e =>
        {
            e.ToTable("Soluciones");
            e.Property(x => x.Codigo).HasMaxLength(60);
            e.Property(x => x.Titulo).HasMaxLength(200);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MotivoRechazo).HasMaxLength(500);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasOne<Sistema>().WithMany().HasForeignKey(x => x.SistemaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Subsistema>().WithMany().HasForeignKey(x => x.SubsistemaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Ticket>().WithMany().HasForeignKey(x => x.TicketOrigenId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.PropuestaPorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RevisadaPorId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Adjuntos).WithOne().HasForeignKey(a => a.SolucionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Favorito>(e =>
        {
            e.ToTable("Favoritos", t => t.HasCheckConstraint(
                "CK_Favoritos_Elemento",
                "([TicketId] IS NOT NULL AND [SolucionId] IS NULL) OR ([TicketId] IS NULL AND [SolucionId] IS NOT NULL)"));
            e.HasIndex(x => new { x.UsuarioId, x.TicketId }).IsUnique().HasFilter("[TicketId] IS NOT NULL");
            e.HasIndex(x => new { x.UsuarioId, x.SolucionId }).IsUnique().HasFilter("[SolucionId] IS NOT NULL");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Ticket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Solucion>().WithMany().HasForeignKey(x => x.SolucionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Notificacion>(e =>
        {
            e.ToTable("Notificaciones");
            e.Property(x => x.Destino).HasMaxLength(200);
            e.Property(x => x.Canal).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Asunto).HasMaxLength(200);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Estado);
            e.HasOne<Ticket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.DestinatarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelo.Entity<RegistroAuditoria>(e =>
        {
            e.ToTable("RegistrosAuditoria");
            e.Property(x => x.Operacion).HasMaxLength(40);
            e.Property(x => x.Entidad).HasMaxLength(40);
            e.Property(x => x.EntidadId).HasMaxLength(40);
            e.Property(x => x.Detalle).HasMaxLength(500);
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.Entidad, x.EntidadId });
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelo.Entity<ParametroSugerencia>(e =>
        {
            e.ToTable("ParametrosSugerencia");
            e.HasKey(x => x.Clave);
            e.Property(x => x.Clave).HasMaxLength(40);
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

public class FechaUtc() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
