using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Data
{
    public class BookingDBContext : DbContext
    {
        public BookingDBContext(DbContextOptions<BookingDBContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Slot> Slots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AccessLogEntry> AccessLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Branch>(branch =>
            {
                branch.Property(b => b.Name).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Slot>(slot =>
            {
                slot.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId);

                slot.HasIndex(s => new { s.BranchId, s.StartsAt }).IsUnique();
            });

            modelBuilder.Entity<Appointment>(appointment =>
            {
                appointment.HasOne<Slot>().WithMany().HasForeignKey(a => a.SlotId);
                appointment.Property(a => a.CitizenId).IsRequired().HasMaxLength(13);
                appointment.Property(a => a.ServiceType).IsRequired().HasMaxLength(32);

                appointment.HasIndex(a => a.SlotId)
                    .IsUnique()
                    .HasFilter($"\"Status\" = {(int)AppointmentStatus.Active}");
            });

            modelBuilder.Entity<AccessLogEntry>(entry =>
            {
                entry.Property(e => e.CitizenId).IsRequired().HasMaxLength(13);
                entry.Property(e => e.AccessedBy).IsRequired().HasMaxLength(100);
                entry.Property(e => e.AccessedByRole).IsRequired().HasMaxLength(20);
                entry.Property(e => e.Reason).IsRequired().HasMaxLength(200);
                entry.HasIndex(e => e.CitizenId);
            });
        }
    }
}
