using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Enum;
using sgspe.Models.Notifications;
using sgspe.Services.Notifications;

namespace sgspe.Controllers;

[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _service;

    public NotificationsController(NotificationService service) => _service = service;

    private bool CanAccessCompany(Guid companyId) => User.IsAdmin() || User.GetCompanyId() == companyId;
    private bool IsManager => User.IsAdmin() || User.IsManager();

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> GetAll(Guid companyId, [FromQuery] bool onlyUnread = false)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var items = await _service.ListAsync(companyId, User.GetUserId(), IsManager, onlyUnread);
        return Ok(items.Select(NotificationResponse.FromEntity).ToList());
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        return Ok(new { count = await _service.UnreadCountAsync(companyId, User.GetUserId(), IsManager) });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid companyId, Guid id)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        return await _service.MarkReadAsync(companyId, User.GetUserId(), IsManager, id) ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllRead(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        await _service.MarkAllReadAsync(companyId, User.GetUserId(), IsManager);
        return NoContent();
    }

    /// <summary>Força a verificação das metas do mês corrente (além da verificação automática periódica).</summary>
    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPost("refresh")]
    public async Task<ActionResult<object>> Refresh(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var now = DateTime.UtcNow;
        var created = await _service.GenerateGoalNotificationsAsync(companyId, now.Year, now.Month);
        return Ok(new { created });
    }
}
