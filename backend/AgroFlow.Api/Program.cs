using System.Text.Json.Serialization;
using AgroFlow.Api.Contracts.Appointments;
using AgroFlow.Api.Middleware;
using AgroFlow.Api.Persistence;
using AgroFlow.Api.Security;
using AgroFlow.Api.Services;
using AgroFlow.Application.Appointments;
using AgroFlow.Application.Conversations;
using AgroFlow.Application.MasterData;
using AgroFlow.Application.Services;
using AgroFlow.Data.Appointments;
using AgroFlow.Data.Identity;
using AgroFlow.Data.Repositories;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace AgroFlow.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isLocalDemo = builder.Configuration.GetValue("LocalDemo:Enabled", false);

        builder.Services.AddLogging(config =>
        {
            config.ClearProviders();
            config.AddConsole();
            var path = builder.Configuration.GetValue<string>("LogPath");
            if (!string.IsNullOrEmpty(path))
            {
                config.AddFile(path);
            }
        });

        builder.Services.AddControllers(options =>
            options.Conventions.Add(new LocalDemoAppointmentsConvention(isLocalDemo)))
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroFlow API", Version = "v1" });
            if (!isLocalDemo)
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Type = SecuritySchemeType.Http
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            }
        });
        builder.Services.AddHealthChecks();
        var connectionName = isLocalDemo ? "AgroFlowDb" : "AgroFlowMvpDb";
        var connection = LocalDemoDatabase.ResolveConnectionString(
            builder.Configuration.GetConnectionString(connectionName), builder.Environment.ContentRootPath, isLocalDemo);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<AgroFlowDbContext>(options => options.UseSqlite(connection));

        if (isLocalDemo)
        {
            builder.Services.AddScoped<IAppointmentStore, EfAppointmentStore>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();

            // Canal de WhatsApp, conversaciones y transportistas: siempre
            // detrás de una clave (ApiKeySchemes), nunca anónimos.
            builder.Services.AddSingleton(builder.Configuration
                .GetSection(ConversationOptions.SectionName).Get<ConversationOptions>() ?? new ConversationOptions());
            builder.Services.AddScoped<IConversationStore, EfConversationStore>();
            builder.Services.AddScoped<ConversationService>();
            builder.Services.AddScoped<ITenantContext, LocalDemoTenantContext>();
            builder.Services.AddScoped<IMasterDataUnitOfWork, EfMasterDataUnitOfWork>();
            builder.Services.AddScoped<MasterDataService>();
            builder.Services.AddAuthentication().AddAgroFlowApiKeys(builder.Configuration);
        }
        else
        {
            builder.Services.AddIdentityCore<AgroFlowUser>(options =>
            {
                options.Password.RequiredLength = 8;
            })
                .AddRoles<IdentityRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<AgroFlowDbContext>()
                .AddErrorDescriber<SpanishIdentityErrorDescriber>();

            builder.Services.AddMvpJwt(builder.Configuration);
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
            builder.Services.AddScoped<IAppointmentReader, EfAppointmentStore>();
        }

        builder.Services.AddAuthorization(options =>
        {
            AppointmentsAuthorizationPolicy.Configure(options, isLocalDemo);
            ApiKeyPolicies.Configure(options);
        });
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        var app = builder.Build();
        if (isLocalDemo)
        {
            using var appointmentsScope = app.Services.CreateScope();
            await AppointmentsStartup.InitializeAsync(
                appointmentsScope.ServiceProvider.GetRequiredService<AgroFlowDbContext>(),
                appointmentsScope.ServiceProvider.GetRequiredService<TimeProvider>(),
                app.Configuration["LocalDemo:WhatsAppInstance"]);
            await LocalDemoTeamFile.ApplyIfPresentAsync(
                appointmentsScope.ServiceProvider.GetRequiredService<AgroFlowDbContext>(),
                appointmentsScope.ServiceProvider.GetRequiredService<TimeProvider>(),
                app.Configuration,
                app.Environment,
                app.Logger);

            foreach (var setting in new[] { ApiKeySchemes.ChannelKeySetting, ApiKeySchemes.DashboardKeySetting })
            {
                if (string.IsNullOrEmpty(app.Configuration[setting]))
                {
                    app.Logger.LogWarning(
                        "{Setting} no está configurada: sus rutas responderán 401 hasta configurarla.", setting);
                }
            }
        }
        else
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AgroFlowDbContext>();
            await db.Database.MigrateAsync();
            if (app.Configuration["MvpSeed:Password"] is { Length: > 0 } password)
            {
                await MvpReadSeed.ApplyAsync(scope.ServiceProvider, password);
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");
        app.UseMiddleware<ExceptionHandlingMiddleware>(!isLocalDemo);
        if (!isLocalDemo)
        {
            app.UseAuthentication();
        }
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/healthcheck");

        // /health pertenece al contrato de la demo y comprueba la conexión real.
        if (isLocalDemo)
        {
            app.MapGet("/health", async (HttpContext http, AgroFlowDbContext db) =>
            {
                var healthy = false;
                try
                {
                    healthy = await db.Database.CanConnectAsync();
                }
                catch
                {
                    healthy = false;
                }

                if (healthy)
                {
                    return Results.Json(new { status = "Healthy" }, statusCode: 200, contentType: "application/json");
                }

                var problem = new AppointmentProblemDetails
                {
                    Title = "Servicio no disponible",
                    Status = 503,
                    Code = "SERVICE_UNAVAILABLE",
                    TraceId = http.TraceIdentifier
                };
                return Results.Json(problem, statusCode: 503, contentType: "application/problem+json");
            });
        }

        app.Run();
    }
}
