namespace Vulindlela.Portal.Mvc.Security
{
    public interface IPasswordHasher
    {
        (byte[] Hash, byte[] Salt) Hash(string password);
        bool Verify(string password, byte[] hash, byte[] salt);
    }
}
