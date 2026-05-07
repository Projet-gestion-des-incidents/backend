using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(n => n.Ticket)
                .Include(n => n.Incident)
                .Include(n => n.Commentaire)
                .Where(n => n.DestinataireId == userId)
                .OrderByDescending(n => n.DateEnvoi)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(n => n.Ticket)
                .Include(n => n.Incident)
                .Include(n => n.Commentaire)
                .Where(n => n.DestinataireId == userId && !n.EstLu)
                .OrderByDescending(n => n.DateEnvoi)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _dbSet
                .Where(n => n.DestinataireId == userId && !n.EstLu)
                .CountAsync();
        }

        public async Task<IEnumerable<Notification>> GetByTypeAsync(Guid userId, TypeNotification type)
        {
            return await _dbSet
                .Include(n => n.Ticket)
                .Include(n => n.Incident)
                .Include(n => n.Commentaire)
                .Where(n => n.DestinataireId == userId && n.TypeNotification == type)
                .OrderByDescending(n => n.DateEnvoi)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetRecentByUserIdAsync(Guid userId, int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _dbSet
                .Include(n => n.Ticket)
                .Include(n => n.Incident)
                .Include(n => n.Commentaire)
                .Where(n => n.DestinataireId == userId && n.DateEnvoi >= cutoffDate)
                .OrderByDescending(n => n.DateEnvoi)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _dbSet.FindAsync(notificationId);
            if (notification != null && !notification.EstLu)
            {
                notification.EstLu = true;
                notification.DateLecture = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unreadNotifications = await _dbSet
                .Where(n => n.DestinataireId == userId && !n.EstLu)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.EstLu = true;
                notification.DateLecture = DateTime.UtcNow;
            }

            if (unreadNotifications.Any())
                await _context.SaveChangesAsync();
        }

        public async Task DeleteOldNotificationsAsync(int daysToKeep)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldNotifications = await _dbSet
                .Where(n => n.DateEnvoi < cutoffDate && n.EstLu)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                _dbSet.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Notification>> GetByTPEIdAsync(Guid tpeId)
        {
            return await _dbSet.Where(n => n.TPEId == tpeId).ToListAsync();
        }

        // NotificationRepository.cs - Ajouter ces méthodes
        public async Task<IEnumerable<Notification>> GetByIncidentIdAsync(Guid incidentId)
        {
            return await _dbSet
                .Where(n => n.IncidentId == incidentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByTicketIdAsync(Guid ticketId)
        {
            return await _dbSet
                .Where(n => n.TicketId == ticketId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByCommentaireIdAsync(Guid commentaireId)
        {
            return await _dbSet
                .Where(n => n.CommentaireId == commentaireId)
                .ToListAsync();
        }
    }
}
