using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Services
{
    public interface IDocumentScanner
    {
        Task ScanAsync(Submission submission, CancellationToken cancellationToken);
    }
}
