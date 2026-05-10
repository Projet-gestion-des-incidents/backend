using projet0.Application.Commun.DTOs;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace projet0.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> GetByIdAsync(Guid id);
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task CreateTPENotificationAsync(Guid userId, Guid tpeId, TypeNotification type, string titre, string message);
        Task<IEnumerable<NotificationDto>> GetByTypeAsync(Guid userId, TypeNotification type);
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createDto);
        Task CreateTicketNotificationAsync(Guid userId, Guid ticketId, TypeNotification type, string titre, string message);
        Task CreateIncidentNotificationAsync(Guid userId, Guid incidentId, TypeNotification type, string titre, string message);
        Task CreateCommentNotificationAsync(Guid userId, Guid commentId, Guid? ticketId, string titre, string message);
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
        Task MarkAllAsConsultedAsync(Guid userId);
        Task DeleteNotificationAsync(Guid id, Guid userId);
        Task DeleteAllUserNotificationsAsync(Guid userId);
    }
}