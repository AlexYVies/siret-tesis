using System.Text.Json;
using System.Text.Json.Serialization;
using Tesis.Domain.Enums;

namespace Tesis.Api.Serializacion;

public sealed class ConvertidorEnumEspanol<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly IReadOnlyDictionary<string, TEnum> _porTexto;
    private readonly IReadOnlyDictionary<TEnum, string> _porValor;

    public ConvertidorEnumEspanol(IReadOnlyDictionary<string, TEnum> catalogo)
    {
        _porTexto = catalogo
            .GroupBy(par => par.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().Value, StringComparer.OrdinalIgnoreCase);
        _porValor = catalogo.ToDictionary(par => par.Value, par => par.Key);
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var texto = reader.GetString();
            if (texto is not null && _porTexto.TryGetValue(texto, out var valor))
            {
                return valor;
            }

            if (texto is not null && Enum.TryParse<TEnum>(texto, ignoreCase: true, out var porNombre))
            {
                return porNombre;
            }
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var valorNumerico))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), valorNumerico);
        }

        var leido = reader.GetString();
        throw new JsonException($"Valor no reconocido para {typeof(TEnum).Name}: '{leido}'.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_porValor.TryGetValue(value, out var texto) ? texto : value.ToString());
    }
}

public static class SerializacionEnumsEspanol
{
    public static IEnumerable<JsonConverter> ConstruirConvertidores()
    {
        yield return new ConvertidorEnumEspanol<NivelPosgrado>(new Dictionary<string, NivelPosgrado>
        {
            ["Maestría"] = NivelPosgrado.Maestria,
            ["Doctorado"] = NivelPosgrado.Doctorado,
        });

        yield return new ConvertidorEnumEspanol<TipoConfirmacion>(new Dictionary<string, TipoConfirmacion>
        {
            ["Asesor"] = TipoConfirmacion.Asesor,
            ["Co-asesor"] = TipoConfirmacion.CoAsesor,
        });

        yield return new ConvertidorEnumEspanol<EstadoConfirmacion>(new Dictionary<string, EstadoConfirmacion>
        {
            ["Pendiente"] = EstadoConfirmacion.Pendiente,
            ["Confirmada"] = EstadoConfirmacion.Confirmada,
            ["Rechazada"] = EstadoConfirmacion.Rechazada,
        });

        yield return new ConvertidorEnumEspanol<ResultadoRevisionIA>(new Dictionary<string, ResultadoRevisionIA>
        {
            ["Válido"] = ResultadoRevisionIA.Valido,
            ["No válido"] = ResultadoRevisionIA.NoValido,
            ["Con observaciones"] = ResultadoRevisionIA.ConObservaciones,
        });

        yield return new ConvertidorEnumEspanol<DecisionRevisionAsesor>(new Dictionary<string, DecisionRevisionAsesor>
        {
            ["Aceptada"] = DecisionRevisionAsesor.Aceptada,
            ["Rechazada para cambios"] = DecisionRevisionAsesor.Rechazada,
        });

        yield return new ConvertidorEnumEspanol<ResultadoDictamen>(new Dictionary<string, ResultadoDictamen>
        {
            ["Aceptada"] = ResultadoDictamen.Aceptada,
            ["Rechazada"] = ResultadoDictamen.Rechazada,
        });

        yield return new ConvertidorEnumEspanol<TesisEstado>(new Dictionary<string, TesisEstado>
        {
            ["Registrada"] = TesisEstado.Registrada,
            ["Confirmada"] = TesisEstado.Confirmada,
            ["En revisión IA"] = TesisEstado.EnRevisionIA,
            ["En revisión del asesor"] = TesisEstado.EnRevisionDelAsesor,
            ["Cambios solicitados"] = TesisEstado.CambiosSolicitados,
            ["En dictamen"] = TesisEstado.EnDictamen,
            ["Aceptada"] = TesisEstado.Aceptada,
            ["Rechazada"] = TesisEstado.Rechazada,
            ["Inactiva"] = TesisEstado.Inactiva,
        });
    }
}