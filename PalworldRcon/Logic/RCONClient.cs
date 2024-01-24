using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using MahApps.Metro.Controls.Dialogs;
using PalworldRcon.Logging;
using PalworldRcon.Network;
using PalworldRcon.Network.Framing;
using PalworldRcon.Network.TCP;

namespace PalworldRcon;

public class RCONClient : TcpClient, INotifyPropertyChanged
{
    public const int TimeoutSeconds = 20;

    public Action<bool> OnAuth;

    public string ServerVersion
    {
        get => _serverVersion;
        private set
        {
            _serverVersion = value;
            OnPropertyChanged();
        }
    }

    public string ServerName
    {
        get => _serverName;
        private set
        {
            _serverName = value;
            OnPropertyChanged();
        }
    }

    private string _serverVersion = "v0.0.0.0";
    private string _serverName = "PalWorld";
    private Settings _settings;
    private Regex _infoRegex = new Regex("\\[(?'version'.*)\\] (?'servername'.*)", RegexOptions.Compiled);
    private Regex _playerRegex = new Regex("(?'playername'.*),(?'charid'\\d*),(?'steamid'\\d*)", RegexOptions.Compiled);
    private RconFramer _framer = new();
    private TaskCompletionSource<RconPacket> _authTask;
    private Queue<(TaskCompletionSource<RconPacket>, PacketType)> _rconCommandList = new();

    public RCONClient(Settings settings)
    {
        _settings = settings;
        _framer.MessageReceived += FramerOnMessageReceived;
        Disconnected += RconDisconnect;
        Connected += OnConnected;
        ConnectionFailed += OnConnectionFailed;
    }

    private void OnConnectionFailed(Exception obj)
    {
        Log.Error(obj);

        MainWindow.Instance.Dispatcher.Invoke(async () =>
        {
            await MainWindow.Instance.ShowMessageAsync("Connection Error", "Unable to connect to the rcon server, check your address and port! You can check console for more info.");
            MainWindow.Instance.ConnectBtn.Content = "Connect";
        });
    }

    private async void OnConnected(TcpClient obj)
    {
        var resp = await SendAsync(_settings.RCONPassword, PacketType.ClientAuth, PacketType.AuthRespExec);

        if (resp == null || !resp.IsSuccess)
        {
            //Auth failed, disconnect
            Log.Info("Authentication failed! Check your rcon password!");
            OnAuth?.Invoke(false);
            Disconnect();
            return;
        }

        //Poll server info
        await GetInfo();

        OnAuth?.Invoke(true);
    }

    public bool ConnectToRCON()
    {
        if (!IPAddress.TryParse(_settings.ServerAddress, out var address)) return false;
        if (Status == ClientStatus.Connected) return true;

        MainWindow.Instance.ConnectBtn.Content = "Connecting...";

        ConnectAsync(_settings.ServerAddress, _settings.ServerPort);

        return true;
    }

    public async void SendNotice(string notice)
    {
        if(string.IsNullOrWhiteSpace(notice) || Status != ClientStatus.Connected) return;

        notice = notice.Replace(" ", "_");

        await SendAsync($"Broadcast {notice}");
    }

    public async void DoQuit(string shutdownMessage)
    {
        if (Status != ClientStatus.Connected) return;

        shutdownMessage = shutdownMessage.Replace(" ", "_");

        await SendAsync($"Shutdown 30 {shutdownMessage}");
    }

    public async Task<string> Save()
    {
        if (Status != ClientStatus.Connected) return null;

        var resp = await SendAsync("Save");

        return resp.Body;
    }

    public async Task<Player[]> GetPlayers()
    {
        try
        {
            var result = await SendAsync("ShowPlayers");
            var matches = _playerRegex.Matches(result.Body);

            var players = new Player[matches.Count];

            for(int i=0; i<matches.Count; i++)
            {
                Match match = matches[i];
                players[i] = new Player(match.Groups["playername"].Value, match.Groups["charid"].Value, match.Groups["steamid"].Value);
            }

            return players;
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return null;
    }

    public async Task<RconPacket> SendRawCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;

        return await SendAsync(command);
    }

    public async Task<RconPacket> KickPlayer(string steamid)
    {
        if (string.IsNullOrWhiteSpace(steamid)) return null;

        return await SendAsync($"KickPlayer {steamid}");
    }

    public async Task<RconPacket> BanPlayer(string steamid)
    {
        if (string.IsNullOrWhiteSpace(steamid)) return null;

        return await SendAsync($"BanPlayer {steamid}");
    }

    public async Task<string> GetInfo()
    {
        try
        {
            var resp = await SendAsync("info");
            var match = _infoRegex.Match(resp.Body);

            if (match.Success)
            {
                ServerName = match.Groups["servername"].Value;
                ServerVersion = match.Groups["version"].Value;
            }

            return resp.Body;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    private void RconDisconnect(TcpClient tcpClient, ConnectionCloseType connectionCloseType)
    {
        ServerName = "Disconnected";
        ServerVersion = "v0.0.0.0";

        if (connectionCloseType != ConnectionCloseType.Closed)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                MainWindow.Instance.ShowMessageAsync("Connection lost!", $"You unexpectedly lost connection to the rcon server! ({connectionCloseType})");
            });
        }
    }

    protected override void ReceiveData(byte[] buffer, int length)
    {
        //Pass into framer to handle size and null terminators
        _framer.ReceiveData(buffer, length);
    }

    private void FramerOnMessageReceived(byte[] obj)
    {
        //We have data!
        var packet = new RconPacket(obj);

        var lastInQueue = _rconCommandList.Dequeue();

        if (lastInQueue.Item2 != packet.Type)
        {
            Log.Error("We received an unknown packet! No matching exec was sent!");
            Log.Error($"{packet.Type} with body {packet.Body} ");
            return;
        }

        if (packet.Type == PacketType.AuthRespExec && packet.ID == -1 && !_authTask.Task.IsCompleted)
        {
            //Auth failed!
            _authTask.SetResult(packet);
            Log.Error("Auth failed!");
            return;
        }

        switch (packet.Type)
        {
            case PacketType.Response:
                //This is a response to a message
                lastInQueue.Item1.SetResult(packet);
                break;
            case PacketType.AuthRespExec:
                //Auth response!
                packet.IsSuccess = true;
                lastInQueue.Item1.SetResult(packet);
                break;
            default:
                //We got something unexpected?! Log to console.
                break;
        }
    }

    private async Task<RconPacket> SendAsync(string body, PacketType type = PacketType.AuthRespExec, PacketType expectedType = PacketType.Response)
    {
        if (Status != ClientStatus.Connected)
        {
            Log.Error("You're not connected to an rcon server!");
            return new RconPacket(0, PacketType.Response, "Not connected!");
        }

        var packet = new RconPacket(0, type, body);

        try
        {
            Send(_framer.Frame(packet.GetBytes()));
        }
        catch (Exception e)
        {
            Log.Error("SendAsync encountered an unknown exception! Packet {packet.Type} with data {packet.Body} failed to send!");
            Log.Error(e);
        }

        var respTask = new TaskCompletionSource<RconPacket>();

        if (type == PacketType.ClientAuth)
        {
            //Auth is always the first command, let's reset the command dict
            _rconCommandList.Clear();
            _authTask = respTask;
        }

        _rconCommandList.Enqueue((respTask, expectedType));

        try
        {
            return await respTask.Task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
        }
        catch (TimeoutException)
        {
            Log.Error($"SendAsync has timed out! Packet {packet.Type} with data {packet.Body} did not get a response from the server!");
        }

        return new RconPacket(0, PacketType.Response, "Failure in SendAsync!");
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}