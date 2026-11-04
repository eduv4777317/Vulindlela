using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Data
{
    public class ClearanceDBContext : DbContext
    {
        public ClearanceDBContext(DbContextOptions<ClearanceDBContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<AppointmentRecord> Appointments { get; set; }
        public DbSet<DocumentRequirement> Requirements { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmittedDocument> SubmittedDocuments { get; set; }
        public DbSet<AccessLogEntry> AccessLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentRecord>(appointment =>
            {
                appointment.HasKey(a => a.AppointmentId);
                appointment.Property(a => a.AppointmentId).ValueGeneratedNever();
                appointment.Property(a => a.CitizenId).IsRequired().HasMaxLength(13);
                appointment.Property(a => a.ServiceType).IsRequired().HasMaxLength(32);
            });

            modelBuilder.Entity<DocumentRequirement>(requirement =>
            {
                requirement.Property(r => r.ServiceType).IsRequired().HasMaxLength(32);
                requirement.Property(r => r.DocumentType).IsRequired().HasMaxLength(32);
                requirement.HasIndex(r => new { r.ServiceType, r.DocumentType }).IsUnique();
            });

            modelBuilder.Entity<Submission>(submission =>
            {
                submission.HasOne<AppointmentRecord>().WithMany().HasForeignKey(s => s.AppointmentId);
                submission.HasMany(s => s.Documents).WithOne().HasForeignKey(d => d.SubmissionId);
                submission.HasIndex(s => s.AppointmentId);
            });

            modelBuilder.Entity<SubmittedDocument>(document =>
            {
                document.Property(d => d.DocumentType).IsRequired().HasMaxLength(32);
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
