using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class HistorialEstadoTesis
{
    public int Id { get; set; }
    public Guid Folio { get; set; }
    public TesisEstado EstadoAnterior { get; set; }
    public TesisEstado EstadoNuevo { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Observacion { get; set; }

    public Tesis? Tesis { get; set; }
    public Usuario? Usuario { get; set; }
}
