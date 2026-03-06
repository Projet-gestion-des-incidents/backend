using Microsoft.AspNetCore.Http;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class CreateIncidentDTO
    {
        public string? DescriptionIncident { get; set; }
        public TypeProbleme TypeProbleme { get; set; }
        public string Emplacement { get; set; }
        public List<Guid> TPEIds { get; set; }
        public IFormFileCollection? PiecesJointes { get; set; }


    }
}
