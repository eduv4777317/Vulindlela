using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Services
{
    public class SimulatedDocumentScanner : IDocumentScanner
    {
        private readonly TimeSpan _delay;

        public SimulatedDocumentScanner(TimeSpan delay)
        {
            _delay = delay;
        }

        public Task ScanAsync(Submission submission, CancellationToken cancellationToken)
        {
            return Task.Delay(_delay, cancellationToken);
        }
    }
}
