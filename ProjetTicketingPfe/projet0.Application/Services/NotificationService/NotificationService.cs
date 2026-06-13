using AutoMapper;
using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;


        public NotificationService(INotificationRepository notificationRepository, IMapper mapper, ILogger<NotificationService> logger)

        {
            _notificationRepository = notificationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NotificationDto> GetByIdAsync(Guid id)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task<IEnumerable<NotificationDto>> GetByTypeAsync(Guid userId, TypeNotification type)
        {
            var notifications = await _notificationRepository.GetByTypeAsync(userId, type);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createDto)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                DestinataireId = createDto.DestinataireId,
                TypeNotification = createDto.TypeNotification,
                Titre = createDto.Titre,
                Message = createDto.Message,
                DateEnvoi = DateTime.UtcNow,
                EstLu = false,
                TicketId = createDto.TicketId,
                IncidentId = createDto.IncidentId,
                CommentaireId = createDto.CommentaireId
            };

            await _notificationRepository.AddAsync(notification);
            return _mapper.Map<NotificationDto>(notification);
        }
        public async Task CreateTPENotificationAsync(Guid userId, Guid tpeId, TypeNotification type, string titre, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                DestinataireId = userId,
                TypeNotification = type,
                Titre = titre,
                Message = message,
                DateEnvoi = DateTime.UtcNow,
                EstLu = false,
                TPEId = tpeId  
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
        }
        public async Task CreateTicketNotificationAsync(Guid userId, Guid ticketId, TypeNotification type, string titre, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                DestinataireId = userId,
                TypeNotification = type,
                Titre = titre,
                Message = message,
                DateEnvoi = DateTime.UtcNow,
                EstLu = false,
                TicketId = ticketId
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

        }

        public async Task CreateIncidentNotificationAsync(Guid userId, Guid incidentId, TypeNotification type, string titre, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                DestinataireId = userId,
                TypeNotification = type,
                Titre = titre,
                Message = message,
                DateEnvoi = DateTime.UtcNow,
                EstLu = false,
                IncidentId = incidentId
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();  
        }
        public async Task CreateCommentNotificationAsync(Guid userId, Guid commentId, Guid? ticketId, string titre, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                DestinataireId = userId,
                TypeNotification = TypeNotification.CommentaireAjoute,
                Titre = titre,
                Message = message,
                DateEnvoi = DateTime.UtcNow,
                EstLu = false,
                TicketId = ticketId,
                CommentaireId = commentId
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();  

        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null && notification.DestinataireId == userId)
            {
                await _notificationRepository.MarkAsReadAsync(notificationId);
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task DeleteNotificationAsync(Guid id, Guid userId)
        {
            _logger.LogInformation($"DeleteNotificationAsync - Tentative de suppression ID: {id}, UserId: {userId}");

            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification != null)
            {
                _logger.LogInformation($"Notification trouvée - DestinataireId: {notification.DestinataireId}, UserId: {userId}");

                if (notification.DestinataireId == userId)
                {
                    await _notificationRepository.DeleteAsync(notification);
                    var saved = await _notificationRepository.SaveChangesAsync();
                    _logger.LogInformation($"SaveChangesAsync a retourné: {saved} modifications");
                }
                else
                {
                    _logger.LogWarning($"Accès non autorisé - Utilisateur {userId} tente de supprimer une notification destinée à {notification.DestinataireId}");
                }
            }
            else
            {
                _logger.LogWarning($"Notification {id} non trouvée");
            }
        }
        public async Task MarkAllAsConsultedAsync(Guid userId)
        {
            await _notificationRepository.MarkAllAsConsultedAsync(userId);
        }
        public async Task DeleteAllUserNotificationsAsync(Guid userId)
        {
            _logger.LogInformation($"DeleteAllUserNotificationsAsync - Suppression de toutes les notifications pour l'utilisateur {userId}");

            var notifications = await _notificationRepository.GetByUserIdAsync(userId);

            if (notifications != null && notifications.Any())
            {
                foreach (var notification in notifications)
                {
                    await _notificationRepository.DeleteAsync(notification);
                }

                var saved = await _notificationRepository.SaveChangesAsync();
                _logger.LogInformation($"DeleteAllUserNotificationsAsync - {saved} notifications supprimées pour l'utilisateur {userId}");
            }
            else
            {
                _logger.LogInformation($"DeleteAllUserNotificationsAsync - Aucune notification trouvée pour l'utilisateur {userId}");
            }
        }
    }
}