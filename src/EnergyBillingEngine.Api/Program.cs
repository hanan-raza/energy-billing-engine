using EnergyBillingEngine.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration.GetValue("BillingEngine:Database", DatabaseProvider.SqlServer);
var connectionString = builder.Configuration.GetConnectionString("Billing")
    ?? throw new InvalidOperationException("Connection string 'Billing' is not configured.");
builder.Services.AddInfrastructure(provider, connectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
