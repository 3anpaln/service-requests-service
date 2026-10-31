using Microsoft.EntityFrameworkCore;
using service_requests_service.Persistent.Entities;

namespace service_requests_service.Persistent.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<InboxEvent> InboxEvents => Set<InboxEvent>();
    public DbSet<DoneServiceRequest> DoneServiceRequests => Set<DoneServiceRequest>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        #region Service Request
        
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("ServiceRequests");
        
            entity.HasKey(x => x.Id);
        
            entity.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();
        
            entity.Property(x => x.Title)
                .HasColumnName("Title")
                .HasMaxLength(200)
                .IsRequired();
        
            entity.Property(x => x.Description)
                .HasColumnName("Description")
                .HasMaxLength(2000);
        
            entity.Property(x => x.Priority)
                .HasColumnName("Priority")
                .HasConversion<string>()
                .IsRequired();
        
            entity.Property(x => x.Status)
                .HasColumnName("Status")
                .HasConversion<string>()
                .IsRequired();
        
            entity.Property(x => x.CreatedAt)
                .HasColumnName("Created_at")
                .IsRequired();
        
            entity.Property(x => x.UpdatedAt)
                .HasColumnName("Updated_at");
        
            //For production possibly need some index
        });
        
        #endregion
        
        #region Outbox Event
        
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("OutboxEvents");
        
            entity.HasKey(x => x.IdempotencyKey);
            
            entity.Property(x => x.IdempotencyKey)
                .HasColumnName("IdempotencyKey")
                .IsRequired();
        
            entity.Property(x => x.EventType)
                .HasColumnName("EventType")
                .HasConversion<string>()
                .IsRequired();
        
            entity.Property(x => x.Payload)
                .HasColumnName("Payload")
                .IsRequired();
            
            entity.Property(x => x.OccuredAt)
                .HasColumnName("OccuredAt")
                .IsRequired();
            
            entity.Property(x => x.IsSend)
                .HasColumnName("IsSend")
                .HasDefaultValue(false)
                .IsRequired();
        });
        
        #endregion
        
        #region Inbox Event
        
        modelBuilder.Entity<InboxEvent>(entity =>
        {
            entity.ToTable("InboxEvents");
            
            entity.HasKey(x => x.IdempotencyKey);
            entity.Property(x => x.IdempotencyKey)
                .HasColumnName("IdempotencyKey")
                .IsRequired();
            
            entity.Property(x => x.ConsumerName)
                .HasColumnName("ConsumerName")
                .HasMaxLength(400)
                .IsRequired();
        
            entity.Property(x => x.ProcessedAt)
                .HasColumnName("ProcessedAt")
                .IsRequired();
        });
        
        #endregion
        
        #region Done Service Request
        
        modelBuilder.Entity<DoneServiceRequest>(entity =>
        {
            entity.ToTable("DoneServiceRequests");
            
            entity.HasKey(x => x.Id);
        
            entity.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();
        
            entity.Property(x => x.Title)
                .HasColumnName("Title")
                .HasMaxLength(200)
                .IsRequired();
        
            entity.Property(x => x.Description)
                .HasColumnName("Description")
                .HasMaxLength(2000);
        
            entity.Property(x => x.Priority)
                .HasColumnName("Priority")
                .HasConversion<string>()
                .IsRequired();
        
            entity.Property(x => x.Status)
                .HasColumnName("Status")
                .HasConversion<string>()
                .IsRequired();
        
            entity.Property(x => x.CreatedAt)
                .HasColumnName("Created_at")
                .IsRequired();
        });
        
        #endregion
    }
}