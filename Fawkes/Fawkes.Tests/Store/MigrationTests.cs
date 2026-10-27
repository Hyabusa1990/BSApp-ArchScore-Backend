using Microsoft.EntityFrameworkCore;

namespace Fawkes.Store
{
    [TestFixture]
    public class MigrationTests
    {
        // No connection is opened; the connection string only selects the provider.
        private const string ConnectionString = "Host=localhost;Database=fawkes";

        [Test]
        public void FawkesDbContextHasNoPendingModelChanges()
        {
            using var context = new FawkesDbContext(new DbContextOptionsBuilder<FawkesDbContext>().UseNpgsql(ConnectionString).Options);

            Assert.That(context.Database.GetMigrations(), Is.Not.Empty);
            Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        }

        [Test]
        public void IdentityDbContextHasNoPendingModelChanges()
        {
            using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(ConnectionString).Options);

            Assert.That(context.Database.GetMigrations(), Is.Not.Empty);
            Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        }
    }
}
