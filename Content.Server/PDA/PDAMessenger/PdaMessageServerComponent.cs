namespace Content.Server.PDA.PDAMessenger;

[RegisterComponent]
[Access(typeof(PdaMessageServerSystem))]
public sealed class PdaMessageServerComponent : Component
{
    /// <summary>
    /// Known PDA messengers in network by address with messenger names
    /// </summary>
    [ViewVariables]
    public Dictionary<string, string> KnownPDAMessengers = new();

    /// <summary>
    /// Queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField("messageQueue")]
    public Queue<Dictionary<string,string>> MessageQueue { get; } = new();

    /// <summary>
    /// Queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField("messageList")]
    public List<Dictionary<string,string>> MessageList { get; } = new();

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

}
