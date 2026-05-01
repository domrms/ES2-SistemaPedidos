using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Shared.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    public DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(construtor =>
        {
            construtor.ToTable("pedidos");
            construtor.HasKey(pedido => pedido.Id);

            construtor.Property(pedido => pedido.Id).HasColumnName("id");
            construtor.Property(pedido => pedido.ClienteId).HasColumnName("cliente_id").HasMaxLength(255).IsRequired();
            construtor.Property(pedido => pedido.ValorTotal).HasColumnName("valor_total").HasPrecision(19, 2).IsRequired();
            construtor.Property(pedido => pedido.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            construtor.Property(pedido => pedido.CriadoEm).HasColumnName("criado_em").IsRequired();
            construtor.Property(pedido => pedido.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();
            construtor.Property(pedido => pedido.ProcessamentoIniciadoEm).HasColumnName("processamento_iniciado_em");
            construtor.Property(pedido => pedido.ConcluidoEm).HasColumnName("concluido_em");
            construtor.Property(pedido => pedido.MensagemErro).HasColumnName("mensagem_erro");
            construtor.Property(pedido => pedido.MotivoAprovacao).HasColumnName("motivo_aprovacao");
            construtor.Property(pedido => pedido.MotivoRejeicao).HasColumnName("motivo_rejeicao");

            construtor.HasMany(pedido => pedido.Itens)
                .WithOne(item => item.Pedido)
                .HasForeignKey(item => item.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            construtor.HasIndex(pedido => pedido.ClienteId);
            construtor.HasIndex(pedido => pedido.Status);
            construtor.HasIndex(pedido => pedido.CriadoEm);
            construtor.HasIndex(pedido => new { pedido.Status, pedido.AtualizadoEm });
        });

        modelBuilder.Entity<Pedido>(construtor =>
        {
            construtor.Navigation(pedido => pedido.Itens).HasField("_itens");
            construtor.Navigation(pedido => pedido.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ItemPedido>(construtor =>
        {
            construtor.ToTable("itens_pedido");
            construtor.HasKey(item => item.Id);

            construtor.Property(item => item.Id).HasColumnName("id");
            construtor.Property(item => item.PedidoId).HasColumnName("pedido_id").IsRequired();
            construtor.Property(item => item.ProdutoId).HasColumnName("produto_id").HasMaxLength(255).IsRequired();
            construtor.Property(item => item.Quantidade).HasColumnName("quantidade").IsRequired();
            construtor.Property(item => item.PrecoUnitario).HasColumnName("preco_unitario").HasPrecision(19, 2).IsRequired();
            construtor.Property(item => item.ValorLinha).HasColumnName("valor_linha").HasPrecision(19, 2).IsRequired();
            construtor.Property(item => item.Descricao).HasColumnName("descricao").HasMaxLength(500);

            construtor.HasIndex(item => item.PedidoId);
            construtor.HasIndex(item => item.ProdutoId);
        });

        modelBuilder.Entity<MensagemProcessada>(construtor =>
        {
            construtor.ToTable("mensagens_processadas");
            construtor.HasKey(mensagem => mensagem.MensagemId);

            construtor.Property(mensagem => mensagem.MensagemId).HasColumnName("mensagem_id").HasMaxLength(255);
            construtor.Property(mensagem => mensagem.PedidoId).HasColumnName("pedido_id");
            construtor.Property(mensagem => mensagem.ProcessadaEm).HasColumnName("processada_em").IsRequired();
            construtor.Property(mensagem => mensagem.TipoMensagem).HasColumnName("tipo_mensagem").HasMaxLength(100).IsRequired();
            construtor.Property(mensagem => mensagem.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            construtor.Property(mensagem => mensagem.DetalhesErro).HasColumnName("detalhes_erro");

            construtor.HasOne(mensagem => mensagem.Pedido)
                .WithMany()
                .HasForeignKey(mensagem => mensagem.PedidoId)
                .OnDelete(DeleteBehavior.SetNull);

            construtor.HasIndex(mensagem => new { mensagem.PedidoId, mensagem.ProcessadaEm });
            construtor.HasIndex(mensagem => new { mensagem.TipoMensagem, mensagem.ProcessadaEm });
        });
    }
}
