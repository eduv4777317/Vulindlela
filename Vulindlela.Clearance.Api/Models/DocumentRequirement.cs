namespace Vulindlela.Clearance.Api.Models
{
    public class DocumentRequirement
    {
        public int Id { get; set; }
        public string ServiceType { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public bool MustBeCertified { get; set; }
    }
}
