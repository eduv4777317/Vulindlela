using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Data.Repositories
{
    public enum InsertAccountResult
    {
        Inserted,
        UsernameTaken
    }

    public interface IAccountRepository
    {
        Task<Account?> GetByUsernameAsync(string username);
        Task<InsertAccountResult> InsertAsync(Account account);
    }
}
