using Challenge.Api;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Challenge.UnitTests;

public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetails = Substitute.For<IProblemDetailsService>();

    private GlobalExceptionHandler CreateSut()
    {
        _problemDetails.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(ValueTask.FromResult(true));
        return new GlobalExceptionHandler(_problemDetails, NullLogger<GlobalExceptionHandler>.Instance);
    }

    [Fact]
    public async Task TryHandleAsync_ForUnexpectedException_WritesProblemDetailsWith500()
    {
        var context = new DefaultHttpContext();

        var handled = await CreateSut().TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        await _problemDetails.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(
            c => c!.ProblemDetails.Status == StatusCodes.Status500InternalServerError));
    }
}
