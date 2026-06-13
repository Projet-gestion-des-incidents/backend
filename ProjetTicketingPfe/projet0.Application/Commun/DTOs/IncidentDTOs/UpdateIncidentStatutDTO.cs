using projet0.Domain.Enums;


namespace projet0.Application.Commun.DTOs.Incident
{
    public class UpdateIncidentStatutDTO
    {
        public StatutIncident StatutIncident { get; set; }
        public DateTime? DateResolution { get; set; }
    }
}
