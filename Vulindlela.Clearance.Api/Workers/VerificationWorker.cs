using Vulindlela.Clearance.Api.Queue;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Workers
{
    public class VerificationWorker : BackgroundService
    {
        private readonly IVerificationQueue _verificationQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VerificationWorker> _logger;

        public VerificationWorker(IVerificationQueue verificationQueue, IServiceScopeFactory scopeFactory, ILogger<VerificationWorker> logger)
        {
            _verificationQueue = verificationQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await RequeueUnfinishedAsync(cancellationToken);

            await foreach (var submissionId in _verificationQueue.DequeueAllAsync(cancellationToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    IClearanceService clearanceService = scope.ServiceProvider.GetRequiredService<IClearanceService>();
                    await clearanceService.VerifySubmissionAsync(submissionId, cancellationToken);
                    _logger.LogInformation("Verified submission {SubmissionId}.", submissionId);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Verifying submission {submissionId} failed.");
                }
            }
        }

        private async Task RequeueUnfinishedAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            IClearanceService clearanceService = scope.ServiceProvider.GetRequiredService<IClearanceService>();
            foreach (var submissionId in await clearanceService.GetUnfinishedSubmissionIdsAsync())
            {
                await _verificationQueue.EnqueueAsync(submissionId, stoppingToken);
            }
        }
    }
}
