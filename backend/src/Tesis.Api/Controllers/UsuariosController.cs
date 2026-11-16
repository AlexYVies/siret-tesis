using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tesis.Api.Core;
using Tesis.Api.Core.Servicios;
using Tesis.Api.Modelos.Cuentas;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ICorreoService _correo;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UsuariosController> _logger;
    private readonly TesisDbContext _contexto;

    public UsuariosController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        ITokenService tokenService,
        ICorreoService correo,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<UsuariosController> logger,
        TesisDbContext contexto)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _correo = correo;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _contexto = contexto;
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Registro(RegistroUsuarioRequest request)
    {
        var dominioInstitucional = _configuration["Identity:EmailDominioInstitucional"] ?? "upt.edu.mx";
        var correo = request.Correo?.Trim();

        if (string.IsNullOrWhiteSpace(correo) || !correo.Contains('@'))
        {
            return Error("CORREO_OBLIGATORIO", "Debe ingresar un correo electrónico válido.", StatusCodes.Status400BadRequest);
        }

        if (!ConstantesRol.AutoRegistrables.Contains(request.Rol))
        {
            return Error(
                "ROL_NO_REGISTRABLE",
                "El registro de docentes se realiza únicamente mediante invitación enviada por correo por el Administrador. Solo Alumno e Invitado están disponibles aquí.",
                StatusCodes.Status400BadRequest);
        }

        // Flujo de Invitado: solo pide el correo (cualquier dominio, no necesariamente UPT)
        if (request.Rol == ConstantesRol.Invitado)
        {
            if (correo.Equals("invitado@upt.edu.mx", StringComparison.OrdinalIgnoreCase))
            {
                return Error("CORREO_RESERVADO", "Por favor ingresa tu correo electrónico personal.", StatusCodes.Status400BadRequest);
            }

            var usuarioInvitado = await _userManager.FindByEmailAsync(correo);
            if (usuarioInvitado is not null)
            {
                return Error(
                    "CORREO_YA_REGISTRADO",
                    "Este correo ya tiene una cuenta registrada. Puedes iniciar sesión directamente en la sección de Invitado.",
                    StatusCodes.Status400BadRequest);
            }

            usuarioInvitado = new Usuario
            {
                UserName = correo,
                Email = correo,
                EmailConfirmed = true,
            };
            var creacionInvitado = await _userManager.CreateAsync(usuarioInvitado, "Invitado1234!");
            if (!creacionInvitado.Succeeded)
            {
                return ErrorIdentity(creacionInvitado, StatusCodes.Status400BadRequest, "REGISTRO_INVALIDO");
            }

            if (!await _contexto.Roles.AnyAsync(r => r.NormalizedName == ConstantesRol.Invitado.ToUpperInvariant()))
            {
                _contexto.Roles.Add(new Rol { Name = ConstantesRol.Invitado, NormalizedName = ConstantesRol.Invitado.ToUpperInvariant() });
                await _contexto.SaveChangesAsync();
            }

            await _userManager.AddToRoleAsync(usuarioInvitado, ConstantesRol.Invitado);

            var rolesInvitado = new[] { ConstantesRol.Invitado };
            var tokenJwt = _tokenService.Generar(usuarioInvitado, rolesInvitado);
            var expiracion = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiracionMinutos"));

            return StatusCode(
                StatusCodes.Status201Created,
                new RegistroUsuarioResponse(
                    usuarioInvitado.Id,
                    null,
                    tokenJwt,
                    expiracion,
                    correo,
                    rolesInvitado));
        }

        if (!correo.EndsWith("@" + dominioInstitucional, StringComparison.OrdinalIgnoreCase))
        {
            return Error(
                "CORREO_NO_INSTITUCIONAL",
                $"El correo debe pertenecer al dominio institucional {dominioInstitucional}.",
                StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return Error(
                "CONTRASENA_OBLIGATORIA",
                "La contraseña es obligatoria para el registro de alumno.",
                StatusCodes.Status400BadRequest);
        }

        var usuario = new Usuario
        {
            UserName = correo,
            Email = correo,
            EmailConfirmed = false,
        };

        var creacion = await _userManager.CreateAsync(usuario, request.Contrasena);
        if (!creacion.Succeeded)
        {
            var esDuplicado = creacion.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail");
            if (esDuplicado)
            {
                var usuarioExistente = await _userManager.FindByEmailAsync(correo);
                if (usuarioExistente != null && !usuarioExistente.EmailConfirmed)
                {
                    return Error(
                        "CORREO_REGISTRADO_PENDIENTE_VALIDACION",
                        "Este correo institucional ya está registrado pero su cuenta aún no ha sido activada.",
                        StatusCodes.Status400BadRequest);
                }

                return Error(
                    "CORREO_YA_REGISTRADO",
                    "Este correo institucional ya está registrado. Puedes iniciar sesión con tu contraseña.",
                    StatusCodes.Status400BadRequest);
            }

            return ErrorIdentity(creacion, StatusCodes.Status400BadRequest, "REGISTRO_INVALIDO");
        }

        var asignacionRol = await _userManager.AddToRoleAsync(usuario, request.Rol);
        if (!asignacionRol.Succeeded)
        {
            return ErrorIdentity(asignacionRol, StatusCodes.Status400BadRequest, "REGISTRO_INVALIDO");
        }

        // Matrícula y Nombre completo
        var prefijoCorreo = correo.Contains('@') ? correo.Split('@')[0] : correo;
        var matricula = !string.IsNullOrWhiteSpace(request.Matricula)
            ? request.Matricula.Trim()
            : (prefijoCorreo.All(char.IsDigit) ? prefijoCorreo : (request.Rol == ConstantesRol.Alumno ? prefijoCorreo : null));

        var nombreDesglosado = $"{request.Nombres} {request.PrimerApellido} {request.SegundoApellido}".Trim();
        var nombre = !string.IsNullOrWhiteSpace(request.Nombre)
            ? request.Nombre.Trim()
            : (!string.IsNullOrWhiteSpace(nombreDesglosado) ? nombreDesglosado : (matricula ?? prefijoCorreo));

        try
        {
            if (request.Rol == ConstantesRol.Alumno)
            {
                var alumnoExistente = await _contexto.Alumnos.FirstOrDefaultAsync(a => a.Correo == correo || a.IdUsuario == usuario.Id);
                if (alumnoExistente == null)
                {
                    var nuevoAlumno = new Alumno
                    {
                        IdUsuario = usuario.Id,
                        Nombre = nombre,
                        NumeroControl = matricula ?? string.Empty,
                        Correo = correo,
                    };
                    await _contexto.Alumnos.AddAsync(nuevoAlumno);
                    await _contexto.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear registro de entidad asociada ({Rol}) para usuario Id: {Id}", request.Rol, usuario.Id);
        }

        var tokenValidacion = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
        var tokenDev = _environment.IsDevelopment() ? tokenValidacion : null;

        var enlace = $"{BaseFrontend()}/validar-cuenta?id={usuario.Id}&token={Uri.EscapeDataString(tokenValidacion)}";
        var cuerpoHtml = PlantillasCorreo.GenerarHtmlValidacionCuenta(nombre, usuario.Id, enlace, tokenValidacion);

        try
        {
            await _correo.EnviarAsync(correo, "SIRET - UPT: Valida tu cuenta de correo", cuerpoHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de validación a {Correo}. Usuario Id: {Id}", correo, usuario.Id);
        }

        return StatusCode(StatusCodes.Status201Created, new RegistroUsuarioResponse(usuario.Id, tokenDev));
    }

    [HttpPost("invitar-docente")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> InvitarDocente(InvitarDocenteRequest request)
    {
        var dominioInstitucional = _configuration["Identity:EmailDominioInstitucional"] ?? "upt.edu.mx";
        var correo = request.Correo?.Trim();
        var nombre = request.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(correo))
        {
            return Error("CORREO_OBLIGATORIO", "El correo del docente es obligatorio.", StatusCodes.Status400BadRequest);
        }

        if (!correo.EndsWith("@" + dominioInstitucional, StringComparison.OrdinalIgnoreCase))
        {
            return Error(
                "CORREO_NO_INSTITUCIONAL",
                $"El correo del docente debe pertenecer al dominio institucional @{dominioInstitucional}.",
                StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Error("NOMBRE_OBLIGATORIO", "El nombre completo del docente es obligatorio.", StatusCodes.Status400BadRequest);
        }

        var usuario = await _userManager.FindByEmailAsync(correo);
        if (usuario is null)
        {
            usuario = new Usuario
            {
                UserName = correo,
                Email = correo,
                EmailConfirmed = false,
            };
            var tempPass = $"Invitacion{Guid.NewGuid():N}!1Aa";
            var creacion = await _userManager.CreateAsync(usuario, tempPass);
            if (!creacion.Succeeded)
            {
                return ErrorIdentity(creacion, StatusCodes.Status400BadRequest, "INVITACION_INVALIDA");
            }

            await _userManager.AddToRoleAsync(usuario, ConstantesRol.Docente);
        }
        else
        {
            if (await _userManager.IsInRoleAsync(usuario, ConstantesRol.Alumno))
            {
                return Error("CORREO_DE_ALUMNO", "Este correo institucional pertenece a una cuenta de alumno.", StatusCodes.Status400BadRequest);
            }

            if (!await _userManager.IsInRoleAsync(usuario, ConstantesRol.Docente))
            {
                await _userManager.AddToRoleAsync(usuario, ConstantesRol.Docente);
            }
        }

        if (request.IdProfesor.HasValue)
        {
            var profPorId = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdProfesor == request.IdProfesor.Value);
            if (profPorId != null)
            {
                profPorId.IdUsuario = usuario.Id;
                profPorId.Nombre = nombre;
                await _contexto.SaveChangesAsync();
            }
        }
        else
        {
            var profesorExistente = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == usuario.Id);
            if (profesorExistente == null)
            {
                var profSinCuenta = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == null && p.Nombre == nombre);
                if (profSinCuenta != null)
                {
                    profSinCuenta.IdUsuario = usuario.Id;
                }
                else
                {
                    await _contexto.Profesores.AddAsync(new Profesor
                    {
                        IdUsuario = usuario.Id,
                        Nombre = nombre,
                        Activo = true,
                    });
                }
                await _contexto.SaveChangesAsync();
            }
        }

        var tokenInvitacion = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var enlace = $"{BaseFrontend()}/registro?invitacion={Uri.EscapeDataString(tokenInvitacion)}&id={usuario.Id}&correo={Uri.EscapeDataString(correo)}&nombre={Uri.EscapeDataString(nombre)}";
        var cuerpoHtml = PlantillasCorreo.GenerarHtmlInvitacionDocente(nombre, correo, enlace);

        try
        {
            await _correo.EnviarAsync(correo, "SIRET - UPT: Invitación para registrarte como Docente", cuerpoHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar invitación de docente a {Correo}.", correo);
            return Error("ERROR_ENVIO_CORREO", "Se preparó la cuenta del docente, pero no se pudo enviar el correo de invitación.", StatusCodes.Status500InternalServerError);
        }

        return Ok(new
        {
            idUsuario = usuario.Id,
            correo,
            nombre,
            mensaje = $"Invitación enviada correctamente a {correo}.",
        });
    }

    [HttpGet("directores")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> ListarDirectores()
    {
        var usuariosDirectores = await _userManager.GetUsersInRoleAsync(ConstantesRol.DirectorDePosgrado);
        var ids = usuariosDirectores.Select(u => u.Id).ToList();

        var profesores = await _contexto.Profesores
            .Where(p => p.IdUsuario.HasValue && ids.Contains(p.IdUsuario.Value))
            .ToListAsync();

        var lista = usuariosDirectores.Select(u =>
        {
            var prof = profesores.FirstOrDefault(p => p.IdUsuario == u.Id);
            var nombre = prof?.Nombre ?? u.Email ?? "Director de Posgrado";
            return new DirectorDto(
                u.Id,
                u.Email ?? "",
                nombre,
                u.EmailConfirmed,
                u.FechaReg != default ? u.FechaReg : null);
        }).OrderBy(d => d.Nombre).ToList();

        return Ok(lista);
    }

    [HttpPost("directores")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> RegistrarDirector(SolicitudDirectorRequest request)
    {
        var dominioInstitucional = _configuration["Identity:EmailDominioInstitucional"] ?? "upt.edu.mx";
        var correo = request.Correo?.Trim();
        var nombre = string.IsNullOrWhiteSpace(request.Nombre) ? "Director de Posgrado" : request.Nombre.Trim();
        var contrasena = string.IsNullOrWhiteSpace(request.Contrasena) ? "Admin.123" : request.Contrasena.Trim();

        if (string.IsNullOrWhiteSpace(correo) || !correo.Contains('@'))
        {
            return Error("CORREO_OBLIGATORIO", "Debe indicar un correo electrónico válido.", StatusCodes.Status400BadRequest);
        }

        if (!correo.EndsWith("@" + dominioInstitucional, StringComparison.OrdinalIgnoreCase))
        {
            return Error("CORREO_NO_INSTITUCIONAL", $"El correo del director debe pertenecer al dominio institucional @{dominioInstitucional}.", StatusCodes.Status400BadRequest);
        }

        if (contrasena.Length < 8)
        {
            return Error("CONTRASENA_CORTA", "La contraseña debe tener al menos 8 caracteres.", StatusCodes.Status400BadRequest);
        }

        if (!await _contexto.Roles.AnyAsync(r => r.NormalizedName == ConstantesRol.DirectorDePosgrado.ToUpperInvariant()))
        {
            _contexto.Roles.Add(new Rol { Name = ConstantesRol.DirectorDePosgrado, NormalizedName = ConstantesRol.DirectorDePosgrado.ToUpperInvariant() });
            await _contexto.SaveChangesAsync();
        }

        var usuario = await _userManager.FindByEmailAsync(correo);
        if (usuario is null)
        {
            usuario = new Usuario
            {
                UserName = correo,
                Email = correo,
                EmailConfirmed = true,
                FechaReg = DateTime.UtcNow,
                FechaMod = DateTime.UtcNow,
            };
            var creacion = await _userManager.CreateAsync(usuario, contrasena);
            if (!creacion.Succeeded)
            {
                return ErrorIdentity(creacion, StatusCodes.Status400BadRequest, "REGISTRO_INVALIDO");
            }

            await _userManager.AddToRoleAsync(usuario, ConstantesRol.DirectorDePosgrado);

            var profesor = new Profesor
            {
                IdUsuario = usuario.Id,
                Nombre = nombre,
                Activo = true,
            };
            _contexto.Profesores.Add(profesor);
            await _contexto.SaveChangesAsync();
        }
        else
        {
            if (await _userManager.IsInRoleAsync(usuario, ConstantesRol.Alumno))
            {
                return Error("CORREO_DE_ALUMNO", "Este correo institucional pertenece a una cuenta de alumno.", StatusCodes.Status400BadRequest);
            }

            if (!await _userManager.IsInRoleAsync(usuario, ConstantesRol.DirectorDePosgrado))
            {
                await _userManager.AddToRoleAsync(usuario, ConstantesRol.DirectorDePosgrado);
            }

            if (!usuario.EmailConfirmed)
            {
                usuario.EmailConfirmed = true;
                await _userManager.UpdateAsync(usuario);
            }

            if (!string.IsNullOrWhiteSpace(request.Contrasena))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                await _userManager.ResetPasswordAsync(usuario, token, contrasena);
            }

            var profExistente = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == usuario.Id);
            if (profExistente is null)
            {
                _contexto.Profesores.Add(new Profesor
                {
                    IdUsuario = usuario.Id,
                    Nombre = nombre,
                    Activo = true,
                });
            }
            else if (!string.IsNullOrWhiteSpace(request.Nombre))
            {
                profExistente.Nombre = nombre;
            }
            await _contexto.SaveChangesAsync();
        }

        var directorDto = new DirectorDto(
            usuario.Id,
            usuario.Email ?? "",
            nombre,
            usuario.EmailConfirmed,
            usuario.FechaReg != default ? usuario.FechaReg : null);

        return StatusCode(StatusCodes.Status201Created, directorDto);
    }

    [HttpDelete("directores/{idUsuario:int}")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> RemoverDirector(int idUsuario)
    {
        var usuario = await _userManager.FindByIdAsync(idUsuario.ToString());
        if (usuario is null)
        {
            return Error("USUARIO_NO_ENCONTRADO", "No se encontró el usuario especificado.", StatusCodes.Status404NotFound);
        }

        if (await _userManager.IsInRoleAsync(usuario, ConstantesRol.DirectorDePosgrado))
        {
            await _userManager.RemoveFromRoleAsync(usuario, ConstantesRol.DirectorDePosgrado);
        }

        return Ok(new { message = "Rol de Director de posgrado removido exitosamente." });
    }

    [HttpPost("completar-registro-docente")]
    [AllowAnonymous]
    public async Task<IActionResult> CompletarRegistroDocente(CompletarRegistroDocenteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return Error("CONTRASENA_OBLIGATORIA", "Debe definir una contraseña para su cuenta de docente.", StatusCodes.Status400BadRequest);
        }

        var usuario = await _userManager.FindByIdAsync(request.IdUsuario.ToString());
        if (usuario is null && !string.IsNullOrWhiteSpace(request.Correo))
        {
            usuario = await _userManager.FindByEmailAsync(request.Correo.Trim());
        }

        if (usuario is null)
        {
            return Error("CUENTA_NO_ENCONTRADA", "La invitación no corresponde a una cuenta válida.", StatusCodes.Status400BadRequest);
        }

        if (!await _userManager.IsInRoleAsync(usuario, ConstantesRol.Docente))
        {
            return Error("ROL_NO_AUTORIZADO", "Esta cuenta no cuenta con una invitación de docente.", StatusCodes.Status403Forbidden);
        }

        var restablecimiento = await _userManager.ResetPasswordAsync(usuario, request.Token, request.Contrasena);
        if (!restablecimiento.Succeeded)
        {
            return ErrorIdentity(restablecimiento, StatusCodes.Status400BadRequest, "TOKEN_INVITACION_INVALIDO");
        }

        usuario.EmailConfirmed = true;
        await _userManager.UpdateAsync(usuario);

        var nombreDocenteDesglosado = $"{request.Nombres} {request.PrimerApellido} {request.SegundoApellido}".Trim();
        var nombreFinalDocente = !string.IsNullOrWhiteSpace(request.Nombre)
            ? request.Nombre.Trim()
            : (!string.IsNullOrWhiteSpace(nombreDocenteDesglosado) ? nombreDocenteDesglosado : string.Empty);

        if (!string.IsNullOrWhiteSpace(nombreFinalDocente))
        {
            var profesor = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == usuario.Id);
            if (profesor != null)
            {
                profesor.Nombre = nombreFinalDocente;
                await _contexto.SaveChangesAsync();
            }
            else
            {
                await _contexto.Profesores.AddAsync(new Profesor
                {
                    IdUsuario = usuario.Id,
                    Nombre = nombreFinalDocente,
                    Activo = true,
                });
                await _contexto.SaveChangesAsync();
            }
        }

        var roles = await _userManager.GetRolesAsync(usuario);
        var nombreCortoDocente = ObtenerPrimerNombreYPrimerApellido(nombreFinalDocente, usuario.Email);
        var inicialDocente = ObtenerInicialPrimerApellido(nombreFinalDocente, usuario.Email);
        var tokenJwt = _tokenService.Generar(usuario, roles, nombreCortoDocente, inicialDocente);
        var expiracion = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiracionMinutos"));

        return Ok(new RegistroUsuarioResponse(
            usuario.Id,
            null,
            tokenJwt,
            expiracion,
            usuario.Email ?? string.Empty,
            roles.ToArray(),
            nombreFinalDocente));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var correo = request.Correo?.Trim();
        var usuario = string.IsNullOrWhiteSpace(correo)
            ? null
            : await _userManager.FindByEmailAsync(correo);

        if (usuario is null)
        {
            return Error(
                "CREDENCIALES_INVALIDAS",
                "Correo o contraseña incorrectos.",
                StatusCodes.Status401Unauthorized);
        }

        var verificacion = await _signInManager.CheckPasswordSignInAsync(
            usuario,
            request.Contrasena,
            lockoutOnFailure: false);

        if (!verificacion.Succeeded)
        {
            return Error(
                "CREDENCIALES_INVALIDAS",
                "Correo o contraseña incorrectos.",
                StatusCodes.Status401Unauthorized);
        }

        if (_configuration.GetValue<bool>("Identity:RequerirCuentaValidada") && !usuario.EmailConfirmed)
        {
            return Error(
                "CUENTA_NO_VALIDADA",
                "Debe validar su cuenta antes de iniciar sesión.",
                StatusCodes.Status403Forbidden);
        }

        var roles = await _userManager.GetRolesAsync(usuario);

        if (!string.IsNullOrWhiteSpace(request.RolEsperado))
        {
            var area = request.RolEsperado.Trim().ToLowerInvariant();
            var autorizada = area switch
            {
                "estudiante" or "alumno" => roles.Contains(ConstantesRol.Alumno),
                "docente" or "profesor" => roles.Contains(ConstantesRol.Docente),
                "institucion" or "personal" => roles.Any(r => r is ConstantesRol.DirectorDePosgrado or ConstantesRol.Administrador or ConstantesRol.AdministradorDeTi),
                _ => true
            };

            if (!autorizada)
            {
                var rolAsignado = roles.FirstOrDefault() ?? "otro perfil";
                return Error(
                    "ROL_NO_CORRESPONDE",
                    $"Esta cuenta pertenece al perfil de '{rolAsignado}'. No está autorizada para ingresar desde esta sección.",
                    StatusCodes.Status403Forbidden);
            }
        }

        var alumno = await _contexto.Alumnos.FirstOrDefaultAsync(a => a.IdUsuario == usuario.Id);
        if (alumno != null && string.IsNullOrWhiteSpace(alumno.NumeroControl) && !string.IsNullOrWhiteSpace(usuario.Email))
        {
            var prefijo = usuario.Email.Split('@')[0];
            if (long.TryParse(prefijo, out _))
            {
                alumno.NumeroControl = prefijo;
                await _contexto.SaveChangesAsync();
            }
        }

        var nombre = await ObtenerNombreUsuarioAsync(usuario.Id);
        var nombreCorto = ObtenerPrimerNombreYPrimerApellido(nombre ?? string.Empty, usuario.Email);
        var inicialApellido = ObtenerInicialPrimerApellido(nombre ?? string.Empty, usuario.Email);

        var token = _tokenService.Generar(usuario, roles, nombreCorto, inicialApellido);
        var expiracion = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiracionMinutos"));

        return Ok(new LoginResponse(
            token,
            expiracion,
            usuario.Id,
            usuario.Email ?? string.Empty,
            roles.ToArray(),
            nombre,
            nombreCorto,
            inicialApellido));
    }

    [HttpPost("login-invitado")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginInvitado([FromBody] LoginInvitadoRequest? request = null)
    {
        var correoSolicitado = request?.Correo?.Trim();

        // Si el invitado ingresa su correo para iniciar sesión en su cuenta de invitado
        if (!string.IsNullOrWhiteSpace(correoSolicitado))
        {
            if (!correoSolicitado.Contains('@') || correoSolicitado.Equals("invitado@upt.edu.mx", StringComparison.OrdinalIgnoreCase))
            {
                return Error(
                    "CORREO_INVALIDO",
                    "Ingrese un correo electrónico válido.",
                    StatusCodes.Status400BadRequest);
            }

            var usuarioRegistrado = await _userManager.FindByEmailAsync(correoSolicitado);
            if (usuarioRegistrado is null)
            {
                return Error(
                    "CUENTA_NO_ENCONTRADA",
                    "No encontramos una cuenta de invitado con este correo. Por favor crea tu cuenta primero o entra sin iniciar sesión.",
                    StatusCodes.Status404NotFound);
            }

            var rolesUsuario = await _userManager.GetRolesAsync(usuarioRegistrado);
            if (!rolesUsuario.Contains(ConstantesRol.Invitado))
            {
                var rolPrincipal = rolesUsuario.FirstOrDefault() ?? "otro perfil";
                return Error(
                    "ROL_NO_CORRESPONDE",
                    $"Esta cuenta pertenece al perfil de '{rolPrincipal}'. Por favor ingresa desde tu sección correspondiente.",
                    StatusCodes.Status403Forbidden);
            }

            var tokenRegistrado = _tokenService.Generar(usuarioRegistrado, new[] { ConstantesRol.Invitado }, "Invitado", "I");
            var expRegistrado = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiracionMinutos"));

            return Ok(new LoginResponse(
                tokenRegistrado,
                expRegistrado,
                usuarioRegistrado.Id,
                usuarioRegistrado.Email ?? correoSolicitado,
                new[] { ConstantesRol.Invitado },
                "Invitado",
                "Invitado",
                "I"));
        }

        // Acceso sin iniciar sesión (modo anónimo, sin poder guardar favoritos)
        const string correoInvitado = "invitado@upt.edu.mx";
        var usuario = await _userManager.FindByEmailAsync(correoInvitado);
        if (usuario is null)
        {
            usuario = new Usuario
            {
                UserName = correoInvitado,
                Email = correoInvitado,
                EmailConfirmed = true,
            };
            var creacion = await _userManager.CreateAsync(usuario, "Invitado1234!");
            if (creacion.Succeeded)
            {
                await _userManager.AddToRoleAsync(usuario, ConstantesRol.Invitado);
            }
        }

        var roles = new[] { ConstantesRol.Invitado };
        var token = _tokenService.Generar(usuario, roles, "Invitado", "I");
        var expiracion = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiracionMinutos"));

        return Ok(new LoginResponse(token, expiracion, usuario.Id, correoInvitado, roles, "Invitado", "Invitado", "I"));
    }

    [HttpPost("validar-cuenta")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidarCuenta(ValidarCuentaRequest request)
    {
        var usuario = await _userManager.FindByIdAsync(request.IdUsuario.ToString());
        if (usuario is null)
        {
            return Error(
                "CUENTA_NO_ENCONTRADA",
                "La cuenta no existe.",
                StatusCodes.Status400BadRequest);
        }

        var confirmacion = await _userManager.ConfirmEmailAsync(usuario, request.Token);
        if (!confirmacion.Succeeded)
        {
            return ErrorIdentity(confirmacion, StatusCodes.Status400BadRequest, "TOKEN_INVALIDO");
        }

        return Ok();
    }

    [HttpPost("recuperar-contrasena")]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaRequest request)
    {
        var correo = request.Correo?.Trim();
        if (string.IsNullOrWhiteSpace(correo))
        {
            return Error(
                "CORREO_OBLIGATORIO",
                "El correo es obligatorio.",
                StatusCodes.Status400BadRequest);
        }

        var usuario = await _userManager.FindByEmailAsync(correo);
        if (usuario is null)
        {
            return Error(
                "CUENTA_NO_ENCONTRADA",
                "La cuenta no existe.",
                StatusCodes.Status400BadRequest);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var tokenDev = _environment.IsDevelopment() ? token : null;

        var nombreDestinatario = correo;
        var alumnoRecuperacion = await _contexto.Alumnos.FirstOrDefaultAsync(a => a.IdUsuario == usuario.Id);
        if (alumnoRecuperacion != null && !string.IsNullOrWhiteSpace(alumnoRecuperacion.Nombre))
        {
            nombreDestinatario = alumnoRecuperacion.Nombre;
        }
        else
        {
            var profesorRecuperacion = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == usuario.Id);
            if (profesorRecuperacion != null && !string.IsNullOrWhiteSpace(profesorRecuperacion.Nombre))
            {
                nombreDestinatario = profesorRecuperacion.Nombre;
            }
        }

        var enlace = $"{BaseFrontend()}/restablecer-contrasena?id={usuario.Id}&token={Uri.EscapeDataString(token)}";
        var cuerpoHtml = PlantillasCorreo.GenerarHtmlRecuperarContrasena(nombreDestinatario, usuario.Id, enlace, token);

        try
        {
            await _correo.EnviarAsync(correo, "SIRET - UPT: Restablecer tu contraseña", cuerpoHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de recuperación a {Correo}.", correo);
            return Error("ERROR_ENVIO_CORREO", "No se pudo enviar el correo de recuperación. Verifique la configuración del servidor de correo.", StatusCodes.Status500InternalServerError);
        }

        return Ok(new RecuperarContrasenaResponse(usuario.Id, tokenDev));
    }

    [HttpPost("reenviar-confirmacion")]
    [AllowAnonymous]
    public async Task<IActionResult> ReenviarConfirmacion(ReenviarConfirmacionRequest request)
    {
        var correo = request.Correo?.Trim();
        if (string.IsNullOrWhiteSpace(correo))
        {
            return Error("CORREO_OBLIGATORIO", "El correo es obligatorio.", StatusCodes.Status400BadRequest);
        }

        var usuario = await _userManager.FindByEmailAsync(correo);
        if (usuario is null)
        {
            return Error("CUENTA_NO_ENCONTRADA", "No existe ninguna cuenta asociada a este correo.", StatusCodes.Status400BadRequest);
        }

        if (usuario.EmailConfirmed)
        {
            return Error("CUENTA_YA_VALIDADA", "La cuenta ya se encuentra validada. Puede iniciar sesión directamente.", StatusCodes.Status400BadRequest);
        }

        var tokenValidacion = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
        var tokenDev = _environment.IsDevelopment() ? tokenValidacion : null;

        var nombreValidacion = correo;
        var alumnoValidacion = await _contexto.Alumnos.FirstOrDefaultAsync(a => a.IdUsuario == usuario.Id);
        if (alumnoValidacion != null && !string.IsNullOrWhiteSpace(alumnoValidacion.Nombre))
        {
            nombreValidacion = alumnoValidacion.Nombre;
        }
        else
        {
            var profesorValidacion = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == usuario.Id);
            if (profesorValidacion != null && !string.IsNullOrWhiteSpace(profesorValidacion.Nombre))
            {
                nombreValidacion = profesorValidacion.Nombre;
            }
        }

        var enlace = $"{BaseFrontend()}/validar-cuenta?id={usuario.Id}&token={Uri.EscapeDataString(tokenValidacion)}";
        var cuerpoHtml = PlantillasCorreo.GenerarHtmlValidacionCuenta(nombreValidacion, usuario.Id, enlace, tokenValidacion);

        try
        {
            await _correo.EnviarAsync(correo, "SIRET - UPT: Valida tu cuenta de correo", cuerpoHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reenviar correo de validación a {Correo}.", correo);
            return Error("ERROR_ENVIO_CORREO", "No se pudo enviar el correo de validación. Verifique la configuración del servidor de correo.", StatusCodes.Status500InternalServerError);
        }

        return Ok(new { idUsuario = usuario.Id, tokenDev });
    }

    [HttpPost("probar-correo")]
    [AllowAnonymous]
    public async Task<IActionResult> ProbarCorreo(ProbarCorreoRequest request)
    {
        var correo = request.Destinatario?.Trim();
        if (string.IsNullOrWhiteSpace(correo))
        {
            return Error("CORREO_OBLIGATORIO", "El correo destinatario es obligatorio.", StatusCodes.Status400BadRequest);
        }

        try
        {
            var cuerpoHtml = PlantillasCorreo.GenerarHtmlPrueba(correo);
            await _correo.EnviarAsync(correo, "SIRET - UPT: Correo de prueba del sistema", cuerpoHtml);
            return Ok(new { exitoso = true, mensaje = $"Correo de prueba enviado correctamente a {correo}." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la prueba de correo hacia {Destinatario}: {Mensaje}", correo, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                exitoso = false,
                mensaje = $"Error al enviar correo: {ex.Message}",
                detalle = ex.ToString()
            });
        }
    }

    [HttpPost("restablecer-contrasena")]
    [AllowAnonymous]
    public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NuevaContrasena))
        {
            return Error(
                "CONTRASENA_OBLIGATORIA",
                "La nueva contraseña es obligatoria.",
                StatusCodes.Status400BadRequest);
        }

        var usuario = await _userManager.FindByIdAsync(request.IdUsuario.ToString());
        if (usuario is null)
        {
            return Error(
                "CUENTA_NO_ENCONTRADA",
                "La cuenta no existe.",
                StatusCodes.Status400BadRequest);
        }

        var restablecimiento = await _userManager.ResetPasswordAsync(usuario, request.Token, request.NuevaContrasena);
        if (!restablecimiento.Succeeded)
        {
            return ErrorIdentity(restablecimiento, StatusCodes.Status400BadRequest, "TOKEN_INVALIDO");
        }

        return Ok();
    }

    [Authorize]
    [HttpGet("mi-perfil")]
    public async Task<IActionResult> ObtenerMiPerfil()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var idUsuario))
        {
            return Unauthorized();
        }

        var usuario = await _userManager.FindByIdAsync(idUsuario.ToString());
        if (usuario is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(usuario);
        var alumno = await _contexto.Alumnos.FirstOrDefaultAsync(a => a.IdUsuario == idUsuario);
        if (alumno != null && string.IsNullOrWhiteSpace(alumno.NumeroControl) && !string.IsNullOrWhiteSpace(usuario.Email))
        {
            var prefijo = usuario.Email.Split('@')[0];
            if (long.TryParse(prefijo, out _))
            {
                alumno.NumeroControl = prefijo;
                await _contexto.SaveChangesAsync();
            }
        }
        var profesor = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);

        var nombreCompleto = alumno?.Nombre ?? profesor?.Nombre ?? (usuario.Email ?? "");
        var desglosado = DesglosarNombre(nombreCompleto);
        var primerNombre = desglosado.Nombres.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var primerApellido = desglosado.PrimerApellido;
        var segundoApellido = desglosado.SegundoApellido;
        var nombreCorto = ObtenerPrimerNombreYPrimerApellido(nombreCompleto, usuario.Email);
        var inicialApellido = ObtenerInicialPrimerApellido(nombreCompleto, usuario.Email);

        return Ok(new MiPerfilResponse(
            idUsuario,
            usuario.Email ?? string.Empty,
            nombreCompleto,
            primerNombre,
            primerApellido,
            segundoApellido,
            nombreCorto,
            inicialApellido,
            alumno?.NumeroControl,
            roles.ToArray()));
    }

    [Authorize]
    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaRequest request)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var idUsuario))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.NuevaContrasena) || request.NuevaContrasena.Length < 8)
        {
            return Error("CONTRASENA_INVALIDA", "La nueva contraseña debe tener al menos 8 caracteres.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.ContrasenaActual))
        {
            return Error("CONTRASENA_ACTUAL_OBLIGATORIA", "Debe ingresar su contraseña actual.", StatusCodes.Status400BadRequest);
        }

        var usuario = await _userManager.FindByIdAsync(idUsuario.ToString());
        if (usuario is null)
        {
            return NotFound();
        }

        var resultado = await _userManager.ChangePasswordAsync(usuario, request.ContrasenaActual, request.NuevaContrasena);
        if (!resultado.Succeeded)
        {
            return ErrorIdentity(resultado, StatusCodes.Status400BadRequest, "ERROR_CAMBIO_CONTRASENA");
        }

        return Ok(new { exitoso = true, mensaje = "Contraseña actualizada exitosamente." });
    }

    [Authorize]
    [HttpPost("solicitar-restablecimiento-correo")]
    public async Task<IActionResult> SolicitarRestablecimientoCorreo()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var idUsuario))
        {
            return Unauthorized();
        }

        var usuario = await _userManager.FindByIdAsync(idUsuario.ToString());
        if (usuario?.Email is null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var nombre = await ObtenerNombreUsuarioAsync(usuario.Id) ?? usuario.Email;
        var enlace = $"{BaseFrontend()}/restablecer-contrasena?id={usuario.Id}&token={Uri.EscapeDataString(token)}";
        var cuerpoHtml = PlantillasCorreo.GenerarHtmlRecuperarContrasena(nombre, usuario.Id, enlace, token);

        try
        {
            await _correo.EnviarAsync(usuario.Email, "SIRET - UPT: Restablecer tu contraseña", cuerpoHtml);
            return Ok(new { exitoso = true, mensaje = $"Se ha enviado el enlace de restablecimiento a {usuario.Email}." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar enlace de restablecimiento a {Correo}.", usuario.Email);
            return Error("ERROR_ENVIO_CORREO", "No se pudo enviar el correo de restablecimiento.", StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<string?> ObtenerNombreUsuarioAsync(int idUsuario)
    {
        var alumno = await _contexto.Alumnos.FirstOrDefaultAsync(a => a.IdUsuario == idUsuario);
        if (!string.IsNullOrWhiteSpace(alumno?.Nombre)) return alumno.Nombre;

        var profesor = await _contexto.Profesores.FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);
        if (!string.IsNullOrWhiteSpace(profesor?.Nombre)) return profesor.Nombre;

        return null;
    }

    public static (string Nombres, string PrimerApellido, string? SegundoApellido) DesglosarNombre(string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto)) return ("", "", null);
        var limpio = nombreCompleto.Trim();
        foreach (var prefijo in new[] { "Dr.", "Dra.", "Mtro.", "Mtra.", "Ing.", "Lic.", "Prof.", "Dr ", "Dra ", "Mtro ", "Mtra ", "Ing ", "Lic " })
        {
            if (limpio.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            {
                limpio = limpio[prefijo.Length..].Trim();
            }
        }

        var partes = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return ("", "", null);
        if (partes.Length == 1) return (partes[0], "", null);
        if (partes.Length == 2) return (partes[0], partes[1], null);
        if (partes.Length == 3) return (partes[0], partes[1], partes[2]);
        if (partes.Length == 4) return ($"{partes[0]} {partes[1]}", partes[2], partes[3]);

        return (string.Join(" ", partes[..^2]), partes[^2], partes[^1]);
    }

    public static string ObtenerPrimerNombreYPrimerApellido(string nombreCompleto, string? correo = null)
    {
        var desglosado = DesglosarNombre(nombreCompleto);
        var primerNombre = desglosado.Nombres.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var primerApellido = desglosado.PrimerApellido;

        if (!string.IsNullOrWhiteSpace(primerNombre) && !string.IsNullOrWhiteSpace(primerApellido))
        {
            return $"{primerNombre} {primerApellido}";
        }

        if (!string.IsNullOrWhiteSpace(primerNombre)) return primerNombre;

        if (!string.IsNullOrWhiteSpace(correo) && correo.Contains('@'))
        {
            var local = correo.Split('@')[0];
            if (local.Contains('.'))
            {
                var pedazos = local.Split('.');
                if (pedazos.Length >= 2 && pedazos[0].Length > 0 && pedazos[1].Length > 0)
                {
                    var n = char.ToUpperInvariant(pedazos[0][0]) + pedazos[0][1..];
                    var a = char.ToUpperInvariant(pedazos[1][0]) + pedazos[1][1..];
                    return $"{n} {a}";
                }
            }
        }

        return !string.IsNullOrWhiteSpace(nombreCompleto) ? nombreCompleto.Trim() : (correo ?? "");
    }

    public static string ObtenerInicialPrimerApellido(string nombreCompleto, string? correo = null)
    {
        var desglosado = DesglosarNombre(nombreCompleto);
        if (!string.IsNullOrWhiteSpace(desglosado.PrimerApellido))
        {
            return desglosado.PrimerApellido.Trim()[0].ToString().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(correo) && correo.Contains('@'))
        {
            var local = correo.Split('@')[0];
            if (local.Contains('.'))
            {
                var pedazos = local.Split('.');
                if (pedazos.Length >= 2 && pedazos[1].Length > 0)
                {
                    return pedazos[1].Trim()[0].ToString().ToUpperInvariant();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(desglosado.Nombres))
        {
            return desglosado.Nombres.Trim()[0].ToString().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(correo))
        {
            return correo.Trim()[0].ToString().ToUpperInvariant();
        }

        return "U";
    }

    private string BaseFrontend()
    {
        if (Request?.Headers != null)
        {
            if (Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrWhiteSpace(origin))
            {
                return origin.ToString().TrimEnd('/');
            }
            if (Request.Headers.TryGetValue("Referer", out var referer) && !string.IsNullOrWhiteSpace(referer))
            {
                try
                {
                    var uri = new Uri(referer.ToString());
                    return $"{uri.Scheme}://{uri.Authority}".TrimEnd('/');
                }
                catch { }
            }
        }
        return _configuration["Url:BaseFrontend"] ?? "http://localhost:8081";
    }

    private IActionResult Error(string codigo, string mensaje, int statusCode) =>
        StatusCode(statusCode, new { code = codigo, message = mensaje, details = (string[]?)null });

    private IActionResult ErrorIdentity(IdentityResult resultado, int statusCode, string codigo)
    {
        var mensajes = resultado.Errors
            .Select(e => e.Description)
            .Distinct()
            .ToArray();

        return StatusCode(statusCode, new
        {
            code = codigo,
            message = string.Join(" ", mensajes),
            details = resultado.Errors.Select(e => e.Code).ToArray(),
        });
    }
}