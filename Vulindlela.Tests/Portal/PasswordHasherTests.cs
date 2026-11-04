using System.Text;
using Vulindlela.Portal.Mvc.Security;

namespace Vulindlela.Tests.Portal
{
    public class PasswordHasherTests
    {
        private readonly Pbkdf2PasswordHasher _hasher = new();

        [Fact]
        public void CorrectPassword_Verifies()
        {
            var (hash, salt) = _hasher.Hash("Correct-Horse-1");

            Assert.True(_hasher.Verify("Correct-Horse-1", hash, salt));
        }

        [Fact]
        public void WrongPassword_DoesNotVerify()
        {
            var (hash, salt) = _hasher.Hash("Correct-Horse-1");

            Assert.False(_hasher.Verify("Correct-Horse-2", hash, salt));
        }

        [Fact]
        public void SamePassword_GetsADifferentSaltAndHashEachTime()
        {
            var first = _hasher.Hash("Correct-Horse-1");
            var second = _hasher.Hash("Correct-Horse-1");

            Assert.NotEqual(first.Salt, second.Salt);
            Assert.NotEqual(first.Hash, second.Hash);
        }

        [Fact]
        public void StoredHash_DoesNotContainThePassword()
        {
            var (hash, _) = _hasher.Hash("Correct-Horse-1");

            Assert.DoesNotContain("Correct-Horse-1", Encoding.UTF8.GetString(hash));
            Assert.Equal(Pbkdf2PasswordHasher.HashSize, hash.Length);
        }
    }
}
