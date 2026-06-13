using projet0.Domain.Enums;

namespace projet0.Domain.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public string ReferenceTicket { get; set; }  
        public string TitreTicket { get; set; }
        public string DescriptionTicket { get; set; }

        // Enums
        public StatutTicket? StatutTicket { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateCloture { get; set; }
        public DateTime DateLimite { get; set; }


        // Foreign Keys
        public Guid CreateurId { get; set; }     // Qui a créé le ticket
        public Guid? AssigneeId { get; set; }     // À qui est assigné

        // Navigation Properties
        public virtual ICollection<IncidentTicket> IncidentTickets { get; set; }
        public virtual ICollection<HistoriqueTicket> Historiques { get; set; }
        public virtual ICollection<CommentaireTicket> Commentaires { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<TicketArchive> TicketArchives { get; set; }
        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual ApplicationUser Createur { get; set; } 
        public virtual ApplicationUser Assignee { get; set; }
    }
}
