using Tesis.Domain.Enums;

namespace Tesis.Api.Modelos.Tesis;

public record RegistrarTesisRequest(
    string Titulo,
    NivelPosgrado Nivel,
    string Programa,
    int IdAsesor,
    int? IdCoAsesor);