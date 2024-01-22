using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using RconSharp;

namespace PalworldRcon;

public class RCONClient : INotifyPropertyChanged
{
    public Action OnConnected;
    public Action OnDisconnect;
    public bool IsConnected;

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
    private RconClient _rconClient;
    private Regex _infoRegex = new Regex("\\[(?'version'.*)\\] (?'servername'.*)", RegexOptions.Compiled);

    public RCONClient(Settings settings)
    {
        _settings = settings;
    }

    public async Task<bool> ConnectToRCON()
    {
        if (!IPAddress.TryParse(_settings.ServerAddresss, out var address)) return false;
        if (IsConnected) return true;

        _rconClient = RconClient.Create(_settings.ServerAddresss, _settings.ServerPort);

        _rconClient.ConnectionClosed += RconDisconnect;

        await _rconClient.ConnectAsync();

        IsConnected = await _rconClient.AuthenticateAsync(_settings.RCONPassword);

        if (IsConnected)
        {
            OnConnected?.Invoke();
            await GetInfo();
        }

        return IsConnected;
    }

    public async void SendNotice(string notice)
    {
        if(string.IsNullOrWhiteSpace(notice) || !IsConnected) return;

        notice = notice.Replace(" ", "_");

        await _rconClient.ExecuteCommandAsync($"Broadcast {notice}");
    }

    public async void DoQuit()
    {
        if (!IsConnected) return;

        await _rconClient.ExecuteCommandAsync("Shutdown 30 Server_shutting_down_in_30_seconds!");
    }

    public async Task<string> Save()
    {
        if (!IsConnected) return null;

        var resp = await _rconClient.ExecuteCommandAsync("Save");

        return resp;
    }

    public async Task<Player[]> GetPlayers()
    {
        try
        {
            var result = await _rconClient.ExecuteCommandAsync("ShowPlayers");
            var lines = result.Trim().Split("\n");
            var players = new Player[lines.Length - 1];
            for (int i = 1; i < lines.Length; i++)
            {
                var dataSplit = lines[i].Split(",");
                players[i - 1] = new Player(dataSplit[0].Trim(), dataSplit[1].Trim(), dataSplit[2].Trim());
            }

            return players;
        }
        catch (Exception e)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                MainWindow.Instance.ShowMessageAsync("Error", e.ToString());
            });
        }

        return null;
    }

    public async Task<string> KickPlayer(string steamid)
    {
        if (string.IsNullOrWhiteSpace(steamid)) return null;

        return await _rconClient.ExecuteCommandAsync($"KickPlayer {steamid}");
    }

    public async Task<string> BanPlayer(string steamid)
    {
        if (string.IsNullOrWhiteSpace(steamid)) return null;

        return await _rconClient.ExecuteCommandAsync($"BanPlayer {steamid}");
    }

    public async Task<string> GetInfo()
    {
        try
        {
            var resp = await _rconClient.ExecuteCommandAsync("info");
            var match = _infoRegex.Match(resp);

            if (match.Success)
            {
                ServerName = match.Groups["servername"].Value;
                ServerVersion = match.Groups["version"].Value;
            }

            return resp;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    private void RconDisconnect()
    {
        IsConnected = false;
        OnDisconnect?.Invoke();
        ServerName = "Disconnected";
        ServerVersion = "v0.0.0.0";
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