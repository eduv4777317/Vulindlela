using Microsoft.EntityFrameworkCore;
using Vulindlela.Portal.Mvc.Services;

namespace Vulindlela.Portal.Mvc.Data.Seedings
{
    public class PortalSeeder : IPortalSeeder
    {
        private readonly PortalDBContext _portalDBContext;
        private readonly IAccountService _accountService;
        private readonly IConfiguration _configuration;

        public PortalSeeder(PortalDBContext portalDBContext, IAccountService accountService, IConfiguration configuration)
        {
            _portalDBContext = portalDBContext;
            _accountService = accountService;
            _configuration = configuration;
        }

        public async Task SeedAsync()
        {
            if (await _portalDBContext.Accounts.AnyAsync())
            {
                return;
            }

            var accounts = _configuration.GetSection("SeedAccounts").Get<List<SeedAccount>>() ?? new List<SeedAccount>();
            foreach (var seed in accounts)
            {
                await _accountService.CreateAccountAsync(seed.Username, seed.DisplayName, seed.CitizenId, seed.Role, seed.Password);
            }
        }
    }
}
