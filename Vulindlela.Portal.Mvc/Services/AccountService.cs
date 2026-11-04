using System.Globalization;
using Vulindlela.Portal.Mvc.Data.Repositories;
using Vulindlela.Portal.Mvc.Models;
using Vulindlela.Portal.Mvc.Security;

namespace Vulindlela.Portal.Mvc.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IPasswordHasher _passwordHasher;

        public AccountService(IAccountRepository accountRepository, IPasswordHasher passwordHasher)
        {
            _accountRepository = accountRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegisterOutcome> RegisterCitizenAsync(string citizenId, string displayName, string password)
        {
            var normalisedCitizenId = citizenId?.Trim() ?? "";
            if (!IsValidCitizenId(normalisedCitizenId))
            {
                return RegisterOutcome.InvalidCitizenId;
            }

            var created = await CreateAccountAsync(normalisedCitizenId, displayName, normalisedCitizenId, password);
            return created == CreateAccountOutcome.Created ? RegisterOutcome.Registered : RegisterOutcome.AlreadyRegistered;
        }

        public async Task<CreateAccountOutcome> CreateAccountAsync(string username, string displayName, string citizenId, string password)
        {
            var (hash, salt) = _passwordHasher.Hash(password);
            var account = new Account
            {
                Username = username.Trim(),
                DisplayName = displayName.Trim(),
                CitizenId = citizenId.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await _accountRepository.InsertAsync(account);
            return inserted == InsertAccountResult.Inserted ? CreateAccountOutcome.Created : CreateAccountOutcome.UsernameTaken;
        }

        public async Task<SignInResult> SignInAsync(string username, string password)
        {
            var account = await _accountRepository.GetByUsernameAsync(username?.Trim() ?? "");
            if (account is null || !_passwordHasher.Verify(password ?? "", account.PasswordHash, account.PasswordSalt))
            {
                return new SignInResult(false);
            }

            return new SignInResult(true, account);
        }

        public static bool IsValidCitizenId(string idNumber)
        {
            if (idNumber.Length != 13 || !idNumber.All(char.IsAsciiDigit))
            {
                return false;
            }

            if (!DateOnly.TryParseExact(idNumber[..6], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return false;
            }

            var sum = 0;
            for (var position = 0; position < idNumber.Length; position++)
            {
                var digit = idNumber[idNumber.Length - 1 - position] - '0';
                if (position % 2 == 1)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
            }

            return sum % 10 == 0;
        }
    }
}
