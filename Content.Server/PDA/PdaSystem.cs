using Content.Server.AlertLevel;
using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Instruments;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.Mind;
using Content.Server.PDA.PDAMessenger;
using Content.Server.PDA.Ringer;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Content.Shared.Light.Component;
using Content.Shared.Mobs;
using Robust.Shared.Timing;

namespace Content.Server.PDA
{
    public sealed class PdaSystem : SharedPdaSystem
    {
        [Dependency] private readonly PdaMessageServerSystem _pdaMessageServerSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
        [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly InstrumentSystem _instrument = default!;
        [Dependency] private readonly RingerSystem _ringer = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        // String constants identical with PdaMessagerSystem. Will move to static class
        public const string PDA_CMD_PING = "pda_messenger_ping";
        public const string PDA_CMD_PONG = "pda_messenger_pong";
        public const string PDA_CMD_TX = "pda_messenger_tx";
        public const string PDA_CMD_NAME = "pda_messenger_data_name";
        public const string PDA_CMD_MSG = "pda_messenger_message_data";
        public const string PDA_CMD_PEER = "pda_messenger_peer_message";
        public const string PDA_PEER_MSG = "pda_messenger_peer_data";
        public const string PDA_CMD_TOADDR = "pda_messenger_txaddress_data";
        public const string PDA_FROM_NAME = "FromName";
        public const string PDA_FROM_ADDR = "FromAddress";
        public const string PDA_MSG_CONT = "Content";
        public const string PDA_TO_ADDR = "ToAddress";

        public override void Initialize()
        {
            base.Initialize();

            // Hooks
            SubscribeLocalEvent<PdaComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
            SubscribeLocalEvent<PdaComponent, LightToggleEvent>(OnLightToggle);

            // UI Events:
            SubscribeLocalEvent<PdaComponent, PdaRequestUpdateInterfaceMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaToggleFlashlightMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowRingtoneMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowMusicMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowUplinkMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaLockUplinkMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaRefreshMessage>(OnRefreshMessage);
            SubscribeLocalEvent<PdaComponent, PdaTextMessage>(OnSendMessage);

            SubscribeLocalEvent<StationRenamedEvent>(OnStationRenamed);
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            Logger.Info($"PDA System Initialized");
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var curTime = _gameTiming.CurTime;

            var pdas = EntityManager.EntityQueryEnumerator<PdaComponent, DeviceNetworkComponent>();

            while (pdas.MoveNext(out var uid, out var pda, out var device))
            {
                if (device.TransmitFrequency is null || device.ReceiveFrequency is null)
                    break;

                if (curTime < pda.NextUpdate)
                    continue;

                if (!CheckPdaAssignedStation(uid, pda))
                    continue;

                pda.NextUpdate = curTime + pda.UpdateRate;

                //Retrieve active pda server address if the pda isn't connected to a pda
                var serverAddress = GetPdaConnectedServerAddress(pda);
                if (serverAddress is null )
                    continue;
                pda.ConnectedServer = serverAddress;

                // Ping the PDA Message Server
                var pingPayload = PdaPingPacket(pda);

                // Clear the connected server if its address isn't on the network
                if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, pda.ConnectedServer))
                {
                    pda.ConnectedServer = null;
                    continue;
                }

                _deviceNetworkSystem.QueuePacket(uid, pda.ConnectedServer, pingPayload, device.TransmitFrequency);

                ProcessMessageDownload(uid, frameTime, pda);
            }
        }

        #region EVENTS
        protected override void OnComponentInit(EntityUid uid, PdaComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            if (!HasComp<ServerUserInterfaceComponent>(uid))
                return;

            UpdateAlertLevel(uid, pda);
            UpdateStationName(uid, pda);
            PingPdaServer(uid, pda);
        }

