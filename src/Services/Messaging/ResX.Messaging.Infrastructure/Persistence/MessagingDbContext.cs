using Microsoft.EntityFrameworkCore;
using ResX.Messaging.Domain.AggregateRoots;
using ResX.Messaging.Domain.Entities;

namespace ResX.Messaging.Infrastructure.Persistence;

public class MessagingDbContext : DbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagingDbContext).Assembly);
        modelBuilder.HasDefaultSchema("messaging");
    }
}