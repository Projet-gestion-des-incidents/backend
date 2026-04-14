using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    // Statistiques globales des tickets
    public class TicketDashboardOverviewDTO
    {
        public int TotalTickets { get; set; }
        public int TicketsNonAssigne { get; set; }
        public int TicketsAssignes { get; set; }
        public int TicketsEnCours { get; set; }
        public int TicketsResolus { get; set; }

        // Pourcentages
        public double TauxNonAssigne { get; set; }
        public double TauxAssignes { get; set; }
        public double TauxEnCours { get; set; }
        public double TauxResolus { get; set; }

        // Taux de résolution global
        public double TauxResolutionGlobal { get; set; }  // (Résolus / Total) * 100
    }

    // Statistiques par statut (pour graphique camembert)
    public class TicketStatutStatDTO
    {
        public string Statut { get; set; }  // "Non assigné", "Assigné", "En cours", "Résolu"
        public int Count { get; set; }
        public string Color { get; set; }
        public double Pourcentage { get; set; }
    }

    // Statistiques par jour
    public class TicketJournalierDTO
    {
        public DateTime Date { get; set; }
        public string DateFormatee => Date.ToString("dd/MM/yyyy");
        public string Jour => Date.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));

        public int Crees { get; set; }
        public int NonAssigne { get; set; }
        public int Assignes { get; set; }
        public int EnCours { get; set; }
        public int Resolus { get; set; }
    }

    // Dashboard complet des tickets
    public class TicketDashboardDTO
    {
        public TicketDashboardOverviewDTO Overview { get; set; }
        public List<TicketStatutStatDTO> StatsParStatut { get; set; }
        public List<TicketJournalierDTO> StatsParJour { get; set; }
        public List<TicketJournalierDTO> StatsParSemaine { get; set; }
        public List<TicketJournalierDTO> StatsParMois { get; set; }

        // Top techniciens
        public List<TopTechnicienDTO> TopTechniciens { get; set; }
    }

    // Top techniciens
    public class TopTechnicienDTO
    {
        public Guid TechnicienId { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string NomComplet => $"{Nom} {Prenom}";
        public int TicketsResolus { get; set; }
        public int TicketsEnCours { get; set; }
        public double TauxResolution { get; set; }
    }
}
