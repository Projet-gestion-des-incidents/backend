using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public string ReferenceTicket { get; set; }  // Format: TCK-2026-001
        public string TitreTicket { get; set; }
        public string DescriptionTicket { get; set; }

        // Enums
        public StatutTicket StatutTicket { get; set; }
        public PrioriteTicket PrioriteTicket { get; set; }

        public DateTime DateCreation { get; set; }
        public DateTime? DateCloture { get; set; }

        // Foreign Keys
        public Guid CreateurId { get; set; }     // Qui a créé le ticket
        public Guid? AssigneeId { get; set; }     // À qui est assigné

        // Navigation Properties
        public virtual ApplicationUser Createur { get; set; }
        public virtual ApplicationUser Assignee { get; set; }
        public virtual ICollection<IncidentTicket> IncidentTickets { get; set; }
        public virtual ICollection<HistoriqueTicket> Historiques { get; set; }
        public virtual ICollection<CommentaireTicket> Commentaires { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
