using Microsoft.EntityFrameworkCore;

namespace Fawkes.Api.Store
{
    public class FawkesDbContext : DbContext
    {
        public FawkesDbContext(DbContextOptions<FawkesDbContext> options) : base(options)
        {
        }

        public DbSet<Fixture> Fixtures { get; set; }

        public DbSet<Device> Devices { get; set; }

        public DbSet<FixturePermission> FixturePermissions { get; set; }

        public DbSet<TargetAssignment> TargetAssignments { get; set; }

        public DbSet<Team> Teams { get; set; }

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
                .WithOne(_ => _.Fixture)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Fixture>()
                .HasMany<Team>()
                .WithOne(_ => _.Fixture)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Fixture>()
                .HasMany<TargetAssignment>()
                .WithOne(_ => _.Fixture)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Team>()
                .HasMany<TargetAssignment>()
                .WithOne(_ => _.Team)
                .OnDelete(DeleteBehavior.Cascade);





        }

        public class Fixture
        {
            public int Id { get; set; }
            public Guid UniqueId { get; set; }
            public DateTime Date { get; set; }
            public string? LeagueName { get; set; }
            public string? FixtureName { get; set; }
            public string? Location { get; set; }

        }

        public class FixturePermission
        {
            public int Id { get; set; }
            public int FixtureId { get; set; }
            public required string User { get; set; }
            public required AccessLevel AccessLevel { get; set; }
            public required Fixture Fixture { get; set; }
        }

        public class Device
        {
            public int Id { get; set; }
            public int? FixtureId { get; set; }
            public required string Code { get; set; }
            public DisplayType DisplayType { get; set; }
            public DisplayTheme DisplayTheme { get; set; }
            public int? MatchNo { get; set; }
            public Fixture? Fixture { get; set; }
        }

        public class TargetAssignment
        {
            public int Id { get; set; }
            public int FixtureId { get; set; }
            public int TeamId { get; set; }
            public int RoundNo { get; set; }
            public int TargetNo { get; set; }

            public Fixture Fixture { get; set; }
            public Team Team { get; set; }
        }


        public class Team
        {
            public int Id { get; set; }
            public int FixtureId { get; set; }
            public required string Name { get; set; }
            public int MatchPointsWon { get; set; }
            public int MatchPointsLost { get; set; }
            public int SetPointsWon { get; set; }
            public int SetPointsLost { get; set; }
            public Fixture Fixture { get; set; }
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

        public enum DisplayTheme
        {
            Dark = 0,
            Light = 1

        }


    }
}
