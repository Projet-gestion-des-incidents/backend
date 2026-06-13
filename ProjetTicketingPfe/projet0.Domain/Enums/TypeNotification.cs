namespace projet0.Domain.Enums
{
    public enum TypeNotification
    {
        TicketCree = 1,
        TicketAssigne = 2,
        TicketModifie = 3,
        TicketCloture = 4,
        TicketEnCours = 11,        //  Ticket passé en cours
        IncidentCree = 5,
        IncidentResolu = 6,
        IncidentModifie = 9,
        IncidentEnCours = 12,      // Incident passé en cours
        CommentaireAjoute = 7,
        TPECree = 10,
        Rappel = 8
    }
}