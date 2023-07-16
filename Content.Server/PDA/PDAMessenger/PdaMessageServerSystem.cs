using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.PDA;


namespace Content.Server.PDA.PDAMessenger;

public sealed class PdaMessageServerSystem : EntitySystem
{
    [Dependency] private readonly PdaSystem _system = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public const string PDA_CMD_PING = "pda_messenger_ping_command";
    public const string PDA_CMD_PONG = "pda_messenger_pong_command";
    public const string PDA_CMD_TX = "pda_messenger_tx_command";
    public const string PDA_CMD_PEER = "pda_messenger_peer_command";
    public const string PDA_DATA_FRNAME = "pda_messenger_fromname_data";
    public const string PDA_DATA_FRADDR = "pda_messenger_fromaddr_data";
    public const string PDA_DATA_RXLIST = "pda_messenger_recipient_list_data";
    public const string PDA_DATA_TONAME = "pda_messenger_recipient_name_data";
    public const string PDA_DATA_MSG = "pda_messenger_message_data";
    public const string PDA_DATA_PEER = "pda_messenger_peer_data";
    public const string PDA_DATA_TOADDR = "pda_messenger_txaddress_data";
    public const string PDA_DATA_TIME = "pda_messenger_time_data";
    public const string PDA_FROM_NAME = "FromName";
    public const string PDA_FROM_ADDR = "FromAddress";
    public const string PDA_MSG_CONT = "Content";
    public const string PDA_TO_ADDR = "ToAddress";

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PdaMessageServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<PdaMessageServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<PdaMessageServerComponent, PowerChangedEvent>(OnPowerChanged);
        Logger.Debug($"PDA Server Initialized");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityQueryEnumerator<PdaMessageServerComponent>();
        List<EntityUid> activeServers = new();

        while (servers.MoveNext(out var id, out var server))
        {
            //Make sure the server is disconnected when it becomes unavailable
            if (!server.Available)
            {
                if (server.Active)
                    DisconnectServer(id, server);

                continue;
            }

            if (!server.Active)
                continue;

            activeServers.Add(id);
        }

