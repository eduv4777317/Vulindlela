namespace Vulindlela.Portal.Mvc.Data.Seedings
{
    public sealed class SeedAccount
    {
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? CitizenId { get; set; }
        public string Role { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
