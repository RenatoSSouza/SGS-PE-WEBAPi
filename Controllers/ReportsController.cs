using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Services.Reports;

namespace sgspe.Controllers;

[Authorize(Roles = AppRoles.ManagerOrAdmin)]
[ApiController]
[Route("api/companies/{companyId:guid}/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;

    private readonly CertificationService _certification;

    public ReportsController(ReportService service, CertificationService certification)
    {
        _service = service;
        _certification = certification;
    }

    /// <summary>Relatório anual consolidado para certificação/auditoria ambiental.</summary>
    [HttpGet("certification/{year:int}/pdf")]
    public async Task<IActionResult> Certification(Guid companyId, int year)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != companyId) return Forbid();
        if (year is < 2000 or > 2100) return BadRequest(new { message = "Ano inválido." });
        var data = await _certification.BuildAsync(companyId, year);
        return data is null ? NotFound() : File(CertificationPdfBuilder.Build(data), "application/pdf", $"certificacao-{Stamp()}.pdf");
    }

    [HttpGet("{year:int}/{month:int}/pdf")]
    public async Task<IActionResult> Pdf(Guid companyId, int year, int month)
    {
        var data = await LoadAsync(companyId, year, month);
        if (data.Error is not null) return data.Error;
        return File(PdfReportBuilder.Build(data.Report!), "application/pdf", $"relatorio-{Stamp()}.pdf");
    }

    [HttpGet("{year:int}/{month:int}/xlsx")]
    public async Task<IActionResult> Excel(Guid companyId, int year, int month)
    {
        var data = await LoadAsync(companyId, year, month);
        if (data.Error is not null) return data.Error;
        return File(ExcelReportBuilder.Build(data.Report!),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"relatorio-{Stamp()}.xlsx");
    }

    private async Task<(Models.Reports.ReportData? Report, IActionResult? Error)> LoadAsync(Guid companyId, int year, int month)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != companyId) return (null, Forbid());
        if (month is < 1 or > 12 || year is < 2000 or > 2100) return (null, BadRequest(new { message = "Período inválido." }));
        var report = await _service.BuildAsync(companyId, year, month);
        return report is null ? (null, NotFound()) : (report, null);
    }

    /// <summary>Data e hora da geração no horário de Brasília (2026-11-20_17-05-32), para cada arquivo ter nome único.</summary>
    private static string Stamp()
    {
        var now = DateTime.UtcNow;
        try { now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { now = DateTime.Now; }
        return now.ToString("yyyy-MM-dd_HH-mm-ss");
    }
}
