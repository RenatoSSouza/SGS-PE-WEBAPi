using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Insights;
using sgspe.Services.Insights;
 
namespace sgspe.Controllers;
 
[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/insights")]
public class InsightsController : ControllerBase
{
    private readonly InsightsService _service;
 
    public InsightsController(InsightsService service) => _service = service;
 
    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<InsightsResponse>> GetInsights(Guid companyId, int year, int month)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != companyId) return Forbid();
        var insights = await _service.GetInsightsAsync(companyId, year, month);
        return Ok(insights);
    }

    /// <summary>Série histórica mensal para gráficos de tendência do dashboard.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<HistoryResponse>> GetHistory(Guid companyId, [FromQuery] int months = 12)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != companyId) return Forbid();
        return Ok(await _service.GetHistoryAsync(companyId, months));
    }
}
