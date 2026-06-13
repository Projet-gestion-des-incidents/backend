using projet0.Domain.Enums;


namespace projet0.Application.Commun.DTOs.Incident
{
    public class UpdateIncidentDTO
    {        
        public string? DescriptionIncident { get; set; }
        public string? Emplacement { get; set; }
        public TypeProbleme? TypeProbleme { get; set; }
        public SeveriteIncident? SeveriteIncident { get; set; }

    }
}
