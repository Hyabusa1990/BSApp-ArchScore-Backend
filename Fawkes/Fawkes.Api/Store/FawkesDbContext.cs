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

        }

    }

    public class Fixture
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }
}
