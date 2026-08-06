using ComplianceGuard.Api.Endpoints;
using ComplianceGuard.Api.Middleware;
using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Application.Transfers;
using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Infrastructure.Ai;
using ComplianceGuard.Infrastructure.Ai.Workflows;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<ITransferRepository, DapperTransferRepository>();
builder.Services.AddSingleton(sp => SemanticKernelFactory.Create(sp));
builder.Services.AddSingleton(sp => ComplianceWorkflowFactory.Create(sp));
builder.Services.AddScoped<IAnomalyDetectionService, AnomalyDetectionAgent>();
builder.Services.AddScoped<RecordTransferHandler>();
builder.Services.AddScoped<GetPackageTransferHistoryHandler>();
builder.Services.AddScoped<ReviewAnomalyHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await DataSeeder.SeedAsync(app.Services);
}

app.UseMiddleware<TenantResolutionMiddleware>();

app.MapFacilityEndpoints();
app.MapPackageEndpoints();
app.MapTransferEndpoints();
app.MapLabTestEndpoints();
app.MapAnomalyReviewEndpoints();

app.Run();

public partial class Program { }
