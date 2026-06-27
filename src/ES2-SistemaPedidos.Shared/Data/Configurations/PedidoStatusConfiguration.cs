using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ES2_SistemaPedidos.Shared.Data.Configurations;

public sealed class PedidoStatusConfiguration : IEntityTypeConfiguration<PedidoStatus>
{
    public void Configure(EntityTypeBuilder<PedidoStatus> construtor)
    {
        construtor.ToTable("pedido_status");
        construtor.HasKey(status => status.Id);
        construtor.Property(status => status.Id).HasColumnName("id");
        construtor.Property(status => status.PedidoId).HasColumnName("pedido_id").IsRequired();
        construtor.Property(status => status.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        construtor.Property(status => status.RegistradoEm).HasColumnName("registrado_em").IsRequired();
        construtor.Property(status => status.Detalhe).HasColumnName("detalhe").HasMaxLength(500);
        construtor.HasIndex(status => new { status.PedidoId, status.Id });
        construtor.HasIndex(status => new { status.PedidoId, status.Status }).IsUnique();

        construtor.HasOne(status => status.Pedido)
            .WithMany(pedido => pedido.HistoricoStatus)
            .HasForeignKey(status => status.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
