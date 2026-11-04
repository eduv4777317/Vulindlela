namespace Vulindlela.Booking.Api.Services
{
    public static class ServiceTypes
    {
        public const string IdCollection = "ID_COLLECTION";
        public const string IdApplication = "ID_APPLICATION";
        public const string Passport = "PASSPORT";
        public const string PassportUrgent = "PASSPORT_URGENT";
        public const string BirthCertificate = "BIRTH_CERT";
        public const string MarriageCertificate = "MARRIAGE_CERT";
        public const string DeathCertificate = "DEATH_CERT";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            IdCollection,
            IdApplication,
            Passport,
            PassportUrgent,
            BirthCertificate,
            MarriageCertificate,
            DeathCertificate
        };
    }
}
