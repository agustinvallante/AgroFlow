using System.Text;
using System.Text.Json.Serialization;
using AgroFlow.Api.Contracts.Appointments;
using AgroFlow.Api.Middleware;
using AgroFlow.Api.Persistence;
using AgroFlow.Api.Security;
using AgroFlow.Application.Appointments;
using AgroFlow.Application.Services;
using AgroFlow.Data;
using AgroFlow.Data.Appointments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AgroFlow.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // El perfil sin identidad conserva exactamente los recorridos de la demo.
        // B03 deberá integrar identidad y turnos antes de retirar esta separación.
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
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Ingrese el token",
                    Type = SecuritySchemeType.ApiKey
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

        if (isLocalDemo)
        {
            var appointmentsConnection = LocalDemoDatabase.ResolveConnectionString(
                builder.Configuration.GetConnectionString("AgroFlowDb"), builder.Environment.ContentRootPath);
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddDbContext<AgroFlowDbContext>(options =>
                options.UseSqlite(appointmentsConnection));
            builder.Services.AddScoped<IAppointmentStore, EfAppointmentStore>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();
        }
        else
        {
            // Identity/JWT queda como base técnica. Roles y segregación por
            // ingenio del MVP todavía corresponden a B03.
            builder.Services.AddDbContext<AuthenticateContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("AgroFlowIdentity")));
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
            })
                .AddEntityFrameworkStores<AuthenticateContext>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<SpanishIdentityErrorDescriber>();

            var jwtConfig = builder.Configuration.GetSection("Jwt");
            var keyText = jwtConfig["Key"];
            if (string.IsNullOrWhiteSpace(keyText) || Encoding.UTF8.GetByteCount(keyText) < 32)
            {
                throw new InvalidOperationException(
                    "Fuera de LocalDemo, configure Jwt:Key con al menos 32 bytes mediante secretos o variables de entorno.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig["Issuer"],
                    ValidAudience = jwtConfig["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText))
                };
            });
            builder.Services.AddSingleton<JwtTokenService>();
        }

        builder.Services.AddAuthorization(options =>
            AppointmentsAuthorizationPolicy.Configure(options, isLocalDemo));
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
                appointmentsScope.ServiceProvider.GetRequiredService<TimeProvider>());
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");
        app.UseMiddleware<ExceptionHandlingMiddleware>();
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
