namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class LiaisonTicketsResultDTO
    {
        public int TicketsLies { get; set; }
        public int TicketsDejaLies { get; set; }
        public int TicketsNonTrouves { get; set; }
        public int TotalTicketsTraites { get; set; }
        public List<string> Details { get; set; } = new();
    }
}
