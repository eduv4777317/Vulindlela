using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Vulindlela.Clearance.Api.Data;
using Vulindlela.Clearance.Api.Data.Repositories;
using Vulindlela.Clearance.Api.Models;
using Vulindlela.Clearance.Api.Queue;
using Vulindlela.Clearance.Api.Services;
using Vulindlela.Clearance.Api.Workers;
using Vulindlela.Tests.Booking;

namespace Vulindlela.Tests.Clearance
{
    public class VerificationWorkerTests
    {
        [Fact]
        public async Task Worker_TakesSubmissionOffTheQueue_AndRecordsTheOutcome()
        {
            using var database = new ClearanceTestDatabase();
            using (var db = database.CreateContext())
            {
                var service = database.CreateService(db);
                await service.RegisterAppointmentAsync(1, TestCitizens.Thandi, "PASSPORT", DateTime.UtcNow.AddDays(7));
                await service.SubmitDocumentsAsync(1, new[] { new DocumentInput(DocumentTypes.PassportPhoto, null, null) });
            }

            var services = new ServiceCollection();
            services.AddScoped(_ => database.CreateContext());
            services.AddScoped<IClearanceRepository, ClearanceRepository>();
            services.AddScoped<IClearanceService, ClearanceService>();
            services.AddSingleton<IVerificationQueue>(database.Queue);
            services.AddSingleton<IDocumentScanner>(new SimulatedDocumentScanner(TimeSpan.Zero));
            await using var provider = services.BuildServiceProvider();

            var worker = new VerificationWorker(
                database.Queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<VerificationWorker>.Instance);
            await worker.StartAsync(CancellationToken.None);

            var status = SubmissionStatus.Queued;
            for (var attempt = 0; attempt < 50 && status is SubmissionStatus.Queued or SubmissionStatus.Verifying; attempt++)
            {
                await Task.Delay(100);
                using var check = database.CreateContext();
                status = (await check.Submissions.SingleAsync()).Status;
            }

            await worker.StopAsync(CancellationToken.None);

            Assert.Equal(SubmissionStatus.MissingDocuments, status);
        }
    }
}
