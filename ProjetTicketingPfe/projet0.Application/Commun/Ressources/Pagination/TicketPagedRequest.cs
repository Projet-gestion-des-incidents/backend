using projet0.Application.Common.Models.Pagination;
using projet0.Domain.Enums;

namespace projet0.Application.Commun.Ressources.Pagination
{
    public class TicketPagedRequest : PagedRequest
    {
        // Filtres optionnels pour les tickets
        public StatutTicket? Statut { get; set; }

        public DateTime? DateDebut { get; set; }
        public bool? NonAssigne { get; set; }

        public DateTime? DateFin { get; set; }

        // Filtre par statut de date limite
        public DateLimiteStatut? DateLimiteStatut { get; set; }
    }

    public enum DateLimiteStatut
    {
        Expire = 1,           // Date limite dépassée et ticket non résolu
        RealiseAvantDelai = 2, // Résolu avant la date limite
        RealiseApresDelai = 3, // Résolu après la date limite
        JoursRestants = 4      // Non résolu mais date limite dans le futur
    }
}

