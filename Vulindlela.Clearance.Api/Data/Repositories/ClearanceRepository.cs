using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Data.Repositories
{
    public class ClearanceRepository : IClearanceRepository
    {
        private readonly ClearanceDBContext _clearanceDBContext;

        public ClearanceRepository(ClearanceDBContext clearanceDBContext)
        {
            _clearanceDBContext = clearanceDBContext;
        }

        public async Task<AppointmentRecord?> GetAppointmentAsync(int appointmentId)
        {
            return await _clearanceDBContext.Appointments.FindAsync(appointmentId);
        }

        public async Task AddAppointmentAsync(AppointmentRecord appointment)
        {
            _clearanceDBContext.Appointments.Add(appointment);
            await _clearanceDBContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<DocumentRequirement>> GetRequirementsAsync(string serviceType)
        {
            return await _clearanceDBContext.Requirements
                .AsNoTracking()
                .Where(r => r.ServiceType == serviceType)
                .OrderBy(r => r.DocumentType)
                .ToListAsync();
        }

        public async Task AddSubmissionAsync(Submission submission)
        {
            _clearanceDBContext.Submissions.Add(submission);
            await _clearanceDBContext.SaveChangesAsync();
        }

        public async Task<Submission?> GetSubmissionWithDocumentsAsync(int submissionId)
        {
            return await _clearanceDBContext.Submissions
                .Include(s => s.Documents)
                .FirstOrDefaultAsync(s => s.Id == submissionId);
        }

        public async Task<IReadOnlyList<Submission>> GetLatestSubmissionsAsync(IReadOnlyCollection<int> appointmentIds)
        {
            var submissions = await _clearanceDBContext.Submissions
                .AsNoTracking()
                .Where(s => appointmentIds.Contains(s.AppointmentId))
                .ToListAsync();

            return submissions
                .GroupBy(s => s.AppointmentId)
                .Select(g => g.OrderByDescending(s => s.Id).First())
                .ToList();
        }

        public async Task<IReadOnlyList<int>> GetSubmissionIdsWithStatusAsync(params SubmissionStatus[] statuses)
        {
            return await _clearanceDBContext.Submissions
                .Where(s => statuses.Contains(s.Status))
                .OrderBy(s => s.Id)
                .Select(s => s.Id)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _clearanceDBContext.SaveChangesAsync();
        }
    }
}
