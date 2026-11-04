using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Services
{
    public enum CreateAccountOutcome
    {
        Created,
        UsernameTaken
    }

    public enum RegisterOutcome
    {
        Registered,
        InvalidCitizenId,
        AlreadyRegistered
    }

    public sealed record SignInResult(bool Succeeded, Account? Account = null);
}
