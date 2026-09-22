using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Infrastructure.Persistence.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireBottleManagementAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentTenant = context.HttpContext.RequestServices.GetService<ICurrentTenant>();
        var dbContext = context.HttpContext.RequestServices.GetService<AppDbContext>();

        if (currentTenant != null && currentTenant.TenantId.HasValue && dbContext != null)
        {
            var tenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == currentTenant.TenantId.Value);

            if (tenant != null && !tenant.EnableBottleManagement)
            {
                context.Result = new BadRequestObjectResult(new { detail = "Bottle management is disabled for this tenant." });
                return;
            }
        }

        await next();
    }
}
