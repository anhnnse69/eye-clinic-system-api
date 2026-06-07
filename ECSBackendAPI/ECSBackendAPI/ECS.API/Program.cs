using ECS.API.Extensions;
using ECS.Application;
using ECS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────
builder.Services
    .AddCustomCors()
    .AddApiControllers()
    .AddApiDocumentation()
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ── Build ─────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
    app.UseApiDocumentation();

app.UseCustomCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
