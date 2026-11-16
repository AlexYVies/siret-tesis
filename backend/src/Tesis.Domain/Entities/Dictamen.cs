using Tesis.Domain.Common;
using Tesis.Domain.Enums;

namespace Tesis.Domain.Entities;

public class Dictamen : EntidadAuditable
{
    public int IdDictamen { get; set; }
    public Guid Folio { get; set; }
    public int IdDirector { get; set; }
    public ResultadoDictamen Resultado { get; set; }
    public string? Observaciones { get; set; }
    public DateTime Fecha { get; set; }

    public Tesis? Tesis { get; set; }
}