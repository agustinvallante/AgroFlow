using Dsw2025Tpi.Api.Contracts.Appointments;
using Dsw2025Tpi.Api.NewFolder;
using Dsw2025Tpi.Api.Security;
using Dsw2025Tpi.Application.Appointments;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Appointments;
using Dsw2025Tpi.Data.Helper;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dsw2025Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Perfil de demo local de turnos: sin JWT ni seeding heredado de
        // Identity/Customer. Default false para no cambiar el arranque
        // heredado fuera de Development; appsettings.Development.json lo
        // habilita para "dotnet run" en desarrollo.
        var isLocalDemo = builder.Configuration.GetValue("LocalDemo:Enabled", false);

        // Add services to the container.
        builder.Services.AddLogging(config =>
        {
            config.ClearProviders(); //para limpiar prov por defecto
            config.AddConsole();

            var path = builder.Configuration.GetValue<string>("LogPath");
            if (!string.IsNullOrEmpty(path))
            {
                config.AddFile(path);
            }
        });

        builder.Services.AddControllers().AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); 

        });

        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Desarollo de software TPI",
                Version = "v1",
            });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Ingrese el token",
                Type = SecuritySchemeType.ApiKey
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        });

        builder.Services.AddHealthChecks();

        builder.Services.AddDbContext<AuthenticateContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Dsw2025TpiEntities"));
        });

        // --- AQU� EST� EL CAMBIO ---
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 8
            };

          
        })
        .AddEntityFrameworkStores<AuthenticateContext>()
        .AddDefaultTokenProviders()
        .AddErrorDescriber<SpanishIdentityErrorDescriber>(); 


        if (!isLocalDemo)
        {
            var jwtConfig = builder.Configuration.GetSection("Jwt");
            var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
            var key = Encoding.UTF8.GetBytes(keyText);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtConfig["Issuer"],
                        ValidAudience = jwtConfig["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });
        }


        builder.Services.AddDbContext<Dsw2025TpiContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Dsw2025TpiEntities"));
        });

        // Demo local de turnos (Persona 2): persistencia propia en SQLite,
        // independiente de SQL Server/LocalDB e Identity.
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<AgroFlowDbContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("AgroFlowDb"));
        });
        builder.Services.AddScoped<IAppointmentStore, EfAppointmentStore>();
        builder.Services.AddScoped<IAppointmentService, AppointmentService>();

        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddAuthorization(options =>
            AppointmentsAuthorizationPolicy.Configure(options, isLocalDemo));

        builder.Services.AddScoped<IRepository, EfRepository>();
        builder.Services.AddScoped<ProductsManagementService>();
        builder.Services.AddScoped<OrdersManagementService>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                policy =>
                {
                    policy.WithOrigins("http://localhost:5173") 
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
        });

        var app = builder.Build();

        //para crear los roles y cargar los administradores desde el archivo JSON
        if (!isLocalDemo)
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                string adminRole = "Admin";
                string userRole = "User";

                // Crear roles si no existen
                if (!await roleManager.RoleExistsAsync(adminRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(adminRole));
                    Console.WriteLine($"Rol '{adminRole}' creado.");
                }
                if (!await roleManager.RoleExistsAsync(userRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(userRole));
                    Console.WriteLine($"Rol '{userRole}' creado.");
                }


                string jsonFilePath = Path.Combine(AppContext.BaseDirectory, "admins.json");
                if (File.Exists(jsonFilePath))
                {
                    string json = await File.ReadAllTextAsync(jsonFilePath);
                    var adminsToSeed = JsonSerializer.Deserialize<List<AdminUser>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });

                    if (adminsToSeed != null)
                    {
                        foreach (var adminData in adminsToSeed)
                        {
                            var adminUser = await userManager.FindByNameAsync(adminData.Username);
                            if (adminUser == null)
                            {
                                adminUser = new IdentityUser
                                {
                                    UserName = adminData.Username,
                                    Email = adminData.Email,
                                    EmailConfirmed = true
                                };
                                var createResult = await userManager.CreateAsync(adminUser, adminData.Password);

                                if (createResult.Succeeded)
                                {
                                    await userManager.AddToRoleAsync(adminUser, adminRole);
                                    Console.WriteLine($"Admin '{adminData.Username}' creado con rol '{adminRole}'.");
                                }
                                else
                                {
                                    Console.WriteLine($" Error al crear admin '{adminData.Username}':");
                                    foreach (var error in createResult.Errors)
                                    {
                                        Console.WriteLine($"- {error.Description}");
                                    }
                                }
                            }
                            else
                            {

                                if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                                {
                                    await userManager.AddToRoleAsync(adminUser, adminRole);
                                    Console.WriteLine($"Rol '{adminRole}' asignado a usuario existente '{adminUser.UserName}'.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($" Archivo 'admins.json' no encontrado en: {jsonFilePath}");
                }
                var context = services.GetRequiredService<Dsw2025TpiContext>();
                context.Seedwork<Customer>("customers.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Error durante la carga de administradores:");
                Console.WriteLine(ex.ToString());
            }
        }

        // Demo local de turnos (Persona 2): migra y siembra la base SQLite
        // propia, aislada del arranque heredado de arriba. Con LocalDemo=true
        // una falla aborta el arranque (fail-fast): nunca queda un host
        // "aparentemente sano" sin haber inicializado.
        using (var appointmentsScope = app.Services.CreateScope())
        {
            var services = appointmentsScope.ServiceProvider;
            var agroFlowDb = services.GetRequiredService<AgroFlowDbContext>();
            var timeProvider = services.GetRequiredService<TimeProvider>();
            await AppointmentsStartup.InitializeAsync(agroFlowDb, timeProvider, failFast: isLocalDemo);
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/healthcheck");

        // /health según docs/contracts/openapi.yaml: 200 JSON "Healthy" o 503
        // Problem Details "SERVICE_UNAVAILABLE". El /healthcheck heredado de
        // arriba no cumple el contrato (responde texto plano).
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

        app.Run();
    }
}
