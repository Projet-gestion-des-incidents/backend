namespace projet0.Application.Commun.DTOs.Ticket
{
    public class TicketDetailDTO : TicketDTO
    {
        public List<CommentaireDTO> Commentaires { get; set; } = new();
    }
}
