namespace Tunora.Core.Exceptions;

public class TierLimitExceededException : Exception
{
    public TierLimitExceededException(string message) : base(message) { }
}
