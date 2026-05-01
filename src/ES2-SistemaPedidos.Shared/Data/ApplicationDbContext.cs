using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Shared.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("orders");
            builder.HasKey(order => order.Id);

            builder.Property(order => order.Id).HasColumnName("id");
            builder.Property(order => order.CustomerId).HasColumnName("customer_id").HasMaxLength(255).IsRequired();
            builder.Property(order => order.TotalAmount).HasColumnName("total_amount").HasPrecision(19, 2).IsRequired();
            builder.Property(order => order.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            builder.Property(order => order.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(order => order.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(order => order.ProcessingStartedAt).HasColumnName("processing_started_at");
            builder.Property(order => order.CompletedAt).HasColumnName("completed_at");
            builder.Property(order => order.ErrorMessage).HasColumnName("error_message");
            builder.Property(order => order.ApprovalReason).HasColumnName("approval_reason");
            builder.Property(order => order.RejectionReason).HasColumnName("rejection_reason");

            builder.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(order => order.CustomerId);
            builder.HasIndex(order => order.Status);
            builder.HasIndex(order => order.CreatedAt);
            builder.HasIndex(order => new { order.Status, order.UpdatedAt });
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.Navigation(order => order.Items).HasField("_items");
            builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("order_items");
            builder.HasKey(item => item.Id);

            builder.Property(item => item.Id).HasColumnName("id");
            builder.Property(item => item.OrderId).HasColumnName("order_id").IsRequired();
            builder.Property(item => item.ProductId).HasColumnName("product_id").HasMaxLength(255).IsRequired();
            builder.Property(item => item.Quantity).HasColumnName("quantity").IsRequired();
            builder.Property(item => item.UnitPrice).HasColumnName("unit_price").HasPrecision(19, 2).IsRequired();
            builder.Property(item => item.LineTotal).HasColumnName("line_total").HasPrecision(19, 2).IsRequired();
            builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(500);

            builder.HasIndex(item => item.OrderId);
            builder.HasIndex(item => item.ProductId);
        });

        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.ToTable("processed_messages");
            builder.HasKey(message => message.MessageId);

            builder.Property(message => message.MessageId).HasColumnName("message_id").HasMaxLength(255);
            builder.Property(message => message.OrderId).HasColumnName("order_id");
            builder.Property(message => message.ProcessedAt).HasColumnName("processed_at").IsRequired();
            builder.Property(message => message.MessageType).HasColumnName("message_type").HasMaxLength(100).IsRequired();
            builder.Property(message => message.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            builder.Property(message => message.ErrorDetails).HasColumnName("error_details");

            builder.HasOne(message => message.Order)
                .WithMany()
                .HasForeignKey(message => message.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(message => new { message.OrderId, message.ProcessedAt });
            builder.HasIndex(message => new { message.MessageType, message.ProcessedAt });
        });
    }
}
