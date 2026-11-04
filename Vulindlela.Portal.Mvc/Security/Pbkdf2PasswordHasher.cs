using System.Security.Cryptography;

namespace Vulindlela.Portal.Mvc.Security
{
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        public const int SaltSize = 16;
        public const int HashSize = 32;
        public const int Iterations = 210_000;

        public (byte[] Hash, byte[] Salt) Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            return (Derive(password, salt), salt);
        }

        public bool Verify(string password, byte[] hash, byte[] salt)
        {
            return CryptographicOperations.FixedTimeEquals(Derive(password, salt), hash);
        }

        private static byte[] Derive(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
    }
}
