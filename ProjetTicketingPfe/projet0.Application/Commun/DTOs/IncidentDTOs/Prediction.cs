

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class DailyIncidentCountDTO
    {
        public DateTime Date { get; set; }
        public int TotalIncidents { get; set; }
        public int PaiementRefuse { get; set; }
        public int TerminalHorsLigne { get; set; }
        public int Lenteur { get; set; }
        public int BugAffichage { get; set; }
        public int ConnexionReseau { get; set; }
        public int ErreurFluxTransactionnel { get; set; }
        public int ProblemeLogicielTPE { get; set; }
        public int Autre { get; set; }
    }

    public class PredictionResultDTO
    {
        public DateTime Date { get; set; }
        public string Periode { get; set; } = string.Empty; // "Semaine" ou "Mois"
        public int TotalIncidents { get; set; }
        public Dictionary<string, int> IncidentsParType { get; set; } = new();
        public double ConfidenceLower { get; set; }
        public double ConfidenceUpper { get; set; }
    }

    public class IncidentPredictionResponseDTO
    {
        public List<PredictionResultDTO> PredictionSemaine { get; set; } = new();
        public List<PredictionResultDTO> PredictionMois { get; set; } = new();
        public DateTime DateGeneration { get; set; }
        public string Modele { get; set; } = "ML.NET (SSA Forecast)";
    }
}
