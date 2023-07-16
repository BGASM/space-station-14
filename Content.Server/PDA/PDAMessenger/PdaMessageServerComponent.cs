using Content.Shared.PDA;

namespace Content.Server.PDA.PDAMessenger;

[RegisterComponent]
[Access(typeof(PdaMessageServerSystem))]
public sealed class PdaMessageServerComponent : Component
{
    /// <summary>
    /// Known PDA messengers in network by address with messenger names
    /// </summary>
    [ViewVariables]
    public List<KnownPda> KnownPDAMessengers = new();

    /// <summary>
    ///     Whether the server can become the currently active server. The server being unavailable usually means that it isn't powered
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Available = true;

    /// <summary>
    ///     Whether the server is the currently active server for the station it's on
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Active = true;

    /// <summary>
    /// Queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField("messageQueue")]
    public Queue<PdaMessage> MessageQueue { get; } = new();

}
