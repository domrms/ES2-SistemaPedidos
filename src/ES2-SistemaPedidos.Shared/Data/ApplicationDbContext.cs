using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Shared.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<EventoCliente> Eventos => Set<EventoCliente>();

    public DbSet<PedidoStatus> PedidoStatus => Set<PedidoStatus>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidarHistoricoImutavel();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidarHistoricoImutavel();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Cliente>(construtor =>
        {
            construtor.ToTable("clientes");
            construtor.HasKey(cliente => cliente.Id);
            construtor.Property(cliente => cliente.Id).HasColumnName("id").ValueGeneratedNever();
            construtor.Property(cliente => cliente.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Produto>(construtor =>
        {
            construtor.ToTable("produtos");
            construtor.HasKey(produto => produto.Id);
            construtor.Property(produto => produto.Id).HasColumnName("id").ValueGeneratedNever();
            construtor.Property(produto => produto.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<EventoCliente>(construtor =>
        {
            construtor.ToTable("eventos");
            construtor.HasKey(evento => evento.Id);
            construtor.Property(evento => evento.Id).HasColumnName("id");
            construtor.Property(evento => evento.ClienteId).HasColumnName("cliente_id").IsRequired();
            construtor.Property(evento => evento.ProdutoId).HasColumnName("produto_id").IsRequired();
            construtor.Property(evento => evento.EventoId).HasColumnName("evento_id").HasMaxLength(20).IsRequired();
            construtor.Property(evento => evento.DataHoraEvento).HasColumnName("data_hora_evento").IsRequired();
            construtor.Property(evento => evento.SalvoEm).HasColumnName("salvo_em").IsRequired();
            construtor.HasIndex(evento => evento.EventoId).IsUnique();
            construtor.HasIndex(evento => new { evento.ClienteId, evento.ProdutoId, evento.DataHoraEvento });

            construtor.HasOne(evento => evento.Cliente)
                .WithMany()
                .HasForeignKey(evento => evento.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            construtor.HasOne(evento => evento.Produto)
                .WithMany()
                .HasForeignKey(evento => evento.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ValidarHistoricoImutavel()
    {
        var possuiAlteracao = ChangeTracker.Entries<PedidoStatus>()
            .Any(entrada => entrada.State is EntityState.Modified or EntityState.Deleted);

        if (possuiAlteracao)
            throw new InvalidOperationException("O historico de status do pedido e imutavel.");
    }
}