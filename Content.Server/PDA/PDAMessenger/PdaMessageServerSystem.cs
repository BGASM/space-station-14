using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;


namespace Content.Server.PDA.PDAMessenger;

public sealed class PdaMessageServerSystem : EntitySystem
{
    [Dependency] private readonly PdaSystem _system = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

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
            //ProcessMessageQueue(activeServer);
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

    private void TransmitMessage(EntityUid uid, PdaMessageServerComponent component, DeviceNetworkComponent device, Dictionary<string, string> message)
    {
        var freq = device.TransmitFrequency;

        if (!message.TryGetValue(PDA_TO_ADDR, out string? toAddress) ||
            !message.TryGetValue(PDA_FROM_ADDR, out string? fromAddress) ||
            !message.TryGetValue(PDA_FROM_NAME, out string? fromName)||
            !message.TryGetValue(PDA_MSG_CONT, out string? content))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = PDA_CMD_TX,
            [PDA_TO_ADDR] = toAddress,
            [PDA_FROM_ADDR] = fromAddress,
            [PDA_FROM_NAME] = fromName,
            [PDA_MSG_CONT] = content,
        };

        _deviceNetworkSystem.QueuePacket(uid, toAddress, payload, freq, device: device);
    }

    private void OnPacketReceived(EntityUid uid, PdaMessageServerComponent component, DeviceNetworkPacketEvent args)
    {
        Logger.Debug($"In PDAServer OnPacketReceived Call.");

        if (!HasComp<DeviceNetworkComponent>(uid) || string.IsNullOrEmpty(args.SenderAddress))
            return;

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            Logger.Info($"In PDAServer OnPacketReceived Call. - {command}");
            switch (command)
            {
                case PDA_CMD_PING: //Pda sends ping to server, respond with KnownPDAMessengers
                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, PDA_CMD_PEER },
                        { PDA_PEER_MSG, component.KnownPDAMessengers }
                    };
                    Logger.Info($"In server PING, sending known pdas. - {component.KnownPDAMessengers}");
                    _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, payload);

                    break;

                case PDA_CMD_PONG: //Pda pongs server, add name and address to KnownPDAMessengers

                    if (!args.Data.TryGetValue(PDA_CMD_NAME, out string? pdaOwnerName))
                    {
                        Logger.Info($"In PONG 1. - {pdaOwnerName}");
                        return;
                    }

                    component.KnownPDAMessengers[args.SenderAddress] = pdaOwnerName;
                    Logger.Info($"In PONG 2. - {component.KnownPDAMessengers.ToString()}");
                    break;

                case PDA_CMD_TX: //PDA sends TX message to server, add message to Queue
                    if (!args.Data.TryGetValue(PDA_CMD_NAME, out string? name) ||
                        !args.Data.TryGetValue(PDA_CMD_MSG, out string? content) ||
                        !args.Data.TryGetValue(PDA_CMD_TOADDR, out string? toAddress))
                        return;

                    var message = new Dictionary<string, string>()
                        {
                            { PDA_TO_ADDR, toAddress },
                            { PDA_FROM_ADDR, args.SenderAddress},
                            { PDA_FROM_NAME, name },
                            { PDA_MSG_CONT, content }
                        };
                    Receive(uid, message);

                    break;
            }
        }
    }

    private void Receive(EntityUid uid, Dictionary<string,string> message, PdaMessageServerComponent? component = null)
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

        Logger.Info($"Pinging All PDAS on freq: {device.TransmitFrequency}");
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
        Logger.Info($"Server trying to connect");
        if (!Resolve(uid, ref server, ref device))
            return;

        server.Active = true;
        Logger.Info($"Server successfully connected! {device.DeviceNetId} - {device.ReceiveFrequency}");

        if (_deviceNetworkSystem.IsDeviceConnected(uid, device))
            return;

        Logger.Info($"Server successfully connected! {device.DeviceNetId} - {device.ReceiveFrequency}");
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
