using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Shared.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<EventoCliente> Eventos => Set<EventoCliente>();

    public DbSet<PedidoStatus> PedidoStatus => Set<PedidoStatus>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateImmutableHistory();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateImmutableHistory();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Cliente>(builder =>
        {
            builder.ToTable("clientes");
            builder.HasKey(cliente => cliente.Id);
            builder.Property(cliente => cliente.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(cliente => cliente.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Produto>(builder =>
        {
            builder.ToTable("produtos");
            builder.HasKey(produto => produto.Id);
            builder.Property(produto => produto.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(produto => produto.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<EventoCliente>(builder =>
        {
            builder.ToTable("eventos");
            builder.HasKey(evento => evento.Id);
            builder.Property(evento => evento.Id).HasColumnName("id");
            builder.Property(evento => evento.ClienteId).HasColumnName("cliente_id").IsRequired();
            builder.Property(evento => evento.ProdutoId).HasColumnName("produto_id").IsRequired();
            builder.Property(evento => evento.EventoId).HasColumnName("evento_id").HasMaxLength(20).IsRequired();
            builder.Property(evento => evento.DataHoraEvento).HasColumnName("data_hora_evento").IsRequired();
            builder.Property(evento => evento.SalvoEm).HasColumnName("salvo_em").IsRequired();
            builder.HasIndex(evento => evento.EventoId).IsUnique();
            builder.HasIndex(evento => new { evento.ClienteId, evento.ProdutoId, evento.DataHoraEvento });

            builder.HasOne(evento => evento.Cliente)
                .WithMany()
                .HasForeignKey(evento => evento.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(evento => evento.Produto)
                .WithMany()
                .HasForeignKey(evento => evento.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ValidateImmutableHistory()
    {
        var hasChanges = ChangeTracker.Entries<PedidoStatus>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (hasChanges)
            throw new InvalidOperationException("O historico de status do pedido e imutavel.");
    }
}
