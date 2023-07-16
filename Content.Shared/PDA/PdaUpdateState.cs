using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;


namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PdaUpdateState : CartridgeLoaderUiState
    {
        public bool FlashlightEnabled;
        public bool HasPen;
        public PdaIdInfoText PdaOwnerInfo;
        public string? StationName;
        public bool HasUplink;
        public bool CanPlayMusic;
        public string? Address;
        public List<PdaConversation> ConversationList { get; } = new();
        public List<KnownPda> KnownPDAMessengers { get; } = new();

        public PdaUpdateState(bool flashlightEnabled, bool hasPen, PdaIdInfoText pdaOwnerInfo,
            string? stationName, bool hasUplink = false,
            bool canPlayMusic = false, string? address = null,
            List<PdaConversation>? conversationList = null,
            List<KnownPda>? knownPDAMessengers = null
            )
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PdaOwnerInfo = pdaOwnerInfo;
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
            Address = address;
            ConversationList = conversationList ?? ConversationList;
            KnownPDAMessengers = knownPDAMessengers ?? KnownPDAMessengers;
        }
    }

    [Serializable, NetSerializable]
    public sealed class KnownPda
    {
        public string Name { get; set; }
        public string Address { get; set; }

        public KnownPda(string name, string address)
        {
            Name = name;
            Address = address;
        }
    }
    [Serializable, NetSerializable]
    public struct PdaIdInfoText
    {
        public string? ActualOwnerName;
        public string? IdOwner;
        public string? JobTitle;
        public string? StationAlertLevel;
        public Color StationAlertColor;
    }
}
