using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.API.Controllers
{
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? Guid.Empty;
        }

        /// <summary>
        /// Get all notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            var notifications = await _notificationService.GetByUserIdAsync(userId);
            return Ok(notifications);
        }

        /// <summary>
        /// Get unread notifications for the current user
        /// </summary>
        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnreadNotifications()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            var notifications = await _notificationService.GetUnreadByUserIdAsync(userId);
            return Ok(notifications);
        }

        /// <summary>
        /// Get unread notifications count for the current user
        /// </summary>
        [HttpGet("unread/count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        /// <summary>
        /// Get notifications by type for the current user
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotificationsByType(TypeNotification type)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            var notifications = await _notificationService.GetByTypeAsync(userId, type);
            return Ok(notifications);
        }

        /// <summary>
        /// Get a specific notification
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotification(Guid id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            var notification = await _notificationService.GetByIdAsync(id);
            if (notification == null)
                return NotFound();

            if (notification.DestinataireId != userId)
                return Forbid();

            return Ok(notification);
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(new { message = "Notification marquée comme lue" });
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPut("read/all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "Toutes les notifications ont été marquées comme lues" });
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == Guid.Empty)
                return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, userId);
            return Ok(new { message = "Notification supprimée" });
        }

        /// <summary>
        /// Delete all read notifications (Admin only)
        /// </summary>
        [HttpDelete("cleanup")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CleanupOldNotifications([FromQuery] int daysToKeep = 30)
        {
            await _notificationService.DeleteOldNotificationsAsync(daysToKeep);
            return Ok(new { message = $"Notifications plus vieilles que {daysToKeep} jours supprimées" });
        }
    }
}
