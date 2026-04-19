using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Dashboard;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api")]
public class DashboardController(
    GetVendorDashboardUseCase getVendorDashboard,
    GetPettyCashDashboardUseCase getPettyCashDashboard) : ControllerBase
{
    [HttpGet("vendor-dashboard")]
    public async Task<ActionResult<VendorDashboardDto>> GetVendorDashboard([FromQuery] int safeId)
    {
        var dashboard = await getVendorDashboard.ExecuteAsync(safeId);
        return Ok(dashboard);
    }

    [HttpGet("pettycash-dashboard")]
    public async Task<ActionResult<PettyCashDashboardDto>> GetPettyCashDashboard([FromQuery] int safeId)
    {
        var dashboard = await getPettyCashDashboard.ExecuteAsync(safeId);
        return Ok(dashboard);
    }
}
