using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreRCON;
using CoreRCON.PacketFormats;
using MahApps.Metro.Controls.Dialogs;
using PalworldRcon.Logging;
using PalworldRcon.Logic.Responses;
using ToastNotifications.Messages;

namespace PalworldRcon.Logic;

public class RCONClient(Settings settings) : INotifyPropertyChanged
{
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

    public int PlayerCount
    {
        get => _playerCount;
        private set
        {
            _playerCount = value;
            OnPropertyChanged();
        }
    }

    public bool? IsConnectedAndAuthed => _client?.Authenticated;

    public Action Disconnected;

    private string _serverVersion = "v0.0.0.0";
    private string _serverName = "PalWorld";
    private RCON _client;
    private bool _clientDisposed;
    private int _playerCount;

    private void CreateRconClient(IPAddress address, ushort port)
    {
        if (_client != null && _client.Connected) _client.Dispose();

        _client = new RCON(address, port, settings.RCONPassword, sourceMultiPacketSupport: false, strictCommandPacketIdMatching: false, autoConnect: true);

        _client.OnDisconnected += RconDisconnect;
        _client.OnPacketReceived += OnPacketReceived;

        Log.Debug("Created RCON client, ready to connect.");
    }

    private void OnPacketReceived(RCONPacket obj)
    {
        if(settings.DebugMode)
            Log.Debug($"Packet - Body: {obj.Body} | ID: {obj.Id} | Type: {obj.Type}");
    }

    private void OnConnectionFailed()
    {
        Log.Error("Unable to connect to the rcon server, check your address and port! You can check console for more info.");

        MainWindow.Instance.Dispatcher.Invoke(async () =>
        {
            await MainWindow.Instance.ShowMessageAsync("Connection Error", "Unable to connect to the rcon server, check your address and port! You can check console for more info.");
            MainWindow.Instance.ConnectBtn.Content = "Connect";
        });

        Disconnected?.Invoke();
    }

    public void Disconnect()
    {
        if (_clientDisposed) return;

        _clientDisposed = true;

        _client.Dispose();

        _client = null;

        RconDisconnect();

        Log.Info("Disconnected from the rcon server.");
    }

    public async Task<bool> ConnectToRCON(bool testConn = false)
    {
        if (!IPAddress.TryParse(settings.ServerAddress, out var address)) return false;

        if (_client is { Connected: true })
        {
            //Disconnect client first
            Disconnect();
        }

        _clientDisposed = false;

        //Create new client after dispose and cleanup
        CreateRconClient(address, settings.ServerPort);

        MainWindow.Instance.ConnectBtn.Content = "Connecting...";

        await _client.ConnectAsync();

        if (!_client.Connected)
        {
            //Failed to connect
            if(!testConn)
                OnConnectionFailed();
            return false;
        }

        var success = await _client.AuthenticateAsync();

        if (testConn)
        {
            _client.Dispose();
            _client = null;
            return success;
        }

        if (success)
        {
            OnAuth?.Invoke(true);

            //Poll server info
            var info = await GetInfo();
            ServerName = info.ServerName;
            ServerVersion = info.ServerVersion;
        }
        else
        {
            OnAuth?.Invoke(false);
        }

        return success;
    }

    public async Task<string> SendNotice(string notice)
    {
        if(string.IsNullOrWhiteSpace(notice) || !_client.Authenticated) return null;

        return await _client.SendCommandAsync($"Broadcast {notice}");
    }

    public async Task<string> DoQuit(string shutdownMessage)
    {
        if (!_client.Authenticated) return null;

        return await _client.SendCommandAsync($"Shutdown 30 {shutdownMessage}");
    }

    public async Task<string> Save()
    {
        if (!_client.Authenticated) return null;

        return await _client.SendCommandAsync("Save");
    }

    public async Task<PlayerList> GetPlayers()
    {
        if(!_client.Authenticated) return null;

        var players = await _client.SendCommandAsync<PlayerList>("ShowPlayers");
        PlayerCount = players.Players.Count;

        return players;
    }

    public async Task<string> SendRawCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;

        return await _client.SendCommandAsync(command);
    }

    public async Task<string> KickPlayer(string steamid)
    {
        if (string.IsNullOrWhiteSpace(steamid)) return null;

        return await _client.SendCommandAsync($"KickPlayer {steamid}");
    }

    public async Task<string> BanPlayer(string steamid)
    {
        if (string.IsNullOrWhiteSpace(steamid)) return null;

        return await _client.SendCommandAsync($"BanPlayer {steamid}");
    }

    public async Task<ServerInfo> GetInfo()
    {
        if(!_client.Authenticated) return null;
        return await _client.SendCommandAsync<ServerInfo>("info");
    }

    private void RconDisconnect()
    {
        ServerName = "Disconnected";
        ServerVersion = "v0.0.0.0";

        MainWindow.Instance.Dispatcher.Invoke(() =>
        {
            MainWindow.Instance.Notifier.ShowError("Disconnected from Palworld server!");
        });

        Disconnected?.Invoke();
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