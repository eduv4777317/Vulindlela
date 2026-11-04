using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Data.Repositories
{
    public interface IClearanceRepository
    {
        Task<AppointmentRecord?> GetAppointmentAsync(int appointmentId);
        Task AddAppointmentAsync(AppointmentRecord appointment);

        Task<IReadOnlyList<DocumentRequirement>> GetRequirementsAsync(string serviceType);

        Task AddSubmissionAsync(Submission submission);
        Task<Submission?> GetSubmissionWithDocumentsAsync(int submissionId);
        Task<IReadOnlyList<Submission>> GetLatestSubmissionsAsync(IReadOnlyCollection<int> appointmentIds);
        Task<IReadOnlyList<int>> GetSubmissionIdsWithStatusAsync(params SubmissionStatus[] statuses);

        Task SaveChangesAsync();
    }
}
