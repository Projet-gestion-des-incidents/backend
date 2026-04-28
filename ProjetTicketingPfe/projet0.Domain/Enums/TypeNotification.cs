using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Enums
{
    public enum TypeNotification
    {
        TicketCree = 1,
        TicketAssigne = 2,
        TicketModifie = 3,
        TicketCloture = 4,
        TicketEnCours = 11,        // ✅ NOUVEAU : Ticket passé en cours
        IncidentCree = 5,
        IncidentResolu = 6,
        IncidentModifie = 9,
        IncidentEnCours = 12,      // ✅ NOUVEAU : Incident passé en cours
        CommentaireAjoute = 7,
        TPECree = 10,
        Rappel = 8
    }
}