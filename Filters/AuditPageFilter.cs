using HRMS.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS.Filters;

public class AuditPageFilter : IAsyncPageFilter
{
    private readonly AuditService _audit;

    public AuditPageFilter(AuditService audit) => _audit = audit;

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var executed = await next();
        _audit.LogFromPageHandler(executed);
    }
}
