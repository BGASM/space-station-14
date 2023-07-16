using System.Linq;
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
public sealed class PdaRefreshMessage : BoundUserInterfaceMessage
{
    public PdaRefreshMessage() { }
}

[Serializable, NetSerializable]
public sealed class PdaMessage : BoundUserInterfaceMessage
{
    public DateTime SentAt { get; }
    public string SenderName { get; }
    public string SenderAddress { get; }
    public List<Recipient> RecipientList { get; }
    public string ReceiverName { get; }
    public string ReceiverAddress { get; }
    public string Message { get; }

    public PdaMessage(
        List<Recipient> recipientList,
        string receiverName,
        string receiverAddress,
        string message,
        DateTime? sentAt = default,
        string? senderName = default,
        string? senderAddress = default)
    {
        RecipientList = recipientList;
        ReceiverName = receiverName;
        ReceiverAddress = receiverAddress;
        Message = message;
        SentAt = sentAt ?? DateTime.Now;
        SenderName = senderName ?? "";
        SenderAddress = senderAddress ?? "";
    }
}

[Serializable, NetSerializable]
public sealed class PdaConversation
{
    public List<Recipient> RecipientList { get; }
    public List<MessageMeta> MessageList { get; }
    public string ConversationId { get; }

    public PdaConversation(List<Recipient> recipientList, List<MessageMeta> messageList)
    {
        RecipientList = recipientList;
        MessageList = messageList;
        ConversationId = String.Join("", recipientList.SelectMany(r => r.Name).ToList());
    }

    public MessageMeta LastMessage()
    {
        MessageList.Sort((x,y) => DateTime.Compare(x.SentAt, y.SentAt));
        return MessageList.Last();
    }

}

[Serializable, NetSerializable]
public sealed class MessageMeta
{
    public string Message { get; }
    public DateTime SentAt { get; }
    public string SenderName { get; }

    public MessageMeta(string message, DateTime sentAt, string senderName)
    {
        Message = message;
        SentAt = sentAt;
        SenderName = senderName;
    }
}

[Serializable, NetSerializable]
public sealed class Recipient
{
    public string Name { get; }
    public string Address { get; }

    public Recipient(string name, string address)
    {
        Name = name;
        Address = address;
    }
}

[Serializable, NetSerializable]
public sealed class KnownPdaMessengers
{

}
