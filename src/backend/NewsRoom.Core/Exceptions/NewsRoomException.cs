namespace NewsRoom.Core.Exceptions;

public class NewsRoomException : Exception
{
    public string? CorrelationId { get; }

    public NewsRoomException(string message, string? correlationId = null)
        : base(message)
    {
        CorrelationId = correlationId;
    }

    public NewsRoomException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException)
    {
        CorrelationId = correlationId;
    }
}

public class NewsSourceException : NewsRoomException
{
    public NewsSourceException(string message, string? correlationId = null)
        : base(message, correlationId) { }

    public NewsSourceException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException, correlationId) { }
}

public class ScriptGenerationException : NewsRoomException
{
    public ScriptGenerationException(string message, string? correlationId = null)
        : base(message, correlationId) { }

    public ScriptGenerationException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException, correlationId) { }
}

public class TtsGenerationException : NewsRoomException
{
    public TtsGenerationException(string message, string? correlationId = null)
        : base(message, correlationId) { }

    public TtsGenerationException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException, correlationId) { }
}

public class AvatarGenerationException : NewsRoomException
{
    public AvatarGenerationException(string message, string? correlationId = null)
        : base(message, correlationId) { }

    public AvatarGenerationException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException, correlationId) { }
}

public class BRollGenerationException : NewsRoomException
{
    public BRollGenerationException(string message, string? correlationId = null)
        : base(message, correlationId) { }

    public BRollGenerationException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException, correlationId) { }
}

public class VideoCompositionException : NewsRoomException
{
    public VideoCompositionException(string message, string? correlationId = null)
        : base(message, correlationId) { }

    public VideoCompositionException(string message, Exception innerException, string? correlationId = null)
        : base(message, innerException, correlationId) { }
}
