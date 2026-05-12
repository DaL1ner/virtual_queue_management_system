using Serilog;

namespace Api;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        // Add services to the container
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "VQMS API",
                Version = "v1",
                Description = "Virtual Queue Management System API"
            });
        });

        // Add Infrastructure layer
        builder.Services.AddInfrastructure(builder.Configuration);

        // Add Health Checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "");

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "VQMS API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");

        // Map endpoints
        app.MapHealthEndpoints();
        app.MapHealthChecks("/healthz");

        // Root endpoint
        app.MapGet("/", () => Results.Ok(new { Message = "Welcome to VQMS API", Version = "1.0.0" }))
            .WithName("Root")
            .WithOpenApi();

        app.Run();
    }
}
