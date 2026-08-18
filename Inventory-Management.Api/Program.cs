using Inventory_Management.Api.Middleware;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Application.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Infrastructure.Persistence.Data;
using Inventory_Management.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TmsApi.Api;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ImsDbConnectionString")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    policy.WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod());
});

// Multi-Tenant Services
builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application Services & MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CategoryService).Assembly));
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ISaleItemService, SaleItemService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IPurchaseItemService, PurchaseItemService>();

builder.Services.AddIdentityCore<AppUser>(options =>
{
    // Enterprise Password Policy
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    // Brute-Force Lockout Protection
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TenantResolverMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowAngular");

app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/error", () =>
{
    throw new Exception("Simulated database failure for ProblemDetails testing");
});

app.Run();
