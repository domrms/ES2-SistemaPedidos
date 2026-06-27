using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ES2_SistemaPedidos.Shared.Data.Configurations;

public sealed class PedidoStatusConfiguration : IEntityTypeConfiguration<PedidoStatus>
{
    public void Configure(EntityTypeBuilder<PedidoStatus> builder)
    {
        builder.ToTable("pedido_status");
        builder.HasKey(status => status.Id);
        builder.Property(status => status.Id).HasColumnName("id");
        builder.Property(status => status.PedidoId).HasColumnName("pedido_id").IsRequired();
        builder.Property(status => status.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(status => status.RegistradoEm).HasColumnName("registrado_em").IsRequired();
        builder.Property(status => status.Detalhe).HasColumnName("detalhe").HasMaxLength(500);
        builder.HasIndex(status => new { status.PedidoId, status.Id });
        builder.HasIndex(status => new { status.PedidoId, status.Status }).IsUnique();

        builder.HasOne(status => status.Pedido)
            .WithMany(pedido => pedido.HistoricoStatus)
            .HasForeignKey(status => status.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
