using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PDA
{
    [RegisterComponent, NetworkedComponent]
    public sealed class PdaComponent : Component
    {
        public const string PdaIdSlotId = "PDA-id";
        public const string PdaPenSlotId = "PDA-pen";

        /// <summary>
        /// The base PDA sprite state, eg. "pda", "pda-clown"
        /// </summary>
        [DataField("state")]
        public string? State;

        [DataField("idSlot")]
        public ItemSlot IdSlot = new();

        [DataField("penSlot")]
        public ItemSlot PenSlot = new();

        // Really this should just be using ItemSlot.StartingItem. However, seeing as we have so many different starting
        // PDA's and no nice way to inherit the other fields from the ItemSlot data definition, this makes the yaml much
        // nicer to read.
        [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? IdCard;

        [ViewVariables] public IdCardComponent? ContainedId;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables] public string OwnerName = "Unknown";
        [ViewVariables] public string? StationName;
        [ViewVariables] public string? StationAlertLevel;
        [ViewVariables] public Color StationAlertColor = Color.White;

        /// <summary>
        ///     Next time when sensor updated owners status
        /// </summary>
        [DataField("nextUpdate", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdate = TimeSpan.Zero;

        /// <summary>
        ///     How often does sensor update its owners status (in seconds). Limited by the system update rate.
        /// </summary>
        [DataField("updateRate")]
        public TimeSpan UpdateRate = TimeSpan.FromSeconds(2f);

        /// <summary>
        ///     The server the pda sends it state to.
        ///     The pda will try connecting to a new server when no server is connected.
        ///     It does this by calling the servers entity system for performance reasons.
        /// </summary>
        [DataField("server")]
        public string? ConnectedServer = null;

        /// <summary>
        ///     The station this suit sensor belongs to. If it's null the suit didn't spawn on a station and the sensor doesn't work.
        /// </summary>
        [DataField("station")]
        public EntityUid? StationId = null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("responsePings")]
        public bool ResponsePings { get; set; } = true;

        /// <summary>
        /// Known PDA messengers in network by address with messenger names
        /// </summary>
        [ViewVariables]
        public Dictionary<string, string> KnownPDAMessengers { get; set; } = new();

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
        /// Remaining time of message dequeue
        /// </summary>
        [DataField("messageDownloadTimeRemaining")]
        public float MessageDLTimeRemaining;

        /// <summary>
        /// How long it takes message download
        /// </summary>
        [ViewVariables]
        public float MessageDLTime = 0.3f;
    }
}
