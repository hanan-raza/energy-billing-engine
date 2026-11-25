using EnergyBillingEngine.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Billing")
    ?? throw new InvalidOperationException("Connection string 'Billing' is not configured.");
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
