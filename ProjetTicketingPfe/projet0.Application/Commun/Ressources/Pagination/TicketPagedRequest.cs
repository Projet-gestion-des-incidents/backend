using projet0.Application.Common.Models.Pagination;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun.Ressources.Pagination
{
    public class TicketPagedRequest : PagedRequest
    {
        // Filtres optionnels pour les tickets
        public StatutTicket? Statut { get; set; }
        public PrioriteTicket? Priorite { get; set; }

        public DateTime? DateDebut { get; set; }
        
        public DateTime? DateFin { get; set; }
    }
}
