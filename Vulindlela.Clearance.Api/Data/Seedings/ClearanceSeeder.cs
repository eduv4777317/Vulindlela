using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Models;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Data.Seedings
{
    public class ClearanceSeeder : IClearanceSeeder
    {
        private readonly ClearanceDBContext _clearanceDBContext;

        public ClearanceSeeder(ClearanceDBContext clearanceDBContext)
        {
            _clearanceDBContext = clearanceDBContext;
        }

        public async Task SeedAsync()
        {
            if (await _clearanceDBContext.Requirements.AnyAsync())
            {
                return;
            }

            _clearanceDBContext.Requirements.AddRange(
                Requirement("ID_COLLECTION", DocumentTypes.ApplicationReceipt, false),
                Requirement("ID_APPLICATION", DocumentTypes.BirthCertificate, true),
                Requirement("ID_APPLICATION", DocumentTypes.IdPhoto, false),
                Requirement("PASSPORT", DocumentTypes.IdCopy, true),
                Requirement("PASSPORT", DocumentTypes.PassportPhoto, false),
                Requirement("PASSPORT_URGENT", DocumentTypes.IdCopy, true),
                Requirement("PASSPORT_URGENT", DocumentTypes.PassportPhoto, false),
                Requirement("BIRTH_CERT", DocumentTypes.ParentIdCopy, true),
                Requirement("BIRTH_CERT", DocumentTypes.ProofOfBirth, false),
                Requirement("MARRIAGE_CERT", DocumentTypes.IdCopy, true),
                Requirement("MARRIAGE_CERT", DocumentTypes.MarriageRegister, false),
                Requirement("DEATH_CERT", DocumentTypes.IdCopy, true),
                Requirement("DEATH_CERT", DocumentTypes.DeathNotification, false));

            await _clearanceDBContext.SaveChangesAsync();
        }

        private static DocumentRequirement Requirement(string serviceType, string documentType, bool mustBeCertified) =>
            new() { ServiceType = serviceType, DocumentType = documentType, MustBeCertified = mustBeCertified };
    }
}
