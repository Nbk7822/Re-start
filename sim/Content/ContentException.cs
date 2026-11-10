namespace Return.Sim.Content;

/// <summary>A content error found at load time. The message names the file and the offending id.</summary>
public sealed class ContentException : Exception
{
    public ContentException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
