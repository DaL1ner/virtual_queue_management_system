using System.IO;
using Serilog;
using Infrastructure.DependencyInjection;
using Application.DependencyInjection;
using Api.Endpoints;
using Api.Middleware;
using Microsoft.Extensions.FileProviders;

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
        builder.Services.AddSwaggerGen();

        // Add Infrastructure layer
        builder.Services.AddInfrastructure(builder.Configuration);

        // Add Application layer services
        builder.Services.AddApplication();

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

        // Добавить middleware аутентификации
        app.UseAuthenticationMiddleware();

        // ──────────────────────────────────────────────
        // Раздача статических файлов фронтендов
        // ──────────────────────────────────────────────

        // Определяем пути к dist-папкам фронтендов.
        // Сначала пробуем Development-путь (относительно корня решения — для локального запуска).
        // Если его нет — используем Production-путь (wwwroot — для Docker-контейнера).
        var clientDevPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "client", "client-interface", "dist"));
        var clientProdPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "client");
        var clientDistPath = Directory.Exists(clientDevPath) ? clientDevPath : clientProdPath;

        var appDevPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "user", "user-interface", "dist"));
        var appProdPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "app");
        var appDistPath = Directory.Exists(appDevPath) ? appDevPath : appProdPath;

        Log.Information("Client dist path: {Path} (exists: {Exists})", clientDistPath, Directory.Exists(clientDistPath));
        Log.Information("App dist path: {Path} (exists: {Exists})", appDistPath, Directory.Exists(appDistPath));

        // Раздача статики для /client (интерфейс посетителя)
        // Используем app.Map() вместо app.MapWhen(), так как Map обрезает префикс пути,
        // что необходимо для корректной работы UseStaticFiles.
        app.Map("/client", clientApp =>
        {
            if (Directory.Exists(clientDistPath))
            {
                // StaticFiles ищет файлы относительно clientDistPath
                // (путь уже обрезан Map до /assets/...)
                clientApp.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(clientDistPath),
                    ServeUnknownFileTypes = true // для .js/.ts модулей
                });

                // SPA fallback: все запросы к /client/*, не находящие файл,
                // отдают index.html для обработки Vue Router
                clientApp.Run(async context =>
                {
                    var indexFile = Path.Combine(clientDistPath, "index.html");
                    if (File.Exists(indexFile))
                    {
                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.SendFileAsync(indexFile);
                    }
                    else
                    {
                        Log.Warning("Client index.html not found at {Path}", indexFile);
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Client interface not built. Run: cd frontend/client/client-interface && npm run build:prod");
                    }
                });
            }
            else
            {
                clientApp.Run(async context =>
                {
                    Log.Warning("Client dist directory not found at {Path}", clientDistPath);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Client interface not built. Run: cd frontend/client/client-interface && npm run build:prod");
                });
            }
        });

        // Раздача статики для /app (интерфейс сотрудников)
        app.Map("/app", userApp =>
        {
            if (Directory.Exists(appDistPath))
            {
                userApp.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(appDistPath),
                    ServeUnknownFileTypes = true
                });

                // SPA fallback
                userApp.Run(async context =>
                {
                    var indexFile = Path.Combine(appDistPath, "index.html");
                    if (File.Exists(indexFile))
                    {
                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.SendFileAsync(indexFile);
                    }
                    else
                    {
                        Log.Warning("App index.html not found at {Path}", indexFile);
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("User interface not built. Run: cd frontend/user/user-interface && npm run build:prod");
                    }
                });
            }
            else
            {
                userApp.Run(async context =>
                {
                    Log.Warning("App dist directory not found at {Path}", appDistPath);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("User interface not built. Run: cd frontend/user/user-interface && npm run build:prod");
                });
            }
        });

        // ──────────────────────────────────────────────
        // API endpoints
        // ──────────────────────────────────────────────

        // Map endpoints
        app.MapHealthEndpoints();
        app.MapHealthChecks("/healthz");
        app.MapQueueConfigEndpoints();
        app.MapQueueSessionEndpoints();
        app.MapServiceTypeEndpoints();
        app.MapUserEndpoints();
        app.MapRoleEndpoints();
        app.MapAuthEndpoints();
        app.MapTicketEndpoints();
        app.MapExecutorStateEndpoints();

        // Root endpoint — редирект на /client (интерфейс посетителя)
        app.MapGet("/", () => Results.Redirect("/client/"))
            .WithName("Root")
            .WithOpenApi();

        app.Run();
    }
}
