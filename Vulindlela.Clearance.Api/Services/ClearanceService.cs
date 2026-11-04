using Vulindlela.Clearance.Api.Data.Repositories;
using Vulindlela.Clearance.Api.Models;
using Vulindlela.Clearance.Api.Queue;

namespace Vulindlela.Clearance.Api.Services
{
    public class ClearanceService : IClearanceService
    {
        public const int MaxCertificationAgeMonths = 3;

        private readonly IClearanceRepository _clearanceRepository;
        private readonly IVerificationQueue _verificationQueue;
        private readonly IDocumentScanner _documentScanner;

        public ClearanceService(IClearanceRepository clearanceRepository, IVerificationQueue verificationQueue, IDocumentScanner documentScanner)
        {
            _clearanceRepository = clearanceRepository;
            _verificationQueue = verificationQueue;
            _documentScanner = documentScanner;
        }

        public async Task<bool> RegisterAppointmentAsync(int appointmentId, string citizenId, string serviceType, DateTime startsAtUtc)
        {
            if (await _clearanceRepository.GetAppointmentAsync(appointmentId) is not null)
                return false;

            await _clearanceRepository.AddAppointmentAsync(new AppointmentRecord
            {
                AppointmentId = appointmentId,
                CitizenId = citizenId,
                ServiceType = serviceType.Trim().ToUpperInvariant(),
                StartsAt = startsAtUtc,
                ReceivedAt = DateTime.UtcNow
            });

            return true;
        }

        public Task<IReadOnlyList<DocumentRequirement>> GetRequirementsAsync(string serviceType)
        {
            return _clearanceRepository.GetRequirementsAsync(serviceType.Trim().ToUpperInvariant());
        }

        public async Task<SubmitResult> SubmitDocumentsAsync(int appointmentId, IReadOnlyList<DocumentInput> documents)
        {
            if (await _clearanceRepository.GetAppointmentAsync(appointmentId) is null)
                return new SubmitResult(SubmitOutcome.AppointmentNotFound);

            var submission = new Submission
            {
                AppointmentId = appointmentId,
                Status = SubmissionStatus.Queued,
                SubmittedAt = DateTime.UtcNow,
                Documents = documents.Select(d => new SubmittedDocument
                {
                    DocumentType = d.DocumentType.Trim().ToUpperInvariant(),
                    CertifiedOn = d.CertifiedOn,
                    ExpiresOn = d.ExpiresOn
                }).ToList()
            };

            await _clearanceRepository.AddSubmissionAsync(submission);
            await _verificationQueue.EnqueueAsync(submission.Id);

            return new SubmitResult(SubmitOutcome.Queued, submission);
        }

        public async Task VerifySubmissionAsync(int submissionId, CancellationToken cancellationToken)
        {
            var submission = await _clearanceRepository.GetSubmissionWithDocumentsAsync(submissionId);
            if (submission is null || submission.Status is SubmissionStatus.Cleared or SubmissionStatus.MissingDocuments)
                return;

            var appointment = await _clearanceRepository.GetAppointmentAsync(submission.AppointmentId);
            if (appointment is null)
                return;

            submission.Status = SubmissionStatus.Verifying;
            await _clearanceRepository.SaveChangesAsync();

            await _documentScanner.ScanAsync(submission, cancellationToken);

            var requirements = await _clearanceRepository.GetRequirementsAsync(appointment.ServiceType);
            var problems = FindProblems(requirements, submission.Documents, DateOnly.FromDateTime(appointment.StartsAt));

            submission.Status = problems.Count == 0 ? SubmissionStatus.Cleared : SubmissionStatus.MissingDocuments;
            submission.Problems = string.Join("|", problems);
            submission.VerifiedAt = DateTime.UtcNow;
            await _clearanceRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ClearanceStatus>> GetClearancesAsync(IReadOnlyCollection<int> appointmentIds, AccessContext reader)
        {
            var latest = (await _clearanceRepository.GetLatestSubmissionsAsync(appointmentIds))
                .ToDictionary(s => s.AppointmentId);

            var statuses = new List<ClearanceStatus>();
            var citizensRead = new HashSet<string>();
            foreach (var appointmentId in appointmentIds.Distinct())
            {
                var appointment = await _clearanceRepository.GetAppointmentAsync(appointmentId);
                if (appointment is not null)
                {
                    citizensRead.Add(appointment.CitizenId);
                }

                var requirements = appointment is null
                    ? Array.Empty<DocumentRequirement>()
                    : await _clearanceRepository.GetRequirementsAsync(appointment.ServiceType);

                statuses.Add(new ClearanceStatus(
                    appointmentId,
                    appointment is not null,
                    latest.GetValueOrDefault(appointmentId),
                    requirements));
            }

            if (citizensRead.Count > 0)
            {
                var now = DateTime.UtcNow;
                await _clearanceRepository.AddAccessLogEntriesAsync(citizensRead
                    .Select(citizenId => new AccessLogEntry
                    {
                        CitizenId = citizenId,
                        AccessedBy = reader.AccessedBy,
                        AccessedByRole = reader.AccessedByRole,
                        Reason = reader.Reason,
                        AccessedAt = now
                    })
                    .ToList());
            }

            return statuses;
        }

        public Task<IReadOnlyList<AccessLogEntry>> GetAccessLogAsync(string citizenId)
        {
            return _clearanceRepository.GetAccessLogAsync(citizenId?.Trim() ?? "");
        }

        public Task<IReadOnlyList<int>> GetUnfinishedSubmissionIdsAsync()
        {
            return _clearanceRepository.GetSubmissionIdsWithStatusAsync(SubmissionStatus.Queued, SubmissionStatus.Verifying);
        }

        public static IReadOnlyList<string> FindProblems(
            IReadOnlyList<DocumentRequirement> requirements,
            IReadOnlyList<SubmittedDocument> documents,
            DateOnly appointmentDate)
        {
            var problems = new List<string>();
            var oldestAcceptedCertification = appointmentDate.AddMonths(-MaxCertificationAgeMonths);

            foreach (var requirement in requirements)
            {
                var document = documents.FirstOrDefault(d => d.DocumentType == requirement.DocumentType);
                if (document is null)
                {
                    problems.Add($"{requirement.DocumentType} is missing.");
                    continue;
                }

                if (document.ExpiresOn is not null && document.ExpiresOn <= appointmentDate)
                {
                    problems.Add($"{requirement.DocumentType} expires on {document.ExpiresOn:yyyy-MM-dd}, before your appointment.");
                }

                if (requirement.MustBeCertified && document.CertifiedOn is null)
                {
                    problems.Add($"{requirement.DocumentType} must be a certified copy.");
                }
                else if (requirement.MustBeCertified && document.CertifiedOn < oldestAcceptedCertification)
                {
                    problems.Add($"{requirement.DocumentType} was certified on {document.CertifiedOn:yyyy-MM-dd}, more than {MaxCertificationAgeMonths} months before your appointment.");
                }
            }

            return problems;
        }
    }
}
