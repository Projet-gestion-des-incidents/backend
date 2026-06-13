using Microsoft.AspNetCore.Http;
using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class CreateIncidentDTO
    {
        public string? DescriptionIncident { get; set; }
        public TypeProbleme TypeProbleme { get; set; }
        public string? Emplacement { get; set; }
        public List<Guid> TPEIds { get; set; }
        public IFormFileCollection? PiecesJointes { get; set; }

    }
}
