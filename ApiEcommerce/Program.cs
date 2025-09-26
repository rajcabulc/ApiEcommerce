using ApiEcommerce.Constants;
using ApiEcommerce.Data;
using ApiEcommerce.Repository;
using ApiEcommerce.Repository.IRepository;
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
                }
            );

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
                app.UseSwaggerUI();
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
