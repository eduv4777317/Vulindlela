using System.Text;
using Vulindlela.Portal.Mvc.Security;

namespace Vulindlela.Tests.Portal
{
    public class PasswordHasherTests
    {
        private readonly Pbkdf2PasswordHasher _hasher = new();
        private readonly string _password = Guid.NewGuid().ToString();
        private readonly string _otherPassword = Guid.NewGuid().ToString();

        [Fact]
        public void CorrectPassword_Verifies()
        {
            var (hash, salt) = _hasher.Hash(_password);

            Assert.True(_hasher.Verify(_password, hash, salt));
        }

        [Fact]
        public void WrongPassword_DoesNotVerify()
        {
            var (hash, salt) = _hasher.Hash(_password);

            Assert.False(_hasher.Verify(_otherPassword, hash, salt));
        }

        [Fact]
        public void SamePassword_GetsADifferentSaltAndHashEachTime()
        {
            var first = _hasher.Hash(_password);
            var second = _hasher.Hash(_password);

            Assert.NotEqual(first.Salt, second.Salt);
            Assert.NotEqual(first.Hash, second.Hash);
        }

        [Fact]
        public void StoredHash_DoesNotContainThePassword()
        {
            var (hash, _) = _hasher.Hash(_password);

            Assert.DoesNotContain(_password, Encoding.UTF8.GetString(hash));
            Assert.Equal(Pbkdf2PasswordHasher.HashSize, hash.Length);
        }
    }
}
