using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationDbContext _dbContext;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(NotificationDbContext dbContext, ILogger<NotificationsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNotifications()
        {
            var notifications = await _dbContext.Notifications
                .ToListAsync();
            return Ok(notifications);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetNotification(Guid id)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
            {
                return NotFound();
            }

            return Ok(notification);
        }

        [HttpGet("job/{jobId:guid}")]
        public async Task<IActionResult> GetNotificationsByJobId(Guid jobId)
        {
            var notifications = await _dbContext.Notifications
                .Where(n => n.JobId == jobId)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPatch("{id:guid}/resend")]
        public async Task<IActionResult> PayInvoice(Guid id)
        {
            var notification = await _dbContext.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            if (notification.IsSent)
            {
                return BadRequest("Notification already sent");
            }

            // Here you would normally trigger email/SMS sending logic
            notification.IsSent = true;
            notification.SentAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Notification {id} resent to {notification.RecipientEmail}");

            return Ok(notification);
        }
    }
}
