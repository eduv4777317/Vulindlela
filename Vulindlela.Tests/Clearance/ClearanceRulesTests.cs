using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Models;
using Vulindlela.Clearance.Api.Services;
using Vulindlela.Tests.Booking;

namespace Vulindlela.Tests.Clearance
{
    public class ClearanceRulesTests
    {
        private static readonly DateTime AppointmentAt = new(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task AllDocumentsValid_IsCleared()
        {
            var submission = await SubmitAndVerifyAsync(
                new DocumentInput(DocumentTypes.IdCopy, new DateOnly(2026, 11, 1), null),
                new DocumentInput(DocumentTypes.PassportPhoto, null, null));

            Assert.Equal(SubmissionStatus.Cleared, submission.Status);
            Assert.Equal("", submission.Problems);
        }

        [Fact]
        public async Task MissingDocument_IsReportedByName()
        {
            var submission = await SubmitAndVerifyAsync(
                new DocumentInput(DocumentTypes.IdCopy, new DateOnly(2026, 11, 1), null));

            Assert.Equal(SubmissionStatus.MissingDocuments, submission.Status);
            Assert.Contains("PASSPORT_PHOTO is missing", submission.Problems);
        }

        [Fact]
        public async Task CopyCertifiedTooLongAgo_IsRejected()
        {
            var submission = await SubmitAndVerifyAsync(
                new DocumentInput(DocumentTypes.IdCopy, new DateOnly(2026, 6, 1), null),
                new DocumentInput(DocumentTypes.PassportPhoto, null, null));

            Assert.Equal(SubmissionStatus.MissingDocuments, submission.Status);
            Assert.Contains("more than 3 months", submission.Problems);
        }

        [Fact]
        public async Task UncertifiedCopy_IsRejected()
        {
            var submission = await SubmitAndVerifyAsync(
                new DocumentInput(DocumentTypes.IdCopy, null, null),
                new DocumentInput(DocumentTypes.PassportPhoto, null, null));

            Assert.Contains("must be a certified copy", submission.Problems);
        }

        [Fact]
        public async Task DocumentExpiringBeforeTheAppointment_IsRejected()
        {
            var submission = await SubmitAndVerifyAsync(
                new DocumentInput(DocumentTypes.IdCopy, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 20)),
                new DocumentInput(DocumentTypes.PassportPhoto, null, null));

            Assert.Contains("expires on 2026-11-20", submission.Problems);
        }

        [Fact]
        public async Task Submission_IsQueued_AndNotVerifiedOnTheRequest()
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();
            var service = database.CreateService(db);
            await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", AppointmentAt);

            var result = await service.SubmitDocumentsAsync(1, new[] { new DocumentInput(DocumentTypes.IdCopy, null, null) });

            Assert.Equal(SubmitOutcome.Queued, result.Outcome);
            Assert.Equal(SubmissionStatus.Queued, result.Submission!.Status);
            await using var queued = database.Queue.DequeueAllAsync(CancellationToken.None).GetAsyncEnumerator();
            Assert.True(await queued.MoveNextAsync());
            Assert.Equal(result.Submission.Id, queued.Current);
        }

        [Fact]
        public async Task Submission_ForUnknownAppointment_IsRejected()
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();

            var result = await database.CreateService(db).SubmitDocumentsAsync(999, Array.Empty<DocumentInput>());

            Assert.Equal(SubmitOutcome.AppointmentNotFound, result.Outcome);
        }

        [Fact]
        public async Task RegisteringTheSameAppointmentTwice_KeepsOneRecord()
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();
            var service = database.CreateService(db);

            var first = await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", AppointmentAt);
            var second = await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", AppointmentAt);

            Assert.True(first);
            Assert.False(second);
            Assert.Equal(1, await db.Appointments.CountAsync());
        }

        private static async Task<Submission> SubmitAndVerifyAsync(params DocumentInput[] documents)
        {
            using var database = new ClearanceTestDatabase();
            using var db = database.CreateContext();
            var service = database.CreateService(db);
            await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", AppointmentAt);
            var submitted = await service.SubmitDocumentsAsync(1, documents);

            await service.VerifySubmissionAsync(submitted.Submission!.Id, CancellationToken.None);

            using var check = database.CreateContext();
            return (await check.Submissions.FindAsync(submitted.Submission.Id))!;
        }
    }
}
