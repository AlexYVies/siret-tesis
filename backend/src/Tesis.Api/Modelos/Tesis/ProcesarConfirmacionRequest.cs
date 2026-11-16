using Tesis.Domain.Enums;

namespace Tesis.Api.Modelos.Tesis;

public record ProcesarConfirmacionRequest(TipoConfirmacion Tipo, EstadoConfirmacion Decision);