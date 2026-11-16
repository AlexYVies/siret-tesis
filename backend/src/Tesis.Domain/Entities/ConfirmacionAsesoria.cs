using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class ConfirmacionAsesoria : EntidadAuditable
{
    public int IdConfirmacion { get; set; }
    public Guid Folio { get; set; }
    public int IdProfesor { get; set; }
    public TipoConfirmacion Tipo { get; set; }
    public EstadoConfirmacion Estado { get; private set; } = EstadoConfirmacion.Pendiente;
    public DateTime Fecha { get; set; }

    public Tesis? Tesis { get; set; }
    public Profesor? Profesor { get; set; }

    public void Confirmar()
    {
        Guardia.Exigir(Estado == EstadoConfirmacion.Pendiente, "La confirmación de asesoría ya fue respondida.");
        Estado = EstadoConfirmacion.Confirmada;
        Fecha = DateTime.UtcNow;
    }

    public void Rechazar()
    {
        Guardia.Exigir(Estado == EstadoConfirmacion.Pendiente, "La confirmación de asesoría ya fue respondida.");
        Estado = EstadoConfirmacion.Rechazada;
        Fecha = DateTime.UtcNow;
    }
}