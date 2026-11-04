using Microsoft.EntityFrameworkCore;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Data
{
    public class PortalDBContext : DbContext
    {
        public PortalDBContext(DbContextOptions<PortalDBContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(account =>
            {
                account.Property(a => a.Username).IsRequired().HasMaxLength(64);
                account.Property(a => a.DisplayName).IsRequired().HasMaxLength(100);
                account.Property(a => a.CitizenId).IsRequired().HasMaxLength(13);
                account.HasIndex(a => a.Username).IsUnique();
                account.HasIndex(a => a.CitizenId).IsUnique();
            });
        }
    }
}
