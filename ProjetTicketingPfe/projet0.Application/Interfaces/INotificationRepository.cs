using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>

    {
        Task DeleteByTicketAndUserAsync(Guid ticketId, Guid userId);
        Task DeleteByTicketForAllExceptUserAsync(Guid ticketId, Guid userIdToKeep);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<IEnumerable<Notification>> GetByTypeAsync(Guid userId, TypeNotification type);
        Task<IEnumerable<Notification>> GetRecentByUserIdAsync(Guid userId, int days);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
        Task DeleteOldNotificationsAsync(int daysToKeep);
        Task<IEnumerable<Notification>> GetByTPEIdAsync(Guid tpeId);
        Task<IEnumerable<Notification>> GetByCommentaireIdAsync(Guid commentaireId);
        Task<IEnumerable<Notification>> GetByTicketIdAsync(Guid ticketId);
        Task<IEnumerable<Notification>> GetByIncidentIdAsync(Guid incidentId);
        Task MarkAllAsConsultedAsync(Guid userId);
    }
}
