namespace projet0.Application.Commun.DTOs
{

    // Statistiques globales
    public class TPEDashboardOverviewDTO
    {
        public int TotalTPEs { get; set; }
        public int TotalIncidentsLiees { get; set; }
        public double TauxGlobalPanne { get; set; }  // (TotalIncidents / TotalTPEs) × 100
    }

    // Taux de panne par modèle
    public class TPEPanneParModeleDTO
    {
        public string Modele { get; set; }           // "Ingenico", "Verifone", "PAX"
        public int NombreTPEs { get; set; }          // Nombre total de TPEs de ce modèle
        public int NombreIncidents { get; set; }     // Nombre d'incidents liés à ce modèle
        public double TauxPanne { get; set; }        // (NombreIncidents / NombreTPEs) × 100
        public string Color { get; set; }            // Pour graphique
    }

    // Taux de panne par adresse (via commerçant)
    public class TPEPanneParAdresseDTO
    {
        public Guid CommercantId { get; set; }
        public string CommercantNom { get; set; }     // Nom du commerçant
        public string Adresse { get; set; }           // Adresse du commerçant
        public int NombreTPEs { get; set; }           // Nombre de TPEs chez ce commerçant
        public int NombreIncidents { get; set; }      // Nombre d'incidents pour ses TPEs
        public double TauxPanne { get; set; }         // (NombreIncidents / NombreTPEs) × 100
        public double PourcentageTPEsTotal { get; set; } // Part des TPEs de ce commerçant dans le total
    }

    public class TPEDashboardDTO
    {
        public TPEDashboardOverviewDTO Overview { get; set; }
        public List<TPEPanneParModeleDTO> PannesParModele { get; set; }
        public List<TPEPanneParAdresseDTO> PannesParAdresse { get; set; }
    }
}
