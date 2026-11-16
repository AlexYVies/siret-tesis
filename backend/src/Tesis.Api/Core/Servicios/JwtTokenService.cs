using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tesis.Domain.Entities;

namespace Tesis.Api.Core.Servicios;

public interface ITokenService
{
    string Generar(Usuario usuario, IEnumerable<string> roles, string? nombre = null, string? inicialApellido = null);
}

public sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generar(Usuario usuario, IEnumerable<string> roles, string? nombre = null, string? inicialApellido = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
        };

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            claims.Add(new Claim("nombre", nombre));
            claims.Add(new Claim(ClaimTypes.GivenName, nombre));
        }

        if (!string.IsNullOrWhiteSpace(inicialApellido))
        {
            claims.Add(new Claim("inicial_apellido", inicialApellido));
        }

        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var clave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Clave"]!));
        var credencialesSeguridad = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var minutosExpiracion = _configuration.GetValue<int>("Jwt:ExpiracionMinutos");
        var expiracion = DateTime.UtcNow.AddMinutes(minutosExpiracion);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Emisor"],
            audience: _configuration["Jwt:Audiencia"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiracion,
            signingCredentials: credencialesSeguridad);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}