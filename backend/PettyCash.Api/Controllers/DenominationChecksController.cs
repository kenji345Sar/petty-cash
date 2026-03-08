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
    GetDenominationChecksUseCase getDenominationChecksUseCase,
    UpdateDenominationCheckUseCase updateDenominationCheckUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DenominationCheckDto>>> GetAll()
    {
        var checks = await getDenominationChecksUseCase.ExecuteAsync();
        return Ok(checks);
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

    [HttpPut("{id}")]
    public async Task<ActionResult<DenominationCheckDto>> Update(int id, DenominationCheckRequestDto dto)
    {
        try
        {
            var check = await updateDenominationCheckUseCase.ExecuteAsync(id, dto);
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
