using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Services
{
    public interface IClearanceService
    {
        Task<bool> RegisterAppointmentAsync(int appointmentId, string citizenId, string serviceType, DateTime startsAtUtc);

        Task<IReadOnlyList<DocumentRequirement>> GetRequirementsAsync(string serviceType);

        Task<SubmitResult> SubmitDocumentsAsync(int appointmentId, IReadOnlyList<DocumentInput> documents);

        Task VerifySubmissionAsync(int submissionId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ClearanceStatus>> GetClearancesAsync(IReadOnlyCollection<int> appointmentIds);

        Task<IReadOnlyList<int>> GetUnfinishedSubmissionIdsAsync();
    }
}
