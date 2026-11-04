using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Portal.Mvc.Data;
using Vulindlela.Portal.Mvc.Data.Repositories;
using Vulindlela.Portal.Mvc.Security;
using Vulindlela.Portal.Mvc.Services;
using Vulindlela.Tests.Booking;

namespace Vulindlela.Tests.Portal
{
    public sealed class AccountServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly PortalDBContext _db;
        private readonly AccountService _service;

        public AccountServiceTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _db = new PortalDBContext(new DbContextOptionsBuilder<PortalDBContext>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();
            _service = new AccountService(new AccountRepository(_db), new Pbkdf2PasswordHasher());
        }

        [Fact]
        public async Task CorrectPassword_SignsIn()
        {
            await _service.CreateAccountAsync(TestCitizens.Thandi, "Thandi", TestCitizens.Thandi, "Thandi-Demo-2026");

            var signIn = await _service.SignInAsync(TestCitizens.Thandi, "Thandi-Demo-2026");

            Assert.True(signIn.Succeeded);
            Assert.Equal(TestCitizens.Thandi, signIn.Account!.CitizenId);
        }

        [Fact]
        public async Task WrongPassword_IsRefused()
        {
            await _service.CreateAccountAsync(TestCitizens.Thandi, "Thandi", TestCitizens.Thandi, "Thandi-Demo-2026");

            var signIn = await _service.SignInAsync(TestCitizens.Thandi, "Thandi-Demo-2027");

            Assert.False(signIn.Succeeded);
        }

        [Fact]
        public async Task UnknownUser_IsRefused()
        {
            var signIn = await _service.SignInAsync(TestCitizens.Sipho, "Sipho-Demo-2026");

            Assert.False(signIn.Succeeded);
        }

        [Fact]
        public async Task PasswordIsNotStoredInPlainText()
        {
            await _service.CreateAccountAsync(TestCitizens.Thandi, "Thandi", TestCitizens.Thandi, "Thandi-Demo-2026");

            var account = await _db.Accounts.SingleAsync();

            Assert.NotEmpty(account.PasswordSalt);
            Assert.DoesNotContain("Thandi-Demo-2026", Convert.ToBase64String(account.PasswordHash));
            Assert.DoesNotContain("Thandi-Demo-2026", System.Text.Encoding.UTF8.GetString(account.PasswordHash));
        }

        [Fact]
        public async Task TwoAccountsWithTheSamePassword_GetDifferentSaltsAndHashes()
        {
            await _service.CreateAccountAsync(TestCitizens.Thandi, "Thandi", TestCitizens.Thandi, "Same-Password-1");
            await _service.CreateAccountAsync(TestCitizens.Sipho, "Sipho", TestCitizens.Sipho, "Same-Password-1");

            var accounts = await _db.Accounts.ToListAsync();

            Assert.NotEqual(accounts[0].PasswordSalt, accounts[1].PasswordSalt);
            Assert.NotEqual(accounts[0].PasswordHash, accounts[1].PasswordHash);
        }

        [Fact]
        public async Task RegisteredCitizen_CanSignIn()
        {
            var outcome = await _service.RegisterCitizenAsync(TestCitizens.Thandi, "Thandi", "Thandi-Demo-2026");
            var signIn = await _service.SignInAsync(TestCitizens.Thandi, "Thandi-Demo-2026");

            Assert.Equal(RegisterOutcome.Registered, outcome);
            Assert.True(signIn.Succeeded);
        }

        [Fact]
        public async Task SameIdNumber_CannotRegisterTwice()
        {
            await _service.RegisterCitizenAsync(TestCitizens.Thandi, "Thandi", "Thandi-Demo-2026");

            var second = await _service.RegisterCitizenAsync(TestCitizens.Thandi, "Someone else", "Other-Password-1");

            Assert.Equal(RegisterOutcome.AlreadyRegistered, second);
            Assert.True((await _service.SignInAsync(TestCitizens.Thandi, "Thandi-Demo-2026")).Succeeded);
        }

        [Theory]
        [InlineData("9001015009087")]
        [InlineData("12345")]
        public async Task InvalidIdNumber_CannotRegister(string citizenId)
        {
            var outcome = await _service.RegisterCitizenAsync(citizenId, "Thandi", "Thandi-Demo-2026");

            Assert.Equal(RegisterOutcome.InvalidCitizenId, outcome);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
