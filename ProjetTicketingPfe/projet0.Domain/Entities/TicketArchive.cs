using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class TicketArchive
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public Guid ArchiveParId { get; set; }
        public DateTime DateArchivage { get; set; }
        public string? Commentaire { get; set; }

        // Navigation
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser ArchivePar { get; set; }
    }
}
