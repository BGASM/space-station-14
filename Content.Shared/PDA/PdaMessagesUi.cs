using Robust.Shared.Serialization;

namespace Content.Shared.PDA;

[Serializable, NetSerializable]
public sealed class PdaToggleFlashlightMessage : BoundUserInterfaceMessage
{
    public PdaToggleFlashlightMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaShowRingtoneMessage : BoundUserInterfaceMessage
{
    public PdaShowRingtoneMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaShowUplinkMessage : BoundUserInterfaceMessage
{
    public PdaShowUplinkMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaLockUplinkMessage : BoundUserInterfaceMessage
{
    public PdaLockUplinkMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaShowMusicMessage : BoundUserInterfaceMessage
{
    public PdaShowMusicMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
{
    public PdaRequestUpdateInterfaceMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaSendMessage : BoundUserInterfaceMessage
{
    public PdaSendMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaRefreshMessage : BoundUserInterfaceMessage
{
    public PdaRefreshMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaTextMessage : BoundUserInterfaceMessage
{

    public DateTime SentAt { get; }
    public string SenderName { get; }
    public string ReceiverName { get; }
    public string Message { get; }

    public PdaTextMessage(string senderName, string receiverName, string message, DateTime? sentAt = default)
    {
        SentAt = sentAt ?? DateTime.Now;
        SenderName = senderName;
        ReceiverName = receiverName;
        Message = message;
    }
}
