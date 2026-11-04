using System.Threading.Channels;

namespace Vulindlela.Clearance.Api.Queue
{
    public class ChannelVerificationQueue : IVerificationQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        public ValueTask EnqueueAsync(int submissionId, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(submissionId, cancellationToken);
        }

        public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
