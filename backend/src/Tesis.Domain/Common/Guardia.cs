namespace Tesis.Domain.Common;

public static class Guardia
{
    public static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
        {
            throw new InvalidOperationException(mensaje);
        }
    }
}