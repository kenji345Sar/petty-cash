using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.DenominationChecks;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DenominationChecksController(
    CheckChangeBagUseCase checkChangeBagUseCase,
    CheckCashBagUseCase checkCashBagUseCase,
    CheckPrepBagUseCase checkPrepBagUseCase,
    CheckSafeUseCase checkSafeUseCase,
    GetVendorDenominationChecksUseCase getVendorDenominationChecksUseCase,
    GetPettyCashDenominationChecksUseCase getPettyCashDenominationChecksUseCase,
    UpdateVendorDenominationCheckUseCase updateVendorDenominationCheckUseCase,
    UpdatePettyCashDenominationCheckUseCase updatePettyCashDenominationCheckUseCase) : ControllerBase
{
    [HttpGet("vendor")]
    public async Task<ActionResult<IReadOnlyList<DenominationCheckDto>>> GetVendorChecks([FromQuery] int safeId)
    {
        var checks = await getVendorDenominationChecksUseCase.ExecuteAsync(safeId);
        return Ok(checks);
    }

    [HttpGet("safe")]
    public async Task<ActionResult<IReadOnlyList<DenominationCheckDto>>> GetSafeChecks([FromQuery] int safeId)
    {
        var checks = await getPettyCashDenominationChecksUseCase.ExecuteAsync(safeId);
        return Ok(checks);
    }

    [HttpPost("safe/{safeId}")]
    public async Task<ActionResult<DenominationCheckDto>> CheckSafe(int safeId, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await checkSafeUseCase.ExecuteAsync(safeId, dto);
            return Ok(check);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("changebag/{bagId}")]
    public async Task<ActionResult<DenominationCheckDto>> CheckChangeBag(int bagId, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await checkChangeBagUseCase.ExecuteAsync(bagId, dto);
            return Ok(check);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("vendor/{id}")]
    public async Task<ActionResult<DenominationCheckDto>> UpdateVendor(int id, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await updateVendorDenominationCheckUseCase.ExecuteAsync(id, dto);
            return Ok(check);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("safe/{id}")]
    public async Task<ActionResult<DenominationCheckDto>> UpdateSafe(int id, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await updatePettyCashDenominationCheckUseCase.ExecuteAsync(id, dto);
            return Ok(check);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("cashbag/{bagId}")]
    public async Task<ActionResult<DenominationCheckDto>> CheckCashBag(int bagId, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await checkCashBagUseCase.ExecuteAsync(bagId, dto);
            return Ok(check);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("prepbag/{bagId}")]
    public async Task<ActionResult<DenominationCheckDto>> CheckPrepBag(int bagId, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await checkPrepBagUseCase.ExecuteAsync(bagId, dto);
            return Ok(check);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