        protected override void OnItemInserted(EntityUid uid, PdaComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PdaComponent pda, EntRemovedFromContainerMessage args)
        {
            base.OnItemRemoved(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        private void OnLightToggle(EntityUid uid, PdaComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePdaUi(uid, pda);
        }

        private void OnStationRenamed(StationRenamedEvent ev)
        {
            UpdateAllPdaUisOnStation();
        }

        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            UpdateAllPdaUisOnStation();
        }
        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaRequestUpdateInterfaceMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            UpdatePdaUi(uid, pda);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaToggleFlashlightMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
                _unpoweredFlashlight.ToggleLight(uid, flashlight);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowRingtoneMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (HasComp<RingerComponent>(uid))
                _ringer.ToggleRingerUI(uid, (IPlayerSession) msg.Session);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowMusicMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<InstrumentComponent>(uid, out var instrument))
                _instrument.ToggleInstrumentUi(uid, (IPlayerSession) msg.Session, instrument);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowUplinkMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            // check if its locked again to prevent malicious clients opening locked uplinks
            if (TryComp<StoreComponent>(uid, out var store) && IsUnlocked(uid))
                _store.ToggleUi(msg.Session.AttachedEntity!.Value, uid, store);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaLockUplinkMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<RingerUplinkComponent>(uid, out var uplink))
            {
                _ringer.LockUplink(uid, uplink);
                UpdatePdaUi(uid, pda);
            }
        }

        private void OnRefreshMessage(EntityUid uid, PdaComponent pda, PdaRefreshMessage args)
        {
            Refresh(uid, pda);
        }
        private void OnSendMessage(EntityUid uid, PdaComponent pda,  PdaTextMessage args)
        {
            Send(uid, pda, args.SenderName, args.Message );
        }
        private void OnPacketReceived(EntityUid uid, PdaComponent pda, DeviceNetworkPacketEvent args)
        {
            if (!HasComp<DeviceNetworkComponent>(uid) || string.IsNullOrEmpty(args.SenderAddress))
                return;

            if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            {
                switch (command)
                {
                    case PDA_CMD_PEER: // Receive known PDAs from PDA Message Server
                        UpdatePeers(uid, pda, args);
                        break;

                    case PDA_CMD_PING: // PDA Message Server sends ping to PDAs to get PDA to send Pong
                        PongPdaServer(uid, pda);
                        break;

                    case PDA_CMD_TX: // PDA server sends messages to download
                        if (!args.Data.TryGetValue(PDA_CMD_NAME, out string? name) ||
                            !args.Data.TryGetValue(PDA_CMD_MSG, out string? content))
                            return;

                        var message = new Dictionary<string, string>() {{"Name", name}, {"Content", content}} ;
                        Receive(uid, message, args.SenderAddress);

                        break;
                }
            }
        }
        #endregion

        #region SETTERS/GETTERS

        private string? GetPdaConnectedServerAddress(PdaComponent pda)
        {
            if (pda.ConnectedServer != null)
                return pda.ConnectedServer;
            return !_pdaMessageServerSystem.TryGetActiveServerAddress(pda.StationId!.Value, out var address) ? null : address;
        }

        private bool IsUnlocked(EntityUid uid)
        {
            return !TryComp<RingerUplinkComponent>(uid, out var uplink) || uplink.Unlocked;
        }

        private string? GetDeviceNetAddress(EntityUid uid)
        {
            string? address = null;

            if (TryComp(uid, out DeviceNetworkComponent? deviceNetworkComponent))
            {
                address = deviceNetworkComponent?.Address;
            }

            return address;
        }

        /// <summary>
        /// Checks whether the sensor is assigned to a station or not
        /// and tries to assign an unassigned sensor to a station if it's currently on a grid
        /// </summary>
        /// <returns>True if the sensor is assigned to a station or assigning it was successful. False otherwise.</returns>
        private bool CheckPdaAssignedStation(EntityUid uid, PdaComponent pda)
        {
            if (!pda.StationId.HasValue && Transform(uid).GridUid == null)
                return false;

            pda.StationId = _stationSystem.GetOwningStation(uid);
            return pda.StationId.HasValue;
        }

        public NetworkPayload PdaPingPacket(PdaComponent pda)
        {
            var payload = new NetworkPayload()
            {
                [DeviceNetworkConstants.Command] = PDA_CMD_PING,
            };
            return payload;
        }

        public NetworkPayload PdaPongPacket(PdaComponent pda)
        {
            var payload = new NetworkPayload()
            {
                [DeviceNetworkConstants.Command] = PDA_CMD_PONG,
                [PDA_CMD_NAME] = pda.OwnerName
            };
            return payload;
        }

        #endregion

        #region METHODS

        #region ORIGINAL

        public void SetOwner(EntityUid uid, PdaComponent pda, string ownerName)
        {
            pda.OwnerName = ownerName;
            UpdatePdaUi(uid, pda);
        }

        private void UpdateAllPdaUisOnStation()
        {
            var query = EntityQueryEnumerator<PdaComponent>();
            while (query.MoveNext(out var ent, out var comp))
            {
                UpdatePdaUi(ent, comp);
            }
        }

        /// <summary>
        /// Send new UI state to clients, call if you modify something like uplink.
        /// </summary>
        public void UpdatePdaUi(EntityUid uid, PdaComponent pda)
        {
            if (!_ui.TryGetUi(uid, PdaUiKey.Key, out _))
                return;

            var address = GetDeviceNetAddress(uid);
            var hasInstrument = HasComp<InstrumentComponent>(uid);
            var showUplink = HasComp<StoreComponent>(uid) && IsUnlocked(uid);

            UpdateStationName(uid, pda);
            UpdateAlertLevel(uid, pda);
            // TODO: Update the level and name of the station with each call to UpdatePdaUi is only needed for latejoin players.
            // TODO: If someone can implement changing the level and name of the station when changing the PDA grid, this can be removed.

            var state = new PdaUpdateState(
                pda.FlashlightOn,
                pda.PenSlot.HasItem,
                new PdaIdInfoText
                {
                    ActualOwnerName = pda.OwnerName,
                    IdOwner = pda.ContainedId?.FullName,
                    JobTitle = pda.ContainedId?.JobTitle,
                    StationAlertLevel = pda.StationAlertLevel,
                    StationAlertColor = pda.StationAlertColor
                },
                pda.StationName,
                showUplink,
                hasInstrument,
                address,
                pda.MessageList,
                pda.KnownPDAMessengers);

            _cartridgeLoader?.UpdateUiState(uid, state);
        }

        private void UpdateStationName(EntityUid uid, PdaComponent pda)
        {
            var station = _station.GetOwningStation(uid);
            pda.StationName = station is null ? null : Name(station.Value);
        }

        private void UpdateAlertLevel(EntityUid uid, PdaComponent pda)
        {
            var station = _station.GetOwningStation(uid);
            if (!TryComp(station, out AlertLevelComponent? alertComp) ||
                alertComp.AlertLevels == null)
                return;
            pda.StationAlertLevel = alertComp.CurrentLevel;
            if (alertComp.AlertLevels.Levels.TryGetValue(alertComp.CurrentLevel, out var details))
                pda.StationAlertColor = details.Color;
        }

        #endregion


        #region MESSAGER

        private void PingPdaServer(EntityUid uid, PdaComponent? pda = null, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref pda, ref device))
                return;

            var freq = device.TransmitFrequency;

            if (pda.OwnerName == null || freq == null || device == null)
                return;

            var payload = PdaPingPacket(pda);

            _deviceNetworkSystem.QueuePacket(uid, pda.ConnectedServer, payload, freq);
        }

        private void PongPdaServer(EntityUid uid, PdaComponent? pda = null, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref pda, ref device))
                return;

            var freq = device.TransmitFrequency;

            if (pda.OwnerName == null || freq == null || device == null)
                return;

            var payload = PdaPongPacket(pda);
            Logger.Debug($"Ponging Address: {pda.ConnectedServer} on {freq}");

            _deviceNetworkSystem.QueuePacket(uid, pda.ConnectedServer, payload, freq);
            //_deviceNetworkSystem.QueuePacket(uid, null, payload, null);
        }

        private void Refresh(EntityUid uid, PdaComponent pda)
        {
            PingPdaServer(uid, pda);
        }

        private void UpdatePeers(EntityUid uid, PdaComponent pda, DeviceNetworkPacketEvent args)
        {
            if (!args.Data.TryGetValue(PDA_PEER_MSG, out Dictionary<string, string>? pdaPeerData))
            {
                Logger.Info($"Updating Peers {pdaPeerData}");
                return;
            }
            pda.KnownPDAMessengers = pdaPeerData;
            UpdatePdaUi(uid, pda);
        }

        private void Receive(EntityUid uid, Dictionary<string,string> message, string? fromAddress = null, PdaComponent? pda = null)
        {
            if (!Resolve(uid, ref pda))
                return;

            var PdaName = Loc.GetString("fax-machine-popup-source-unknown");
            if (fromAddress != null && pda.KnownPDAMessengers.TryGetValue(fromAddress, out var pdaName)) // If message received from unknown fax address
                PdaName = pdaName;

            pda.MessageQueue.Enqueue(message);
        }

        private void SpawnMessageFromQueue(EntityUid uid, PdaComponent? pda = null)
        {
            if (!Resolve(uid, ref pda) || pda.MessageQueue.Count == 0)
                return;

            var message = pda.MessageQueue.Dequeue();

            pda.MessageList.Add(message);
            UpdatePdaUi(uid, pda);

        }

        public void Send(EntityUid uid, PdaComponent? pda = null, string? toAddress = null, string? message = null)
        {
            if (!Resolve(uid, ref pda))
                return;

            if (toAddress == null)
                return;

            if (!pda.KnownPDAMessengers.TryGetValue(toAddress, out var pdaName))
                return;

            if (message == null)
                return;
            if (pda.OwnerName == null)
                return;

            var payload = new NetworkPayload()
            {
                { DeviceNetworkConstants.Command, PDA_CMD_TX },
                { PDA_CMD_NAME, pda.OwnerName },
                { PDA_CMD_MSG, message },
                { PDA_CMD_TOADDR, toAddress}
            };

            _deviceNetworkSystem.QueuePacket(uid, toAddress, payload);
        }

        private void ProcessMessageDownload(EntityUid uid, float frameTime, PdaComponent pda)
        {
            if (pda.MessageDLTimeRemaining > 0)
            {
                pda.MessageDLTimeRemaining -= frameTime;

                var isDLTimeEnd = pda.MessageDLTime <= 0;
                if (isDLTimeEnd)
                {
                    SpawnMessageFromQueue(uid, pda);
                }
                return;
            }

            if (pda.MessageQueue.Count > 0)
            {
                pda.MessageDLTimeRemaining = pda.MessageDLTime;
            }
        }



        #endregion

        #endregion

    }
}
