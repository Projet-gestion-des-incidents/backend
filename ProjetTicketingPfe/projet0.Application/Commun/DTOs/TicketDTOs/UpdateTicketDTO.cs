using projet0.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class UpdateTicketDTO
    {
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Le titre doit contenir entre 3 et 200 caractères")]
        public string? TitreTicket { get; set; }

        [StringLength(2000, ErrorMessage = "La description ne peut pas dépasser 2000 caractères")]
        public string? DescriptionTicket { get; set; }

        public StatutTicket? StatutTicket { get; set; }

        public Guid? AssigneeId { get; set; }

        // Propriété calculée pour savoir si AssigneeId a été explicitement fourni
        public bool IsAssigneeIdSpecified => AssigneeId.HasValue;
        public DateTime DateLimite { get; set; }

    }
}