using Tesis.Domain.Enums;

namespace Tesis.Application.Modelos;

public record RegistrarTesisComando(
    string Titulo,
    NivelPosgrado Nivel,
    string Programa,
    int IdAsesor,
    int? IdCoAsesor);