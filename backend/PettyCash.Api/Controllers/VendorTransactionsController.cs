using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/vendor-transactions")]
public class VendorTransactionsController(
    GetVendorTransactionsUseCase getVendorTransactionsUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorTransactionDto>>> GetAll([FromQuery] int safeId)
    {
        var transactions = await getVendorTransactionsUseCase.ExecuteAsync(safeId);
        return Ok(transactions);
    }
}
