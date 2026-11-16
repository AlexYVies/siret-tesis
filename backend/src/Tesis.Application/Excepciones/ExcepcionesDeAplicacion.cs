namespace Tesis.Application.Excepciones;

public sealed class EntidadNoEncontradaException : Exception
{
    public EntidadNoEncontradaException(string message) : base(message)
    {
    }
}

public sealed class SolicitudInvalidaException : Exception
{
    public SolicitudInvalidaException(string message) : base(message)
    {
    }
}

public sealed class OperacionNoPermitidaException : Exception
{
    public OperacionNoPermitidaException(string message) : base(message)
    {
    }
}

public sealed class ConflictoDeNegocioException : Exception
{
    public ConflictoDeNegocioException(string message) : base(message)
    {
    }
}

public sealed class ServicioNoDisponibleException : Exception
{
    public ServicioNoDisponibleException(string message) : base(message)
    {
    }
}