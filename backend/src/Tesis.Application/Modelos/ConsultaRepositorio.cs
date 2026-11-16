using Tesis.Domain.Enums;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Application.Modelos;

public sealed record ConsultaRepositorio(
    string? Termino,
    NivelPosgrado? Nivel,
    string? Programa,
    string? Autor,
    OrdenRepositorio Orden,
    int Pagina,
    int Tamano);

public sealed record ResultadoRepositorio(
    IReadOnlyList<EntidadTesis> Datos,
    int Total);