using Challenge.Core.Dtos;
using Challenge.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(ITransactionService transactions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Add(CreateTransactionDto dto, CancellationToken cancellationToken)
    {
        var created = await transactions.AddAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetAll), null, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetAll(
        [FromQuery] string? userId,
        CancellationToken cancellationToken)
    {
        var result = string.IsNullOrWhiteSpace(userId)
            ? await transactions.GetAllAsync(cancellationToken)
            : await transactions.GetByUserAsync(userId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("totals/per-user")]
    public async Task<ActionResult<IReadOnlyList<UserTotalDto>>> GetTotalsPerUser(CancellationToken cancellationToken)
        => Ok(await transactions.GetTotalPerUserAsync(cancellationToken));

    [HttpGet("totals/per-type")]
    public async Task<ActionResult<IReadOnlyList<TransactionTypeTotalDto>>> GetTotalsPerType(CancellationToken cancellationToken)
        => Ok(await transactions.GetTotalPerTypeAsync(cancellationToken));

    [HttpGet("high-volume")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetHighVolume(
        [FromQuery] decimal threshold,
        CancellationToken cancellationToken)
        => Ok(await transactions.GetHighVolumeAsync(threshold, cancellationToken));
}
