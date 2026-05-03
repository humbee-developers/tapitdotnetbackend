namespace TapitAI.Domain.Constants;

public static class HubEvents
{
    public const string ConnectionRequestReceived = "ConnectionRequestReceived";
    public const string ConnectionRequestSent = "ConnectionRequestSent";
    public const string ConnectionWithdrawn = "ConnectionWithdrawn";
    public const string ConnectionRejected = "ConnectionRejected";
    public const string ConnectionAccepted = "ConnectionAccepted";
    public const string ConnectionPassed = "ConnectionPassed";
    public const string WaitingForPartner = "WaitingForPartner";
    public const string ChatStarted = "ChatStarted";
    public const string TapStatusChanged = "TapStatusChanged";
}
