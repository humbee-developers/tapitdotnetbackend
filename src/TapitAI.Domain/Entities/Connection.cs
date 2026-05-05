using TapitAI.Domain.Common;
using TapitAI.Domain.Enums;

namespace TapitAI.Domain.Entities;

public class Connection : AggregateRoot
{
    public string SenderUserId { get; private set; } = null!;
    public string ReceiverUserId { get; private set; } = null!;
    public InvitationStatus InvitationStatus { get; private set; } = InvitationStatus.Pending;
    public ParticipantConnectionStatus? SenderConnectionStatus { get; private set; }
    public ParticipantConnectionStatus? ReceiverConnectionStatus { get; private set; }
    public double SenderLocationLat { get; private set; }
    public double SenderLocationLong { get; private set; }
    public double ReceiverLocationLat { get; private set; }
    public double ReceiverLocationLong { get; private set; }
    public string? SenderInvitationMessage { get; private set; }
    public string? ReceiverInvitationMessage { get; private set; }
    public string? SenderConnectionMessage { get; private set; }
    public string? ReceiverConnectionMessage { get; private set; }
    public ConnectionInitiatedVia InitiatedVia { get; private set; }
    public DateTime InvitedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? ConnectedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? ChatChannelId { get; private set; }

    private Connection() { }

    public static Connection Create(
        string senderUserId, string receiverUserId,
        double senderLat, double senderLong,
        double receiverLat, double receiverLong,
        ConnectionInitiatedVia initiatedVia,
        string? senderInvitationMessage,
        int expiryMinutes)
        => new()
        {
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            InvitationStatus = InvitationStatus.Pending,
            SenderLocationLat = senderLat,
            SenderLocationLong = senderLong,
            ReceiverLocationLat = receiverLat,
            ReceiverLocationLong = receiverLong,
            InitiatedVia = initiatedVia,
            SenderInvitationMessage = senderInvitationMessage,
            InvitedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };

    public void Withdraw()
    {
        if (InvitationStatus != InvitationStatus.Pending)
            throw new InvalidOperationException("Can only withdraw a pending invitation.");
        InvitationStatus = InvitationStatus.Withdrawn;
    }

    public void Reject(string? rejectionMessage)
    {
        if (InvitationStatus != InvitationStatus.Pending)
            throw new InvalidOperationException("Can only reject a pending invitation.");
        InvitationStatus = InvitationStatus.Rejected;
        ReceiverInvitationMessage = rejectionMessage;
    }

    public void Accept()
    {
        if (InvitationStatus != InvitationStatus.Pending)
            throw new InvalidOperationException("Can only accept a pending invitation.");
        InvitationStatus = InvitationStatus.Accepted;
        SenderConnectionStatus = ParticipantConnectionStatus.Pending;
        ReceiverConnectionStatus = ParticipantConnectionStatus.Pending;
        AcceptedAt = DateTime.UtcNow;
    }

    public void SenderPass(string? message)
    {
        if (InvitationStatus != InvitationStatus.Accepted)
            throw new InvalidOperationException("Connection must be accepted before passing.");
        SenderConnectionStatus = ParticipantConnectionStatus.Pass;
        SenderConnectionMessage = message;
    }

    public void ReceiverPass(string? message)
    {
        if (InvitationStatus != InvitationStatus.Accepted)
            throw new InvalidOperationException("Connection must be accepted before passing.");
        ReceiverConnectionStatus = ParticipantConnectionStatus.Pass;
        ReceiverConnectionMessage = message;
    }

    public void SenderStartChat(string? message)
    {
        if (InvitationStatus != InvitationStatus.Accepted)
            throw new InvalidOperationException("Connection must be accepted to start chat.");
        SenderConnectionStatus = ParticipantConnectionStatus.Connect;
        SenderConnectionMessage = message;
        TryCompleteConnection();
    }

    public void ReceiverStartChat(string? message)
    {
        if (InvitationStatus != InvitationStatus.Accepted)
            throw new InvalidOperationException("Connection must be accepted to start chat.");
        ReceiverConnectionStatus = ParticipantConnectionStatus.Connect;
        ReceiverConnectionMessage = message;
        TryCompleteConnection();
    }

    public bool IsBothConnected()
        => SenderConnectionStatus == ParticipantConnectionStatus.Connect
        && ReceiverConnectionStatus == ParticipantConnectionStatus.Connect;

    private void TryCompleteConnection()
    {
        if (IsBothConnected()) ConnectedAt = DateTime.UtcNow;
    }

    public void SetChatChannel(string channelId) => ChatChannelId = channelId;

    public void Block() => InvitationStatus = InvitationStatus.Blocked;

    public void Expire()
    {
        if (InvitationStatus == InvitationStatus.Pending)
        {
            InvitationStatus = InvitationStatus.Expired;
        }
        else if (InvitationStatus == InvitationStatus.Accepted)
        {
            if (SenderConnectionStatus == ParticipantConnectionStatus.Pending)
                SenderConnectionStatus = ParticipantConnectionStatus.Expired;
            if (ReceiverConnectionStatus == ParticipantConnectionStatus.Pending)
                ReceiverConnectionStatus = ParticipantConnectionStatus.Expired;
        }
    }
}
