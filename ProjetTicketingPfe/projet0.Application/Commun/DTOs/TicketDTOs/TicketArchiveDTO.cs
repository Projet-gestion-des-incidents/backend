using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class TicketArchiveDTO
    {
        public Guid TicketId { get; set; }
        public string ReferenceTicket { get; set; }
        public bool EstArchive { get; set; }
        public DateTime? DateArchivage { get; set; }
        public string ArchivePar { get; set; }
    }
}
