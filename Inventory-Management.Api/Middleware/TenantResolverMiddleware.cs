using Inventory_Management.Application.Interfaces.Services;

using Inventory_Management.Infrastructure.Persistence.Data;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Api.Middleware;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant, AppDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("TenantId");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                currentTenant.SetTenant(tenantId);
                
                var tenant = await dbContext.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant != null)
                {
                    if (tenant.Status == TenantStatus.Suspended)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new { message = "Your company account has been suspended. Please contact the system administrator." });
                        return;
                    }
                    if (tenant.Status == TenantStatus.Deactivated)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new { message = "Your company account has been deactivated. Please contact the system administrator." });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
