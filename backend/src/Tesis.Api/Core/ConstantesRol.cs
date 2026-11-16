namespace Tesis.Api.Core;

public static class ConstantesRol
{
    public const string Invitado = "Invitado";
    public const string Alumno = "Alumno";
    public const string Docente = "Docente";
    public const string DirectorDePosgrado = "Director de posgrado";
    public const string Administrador = "Administrador";
    public const string AdministradorDeTi = "Administrador de TI";

    public static readonly string[] Todos =
    {
        Invitado,
        Alumno,
        Docente,
        DirectorDePosgrado,
        Administrador,
        AdministradorDeTi,
    };

    public static readonly string[] AutoRegistrables = { Invitado, Alumno };
}