using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class AjouterPiecesJointesIncidentDTO
    {
        public Guid IncidentId { get; set; }
        public List<IFormFile> Fichiers { get; set; }
    }
}
