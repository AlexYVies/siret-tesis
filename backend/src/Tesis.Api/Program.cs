using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tesis.Api.Core;
using Tesis.Api.Core.Servicios;
using Tesis.Api.Middlewares;
using Tesis.Api.Serializacion;
using Tesis.Application;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;
using Tesis.Infrastructure;
using Tesis.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        foreach (var convertidor in SerializacionEnumsEspanol.ConstruirConvertidores())
        {
            opciones.JsonSerializerOptions.Converters.Add(convertidor);
        }
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Copiar el token JWT en el campo de autorización.",
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddIdentityCore<Usuario>(opciones =>
    {
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Password.RequiredLength = 8;
        opciones.User.RequireUniqueEmail = true;
    })
    .AddRoles<Rol>()
    .AddEntityFrameworkStores<TesisDbContext>()
    .AddSignInManager()
    .AddErrorDescriber<MensajesIdentityEspanol>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Clave"]!)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy("PermitirFrontend", politica =>
        politica.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

using (var alcance = app.Services.CreateScope())
{
    var servicios = alcance.ServiceProvider;
    var roleManager = servicios.GetRequiredService<RoleManager<Rol>>();
    foreach (var rol in ConstantesRol.Todos)
    {
        if (!await roleManager.RoleExistsAsync(rol))
        {
            await roleManager.CreateAsync(new Rol { Name = rol });
        }
    }

    var contexto = servicios.GetRequiredService<TesisDbContext>();
    if (!await contexto.TiposArticulo.AnyAsync())
    {
        contexto.TiposArticulo.AddRange(
            new TipoArticulo { Nombre = "Artículo de revista", Descripcion = "Publicación académica arbitrada." },
            new TipoArticulo { Nombre = "Artículo de congreso", Descripcion = "Memorias y proceedings de congresos." },
            new TipoArticulo { Nombre = "Capítulo de libro", Descripcion = "Contribución en volumen colectivo." },
            new TipoArticulo { Nombre = "Libro", Descripcion = "Obra completa publicada." });
        await contexto.SaveChangesAsync();
    }

    if (!await contexto.Posgrados.AnyAsync())
    {
        contexto.Posgrados.AddRange(
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Energías Renovables", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Computación Óptica", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Dirección de Organizaciones", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Desarrollo de Software", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Automatización y Control", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Optimización de Procesos", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Contribuciones Fiscales", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Maestria, Nombre = "Maestría en Gestión e Innovación Educativa", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Doctorado, Nombre = "Doctorado en Optomecatrónica", Activo = true },
            new Posgrado { Nivel = NivelPosgrado.Doctorado, Nombre = "Doctorado en Ciencias de la Gestión Administrativa", Activo = true });
        await contexto.SaveChangesAsync();
    }

    var userManager = servicios.GetRequiredService<UserManager<Usuario>>();
    var adminCorreo = builder.Configuration["AdministradorInicial:Correo"] ?? "admin@upt.edu.mx";
    var adminPass = builder.Configuration["AdministradorInicial:Contrasena"] ?? "Admin.123";

    var adminUsuario = await userManager.FindByEmailAsync(adminCorreo);
    if (adminUsuario is null)
    {
        adminUsuario = new Usuario
        {
            UserName = adminCorreo,
            Email = adminCorreo,
            EmailConfirmed = true,
            FechaReg = DateTime.UtcNow,
            FechaMod = DateTime.UtcNow,
        };
        var resultado = await userManager.CreateAsync(adminUsuario, adminPass);
        if (resultado.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUsuario, ConstantesRol.Administrador);
        }
    }
    else
    {
        if (!await userManager.IsInRoleAsync(adminUsuario, ConstantesRol.Administrador))
        {
            await userManager.AddToRoleAsync(adminUsuario, ConstantesRol.Administrador);
        }
        if (!adminUsuario.EmailConfirmed)
        {
            adminUsuario.EmailConfirmed = true;
            await userManager.UpdateAsync(adminUsuario);
        }
        var tokenReset = await userManager.GeneratePasswordResetTokenAsync(adminUsuario);
        await userManager.ResetPasswordAsync(adminUsuario, tokenReset, adminPass);
    }

    if (app.Environment.IsDevelopment())
    {
        await SembrarDatosDesarrolloAsync(contexto, userManager);
    }
}

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PermitirFrontend");
app.UseHttpsRedirection();
app.UseMiddleware<ErrorApiMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}