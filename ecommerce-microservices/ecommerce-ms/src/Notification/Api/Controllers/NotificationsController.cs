using Microsoft.AspNetCore.Mvc;
using Notification.Domain.Repositories;
namespace Notification.Api.Controllers;
[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
public sealed class NotificationsController(INotificationRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=20, CancellationToken ct=default)
    {
        var items = await repo.GetPagedAsync(page, pageSize, ct);
        var total = await repo.CountAsync(ct);
        return Ok(new { items, total, page, pageSize });
    }
}
