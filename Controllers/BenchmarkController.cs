using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Benchmark;
using sgspe.Services.Benchmark;

namespace sgspe.Controllers;

[Authorize(Roles = AppRoles.ManagerOrAdmin)]
[ApiController]
[Route("api/companies/{companyId:guid}/benchmark")]
public class BenchmarkController : ControllerBase
{
    private readonly BenchmarkService _service;

    public BenchmarkController(BenchmarkService service) => _service = service;

    [HttpGet("{year:int}/{month:int}")]
    /// <param name="size">Só para o Administrador: Micro, Small, Medium ou "all" (todos os portes). Os demais perfis
    /// comparam sempre com o porte cadastrado da empresa.</param>
    public async Task<ActionResult<BenchmarkResponse>> Get(Guid companyId, int year, int month, [FromQuery] string? size = null)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != companyId) return Forbid();
        if (month is < 1 or > 12) return BadRequest(new { message = "Mês inválido." });
        var isAdmin = User.IsAdmin();
        CompanySize? filter = null;
        var all = isAdmin && string.Equals(size, "all", StringComparison.OrdinalIgnoreCase);
        if (isAdmin && !all && !string.IsNullOrEmpty(size))
        {
            if (!Enum.TryParse<CompanySize>(size, true, out var parsed)) return BadRequest(new { message = "Porte inválido." });
            filter = parsed;
        }
        var result = await _service.GetAsync(companyId, year, month, isAdmin, filter, all);
        return result is null ? NotFound() : Ok(result);
    }
}
