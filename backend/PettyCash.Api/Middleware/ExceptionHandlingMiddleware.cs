using System.Text.Json;

namespace PettyCash.Api.Middleware;

/// <summary>
/// ドメイン例外をHTTPステータスコードに変換するミドルウェア。
///
/// 【マッピング】
///   - KeyNotFoundException → 404 Not Found
///   - ArgumentException → 400 Bad Request（バリデーションエラー、不変条件違反）
///   - InvalidOperationException → 409 Conflict（状態遷移違反、二重操作）
///   - その他 → 500 Internal Server Error
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "予期しないエラーが発生しました");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "サーバーエラーが発生しました");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { message }));
    }
}
