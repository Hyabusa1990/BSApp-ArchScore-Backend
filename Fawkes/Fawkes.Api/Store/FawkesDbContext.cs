using Microsoft.EntityFrameworkCore;

namespace Fawkes.Api.Store
{
    public class FawkesDbContext : DbContext
    {
        public FawkesDbContext(DbContextOptions<FawkesDbContext> options) : base(options)
        {
        }

        public DbSet<Fixture> Fixtures { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Core");

            modelBuilder.Entity<Fixture>()
                .HasMany<Device>()
                .WithOne(d => d.Fixture)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Fixture>()
                .HasMany<FixturePermission>()
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);


        }

    }

    public class Fixture
    {
        public int Id { get; set; }
        public Guid UniqueId { get; set; }
        public DateTime Date { get; set; }
        public string? LeagueName { get; set; }
        public string? FixtureName { get; set; }

    }

    public class FixturePermission
    {
        public int Id { get; set; }
        public int FixtureId { get; set; }
        public required string User { get; set; }
    }

    public class Device
    {
        public int Id { get; set; }
        public int? FixtureId { get; set; } 
        public required string Code { get; set; }
        public DisplayType DisplayType { get; set; }
        public Fixture? Fixture { get; set; }
    }



    public enum DisplayType
    {
        None = 0,
        Match = 1,
        Table = 2
    }

    public enum AccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2,
        Owner = 3
    }
}
