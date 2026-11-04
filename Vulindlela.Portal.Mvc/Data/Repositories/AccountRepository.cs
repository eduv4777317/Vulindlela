using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Data.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private const int SqliteConstraint = 19;
        private const int SqliteConstraintUnique = 2067;

        private readonly PortalDBContext _portalDBContext;

        public AccountRepository(PortalDBContext portalDBContext)
        {
            _portalDBContext = portalDBContext;
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            return await _portalDBContext.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username == username);
        }

        public async Task<InsertAccountResult> InsertAsync(Account account)
        {
            _portalDBContext.Accounts.Add(account);

            try
            {
                await _portalDBContext.SaveChangesAsync();
                return InsertAccountResult.Inserted;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException
            {
                SqliteErrorCode: SqliteConstraint,
                SqliteExtendedErrorCode: SqliteConstraintUnique
            })
            {
                _portalDBContext.Entry(account).State = EntityState.Detached;
                return InsertAccountResult.UsernameTaken;
            }
        }
    }
}
