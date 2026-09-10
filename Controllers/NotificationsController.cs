using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationsController(
            INotificationService notificationService, IHttpContextAccessor httpContextAccessor)
        {
            _notificationService = notificationService;
            _httpContextAccessor = httpContextAccessor;
        }

        // Lấy UserId từ JWT
        private int? userid =>
AuditContext.GetUserId(
_httpContextAccessor.HttpContext!);


        // =========================
        // GET: api/Notifications
        // =========================
        [Authorize(Roles = "Admin,Teacher,Student")]
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
        

            if (!userid.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Không xác định được UserId."
                });
            }

            var notifications =
                await _notificationService
                    .GetByUserIdAsync(userid.Value);

            return Ok(notifications);
        }

        // =========================
        // POST: api/Notifications
        // =========================
        // Tạm dùng để TEST
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            NotificationCreateDto dto)
        {
            var notification =
                await _notificationService.CreateAsync(dto);

            return Ok(new
            {
                message = "Tạo thông báo thành công.",
                data = notification
            });
        }
        [Authorize(Roles = "Admin,Teacher,Student")]
        // =========================
        // PUT: api/Notifications/{id}/read
        // =========================
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
           

            if (!userid.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Không xác định được UserId."
                });
            }

            var result =
                await _notificationService
                    .MarkAsReadAsync(
                        id,
                        userid.Value);

            if (!result)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy thông báo."
                });
            }

            return Ok(new
            {
                message =
                    "Đã đánh dấu thông báo là đã đọc."
            });
        }

        // =========================
        // DELETE: api/Notifications/{id}
        // =========================
        [Authorize(Roles = "Admin,Teacher,Student")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
       

            if (!userid.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Không xác định được UserId."
                });
            }

            var result =
                await _notificationService
                    .DeleteAsync(
                        id,
                        userid.Value);

            if (!result)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy thông báo."
                });
            }

            return Ok(new
            {
                message =
                    "Xóa thông báo thành công."
            });
        }
        // =========================
        // PUT: api/Notifications/read-all
        // =========================
        [Authorize(Roles = "Admin,Teacher,Student")]
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!userid.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Không xác định được UserId."
                });
            }

            var result =
                await _notificationService
                    .MarkAllAsReadAsync(userid.Value);

            if (!result)
            {
                return NotFound(new
                {
                    message =
                        "Không có thông báo chưa đọc."
                });
            }

            return Ok(new
            {
                message =
                    "Đã đánh dấu tất cả thông báo là đã đọc."
            });
        }
    }

}