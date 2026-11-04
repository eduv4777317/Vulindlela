namespace Vulindlela.Clearance.Api.Queue
{
    public interface IVerificationQueue
    {
        ValueTask EnqueueAsync(int submissionId, CancellationToken cancellationToken = default);

        IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
