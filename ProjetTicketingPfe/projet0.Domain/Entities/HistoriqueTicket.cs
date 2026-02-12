using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class HistoriqueTicket
    {
        public Guid Id { get; set; }

        // Statuts
        public StatutTicket AncienStatut { get; set; }
        public StatutTicket NouveauStatut { get; set; }
        public DateTime DateChangement { get; set; }

        // Foreign Keys
        public Guid TicketId { get; set; }
        public Guid? ModifieParId { get; set; }  // Utilisateur qui a fait le changement

        // Navigation Properties
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser ModifiePar { get; set; }
    }
}
