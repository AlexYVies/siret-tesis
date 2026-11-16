using Tesis.Domain.Enums;

namespace Tesis.Application.Modelos;

public record ProcesarConfirmacionComando(
    Guid Folio,
    TipoConfirmacion Tipo,
    EstadoConfirmacion Decision);