        foreach (var activeServer in activeServers)
        {
            PingAllPda(activeServer);
            ProcessMessageQueue(activeServer);
        }
    }

    private void ProcessMessageQueue(EntityUid uid, PdaMessageServerComponent? component = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref component, ref device))
            return;

        while (component.MessageQueue.Count > 0)
        {
            var message = component.MessageQueue.Dequeue();
            TransmitMessage(uid, component, device, message);
        }
    }

    private void TransmitMessage(EntityUid uid, PdaMessageServerComponent component, DeviceNetworkComponent device, PdaMessage message)
    {
        var freq = device.TransmitFrequency;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = PDA_CMD_TX,
            [PDA_DATA_RXLIST] = message.RecipientList,
            [PDA_DATA_TONAME] = message.ReceiverName,
            [PDA_DATA_TOADDR] = message.ReceiverAddress,
            [PDA_DATA_MSG] = message.Message,
            [PDA_DATA_TIME] = message.SentAt,
            [PDA_DATA_FRNAME] = message.SenderName,
            [PDA_DATA_FRADDR] = message.SenderAddress,
        };

        _deviceNetworkSystem.QueuePacket(uid, message.ReceiverAddress, payload, freq, device: device);
    }

    private void OnPacketReceived(EntityUid uid, PdaMessageServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!HasComp<DeviceNetworkComponent>(uid) || string.IsNullOrEmpty(args.SenderAddress))
            return;

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case PDA_CMD_PING: //Pda sends ping to server, respond with KnownPDAMessengers
                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, PDA_CMD_PEER },
                        { PDA_DATA_PEER, component.KnownPDAMessengers }
                    };

                    _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, payload);

                    break;

                case PDA_CMD_PONG: //Pda pongs server, add name and address to KnownPDAMessengers
                    if (!args.Data.TryGetValue(PDA_DATA_FRNAME, out string? pdaOwnerName))
                        return;

                    var foundPda = component.KnownPDAMessengers.Find(p => p.Address == args.SenderAddress);
                    if (foundPda != null)
                    {
                        foundPda.Name = pdaOwnerName;
                    }
                    else
                    {
                        component.KnownPDAMessengers.Add(new KnownPda(pdaOwnerName, args.SenderAddress));
                    }
                    break;

                case PDA_CMD_TX: //PDA sends TX message to server, add message to Queue
                    Logger.Debug($"Made it to server.");
                    if (!args.Data.TryGetValue(PDA_DATA_RXLIST, out List<KnownPda>? recipientList) ||
                        !args.Data.TryGetValue(PDA_DATA_TONAME, out string? receiverName) ||
                        !args.Data.TryGetValue(PDA_DATA_TOADDR, out string? recieverAddress) ||
                        !args.Data.TryGetValue(PDA_DATA_MSG, out string? messageContent) ||
                        !args.Data.TryGetValue(PDA_DATA_TIME, out DateTime? sentAt) ||
                        !args.Data.TryGetValue(PDA_DATA_FRNAME, out string? senderName))
                        return;


                    var message = new PdaMessage(recipientList, receiverName, recieverAddress, messageContent, sentAt, senderName, args.SenderAddress );

                    Receive(uid, message);

                    break;
            }
        }
    }

    private void Receive(EntityUid uid, PdaMessage message, PdaMessageServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.MessageQueue.Enqueue(message);
    }

    private void PingAllPda(EntityUid uid, PdaMessageServerComponent? component = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref component, ref device))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = PDA_CMD_PING
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device.TransmitFrequency, device: device);
    }

    /// <summary>
    /// Returns the address of the currently active server for the given station id if there is one
    /// </summary>
    public bool TryGetActiveServerAddress(EntityUid stationId, out string? address)
    {
        var servers = EntityQueryEnumerator<PdaMessageServerComponent, DeviceNetworkComponent>();
        (EntityUid id, PdaMessageServerComponent server, DeviceNetworkComponent device)? last = default;

        while (servers.MoveNext(out var uid, out var server, out var device))
        {
            if (!_stationSystem.GetOwningStation(uid)?.Equals(stationId) ?? true)
                continue;

            if (!server.Available)
            {
                DisconnectServer(uid,server, device);
                continue;
            }

            last = (uid, server, device);

            if (server.Active)
            {
                address = device.Address;
                return true;
            }
        }

        //If there was no active server for the station make the last available inactive one active
        if (last.HasValue)
        {
            ConnectServer(last.Value.id, last.Value.server, last.Value.device);
            address = last.Value.device.Address;
            return true;
        }

        address = null;
        return address != null;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, PdaMessageServerComponent component, ComponentRemove args)
    {
        component.KnownPDAMessengers.Clear();
    }

    /// <summary>
    /// Disconnects the server losing power
    /// </summary>
    private void OnPowerChanged(EntityUid uid, PdaMessageServerComponent component, ref PowerChangedEvent args)
    {
        component.Available = args.Powered;

        if (!args.Powered)
            DisconnectServer(uid, component);
    }
    private void ConnectServer(EntityUid uid, PdaMessageServerComponent? server = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref server, ref device))
            return;

        server.Active = true;

        if (_deviceNetworkSystem.IsDeviceConnected(uid, device))
            return;

        _deviceNetworkSystem.ConnectDevice(uid, device);
        PingAllPda(uid, server, device);
    }

    /// <summary>
    /// Disconnects a server from the device network and clears the currently active server
    /// </summary>
    private void DisconnectServer(EntityUid uid, PdaMessageServerComponent? server = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref server, ref device))
            return;

        server.KnownPDAMessengers.Clear();
        server.Active = false;

        _deviceNetworkSystem.DisconnectDevice(uid, device, false);
    }
}
