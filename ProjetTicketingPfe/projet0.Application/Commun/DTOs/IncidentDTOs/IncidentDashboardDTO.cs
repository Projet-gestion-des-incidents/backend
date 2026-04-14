using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class IncidentDashboardOverviewDTO
    {
        public int TotalIncidents { get; set; }
        public int IncidentsNonTraite { get; set; }
        public int IncidentsEnCours { get; set; }
        public int IncidentsFerme { get; set; }

        // Pourcentages
        public double TauxNonTraite { get; set; }
        public double TauxEnCours { get; set; }
        public double TauxFerme { get; set; }
    }

    // Statistiques par statut (pour graphique camembert)
    public class IncidentStatutStatDTO
    {
        public string Statut { get; set; }  // "Non traité", "En cours", "Fermé"
        public int Count { get; set; }
        public string Color { get; set; }   // Couleur pour le frontend
        public double Pourcentage { get; set; }
    }

    // Statistiques par jour
    public class IncidentJournalierDTO
    {
        public DateTime Date { get; set; }
        public string DateFormatee => Date.ToString("dd/MM/yyyy");
        public string Jour => Date.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));

        // Nombre d'incidents créés ce jour
        public int Crees { get; set; }

        // Répartition par statut pour ce jour
        public int NonTraite { get; set; }
        public int EnCours { get; set; }
        public int Ferme { get; set; }
    }

    // Dashboard complet
    public class IncidentDashboardDTO
    {
        public IncidentDashboardOverviewDTO Overview { get; set; }
        public List<IncidentStatutStatDTO> StatsParStatut { get; set; }
        public List<IncidentJournalierDTO> StatsParJour { get; set; }
        public List<IncidentJournalierDTO> StatsParSemaine { get; set; }
        public List<IncidentJournalierDTO> StatsParMois { get; set; }
    }
}
