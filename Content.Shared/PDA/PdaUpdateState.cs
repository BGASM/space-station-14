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
        public List<Dictionary<string, string>>? MessageList;
        public Dictionary<string, string> KnownPDAMessengers { get; } = new();

        public PdaUpdateState(bool flashlightEnabled, bool hasPen, PdaIdInfoText pdaOwnerInfo,
            string? stationName, bool hasUplink = false,
            bool canPlayMusic = false, string? address = null,
            List<Dictionary<string, string>>? messageList = null,
            Dictionary<string, string>? knownPDAMessengers = null
            )
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PdaOwnerInfo = pdaOwnerInfo;
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
            Address = address;
            MessageList = messageList;
            if (knownPDAMessengers != null)
                KnownPDAMessengers = knownPDAMessengers;
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
