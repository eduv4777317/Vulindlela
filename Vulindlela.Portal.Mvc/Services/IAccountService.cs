namespace Vulindlela.Portal.Mvc.Services
{
    public interface IAccountService
    {
        Task<RegisterOutcome> RegisterCitizenAsync(string citizenId, string displayName, string password);
        Task<CreateAccountOutcome> CreateAccountAsync(string username, string displayName, string? citizenId, string role, string password);
        Task<SignInResult> SignInAsync(string username, string password);
    }
}
