using ApiEcommerce.Constants;
using ApiEcommerce.Data;
using ApiEcommerce.Repository;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ApiEcommerce
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // connection to database
            var dbConnString = builder.Configuration.GetConnectionString("ConnDb");
            builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(dbConnString));

            //
            builder.Services.AddResponseCaching(opt =>
            {
                opt.MaximumBodySize = 1024 * 1024; // 1 MB
                opt.UseCaseSensitivePaths = true;
            });

            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>(); //
            builder.Services.AddScoped<IUserRepository, UserRepository>(); //
            builder.Services.AddAutoMapper(typeof(Program).Assembly);

            // Authentication
            var secretKey = builder.Configuration.GetValue<string>("ApiSettings:SecretKey");
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("SecretKey no esta Configurada");
            builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
                opt.RequireHttpsMetadata = false; // desactivar en desarrollo HTTPS/en produccion es recomendable activar
                opt.SaveToken = true; // guardar el token en el contexto de la autenticacion
                // definir parametros de validacion
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, // validar el token
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), // clave secreta para validar la firma el token
                    ValidateIssuer = false, // indica que no se valida el emisor del token
                    ValidateAudience = false // indica que no se valida 
                };
            });

            // configurando controllers y cache profiles
            builder.Services.AddControllers(opt =>
            {
                // creando perfiles de cache para llamarlos en los controladores
                opt.CacheProfiles.Add(CacheProfiles.Default10, CacheProfiles.Profile10);
                opt.CacheProfiles.Add(CacheProfiles.Default20, CacheProfiles.Profile20);
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            // incorporando autenticacion en swagger
            builder.Services.AddSwaggerGen(
                options =>
                {
                    // aniadiendo esquema de seguridad Bearer
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "Nuestra API utiliza la Autenticación JWT usando el esquema Bearer. \n\r\n\r" +
                                    "Ingresa a continuación el token generado en login.\n\r\n\r" +
                                    "Ejemplo: \"12345abcdef\"",
                        Name = "Authorization", // encabezado de la peticion
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer"
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                      {
                        new OpenApiSecurityScheme
                        {
                          Reference = new OpenApiReference
                          {
                            Type = ReferenceType.SecurityScheme, // se hace referencia al esquema creado anteriormente
                            Id = "Bearer"
                          },
                          Scheme = "oauth2", // solo es un marcador, requisito de swagger
                          Name = "Bearer",
                          In = ParameterLocation.Header
                        },
                        new List<string>() // lista vacia de scopes
                      }
                    });
                    // agregando documentacion de API
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "ApiEcommerce V1",
                        Description = "API para gestionar Productos  y Usuarios",
                        TermsOfService = new Uri("http://example.com/terms"),
                        Contact = new OpenApiContact
                        {
                            Name = "SEITIN",
                            Url = new Uri("https://seitin.com.gt")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Licencia de Uso",
                            Url = new Uri("https://seitin.com.gt/LicenseApi")
                        }
                    });
                    //
                    options.SwaggerDoc("v2", new OpenApiInfo
                    {
                        Version = "v2",
                        Title = "ApiEcommerce V2",
                        Description = "API para gestionar Productos  y Usuarios",
                        TermsOfService = new Uri("http://example.com/terms"),
                        Contact = new OpenApiContact
                        {
                            Name = "SEITIN",
                            Url = new Uri("https://seitin.com.gt")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Licencia de Uso",
                            Url = new Uri("https://seitin.com.gt/LicenseApi")
                        }
                    });
                }
            );

            // versionamiento de API
            var apiVersioningBuilder = builder.Services.AddApiVersioning(opt =>
            {
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.DefaultApiVersion = new ApiVersion(1, 0); //1,0
                opt.ReportApiVersions = true;
                //opt.ApiVersionReader = ApiVersionReader.Combine(new QueryStringApiVersionReader("api-version")); //?api-version
            });
            apiVersioningBuilder.AddApiExplorer(opt =>
            {
                opt.GroupNameFormat = "'v'VVV"; // v1, v2, v3
                opt.SubstituteApiVersionInUrl = true; // api/v{version}/products
            });

            // Cors, para acceder o aceptar petitiones en diferentes dominios
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy(PolicyNames.AllowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader(); // en origen(*) poner la url del front end o las que se quieran permitir
                    });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                // modificando UserSwagger
                app.UseSwaggerUI(opt =>
                {
                    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                    opt.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
                });
            }

            app.UseHttpsRedirection();

            app.UseCors(PolicyNames.AllowSpecificOrigins); //

            app.UseResponseCaching(); //
            //
            app.UseAuthentication(); //

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
