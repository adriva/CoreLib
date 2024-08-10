using Microsoft.EntityFrameworkCore;

namespace Adriva.Extensions.Analytics.Entities
{
    public class AnalyticsDbContext : DbContext
    {

        public DbSet<AnalyticsItem> AnalyticsItems { get; set; }

        public DbSet<ExceptionItem> Exceptions { get; set; }

        public DbSet<RequestItem> Requests { get; set; }

        public DbSet<MessageItem> Messages { get; set; }

        public DbSet<MetricItem> Metrics { get; set; }

        public DbSet<EventItem> Events { get; set; }

        public DbSet<DependencyItem> Dependencies { get; set; }

        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
                    : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = false;
            this.ChangeTracker.LazyLoadingEnabled = false;
            this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            this.Database.SetCommandTimeout(20);
        }

        public string GetSql() => this.Database.GenerateCreateScript();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalyticsItem>(e =>
            {
                e.Property(x => x.Id).UseSqlServerIdentityColumn();
                e.ToTable("AnalyticsItem")
                    .HasKey(x => x.Id)
                    .ForSqlServerIsClustered();
            });

            modelBuilder.Entity<ExceptionItem>(e =>
            {
                e.HasKey(ex => ex.Id).ForSqlServerIsClustered();
                e.Property(x => x.Id).UseSqlServerIdentityColumn();

                e.HasOne(ex => ex.AnalyticsItem)
                    .WithMany(a => a.Exceptions)
                    .HasForeignKey(ex => ex.AnalyticsItemId);
            });

            modelBuilder.Entity<MetricItem>(e =>
            {
                e.HasKey(ex => ex.Id).ForSqlServerIsClustered();

                e.HasOne(ex => ex.AnalyticsItem)
                    .WithMany(a => a.Metrics)
                    .HasForeignKey(ex => ex.AnalyticsItemId);

                e.Property(x => x.Kind).HasConversion<string>();
            });

            modelBuilder.Entity<EventItem>(e =>
            {
                e.HasKey(ex => ex.Id).ForSqlServerIsClustered();

                e.HasOne(ex => ex.AnalyticsItem)
                    .WithMany(a => a.Events)
                    .HasForeignKey(ex => ex.AnalyticsItemId);
            });

            modelBuilder.Entity<DependencyItem>(e =>
            {
                e.HasKey(ex => ex.Id).ForSqlServerIsClustered();

                e.HasOne(ex => ex.AnalyticsItem)
                    .WithMany(a => a.Dependencies)
                    .HasForeignKey(ex => ex.AnalyticsItemId);
            });

            modelBuilder.Entity<RequestItem>(e =>
            {
                e.HasKey(ri => ri.Id).ForSqlServerIsClustered();

                e.HasOne(ri => ri.AnalyticsItem)
                    .WithOne(a => a.RequestItem)
                    .HasForeignKey<RequestItem>(ri => ri.Id);
            });

            modelBuilder.Entity<MessageItem>(e =>
            {
                e.HasKey(mi => mi.Id).ForSqlServerIsClustered();

                e.HasOne(mi => mi.AnalyticsItem)
                    .WithOne(a => a.MessageItem)
                    .HasForeignKey<MessageItem>(mi => mi.AnalyticsItemId);
                e.Property(x => x.Severity).HasConversion<int?>();
            });
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }
}