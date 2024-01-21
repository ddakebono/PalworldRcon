using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace PalworldRcon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static MainWindow Instance;
        public ObservableCollection<Player> Players { get; } = new();

        private Settings _settings;
        private RCONClient _client;
        private BackgroundWorker _worker;

        public MainWindow()
        {
            Instance = this;

            _settings = new Settings();
            _settings.LoadSettings();

            _client = new RCONClient(_settings);

            InitializeComponent();

            ServerBlock.DataContext = _client;
            ConnectBtn.DataContext = _client;

            _client.OnConnected += WorkerStartup;
            _client.OnDisconnect += OnDisconnect;

            RCONPassword.Password = _settings.RCONPassword;
            ServerAddress.Text = _settings.ServerAddresss;
            ServerPort.Text = _settings.ServerPort.ToString();
        }

        private void OnDisconnect()
        {
            Dispatcher.Invoke(() =>
            {
                ConnectBtn.Content = "Connect";
                Players.Clear();
            });

            _worker.CancelAsync();

        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            AboutFlyout.IsOpen = !AboutFlyout.IsOpen;
        }

        private async void SaveSettings(object sender, RoutedEventArgs e)
        {
            //Validate settings
            if (!IPAddress.TryParse(ServerAddress.Text, out var address))
            {
                await this.ShowMessageAsync("Settings Issue!", "Given Server Address is not a valid IP Address!");
                return;
            }
            if (!ushort.TryParse(ServerPort.Text, CultureInfo.CurrentCulture, out var port))
            {
                await this.ShowMessageAsync("Settings Issue!", "Given Server Port is not valid! (1-65535)");
                return;
            }

            _settings.ServerAddresss = address.ToString();
            _settings.ServerPort = port;
            _settings.RCONPassword = RCONPassword.Password;
            _settings.SaveSettings();
        }

        private async void TryConnection(object sender, RoutedEventArgs e)
        {
            SaveSettings(sender, e);

            var connRes = await _client.ConnectToRCON();

            ConnectBtn.Content = connRes ? "Connected!" : "Connect";

            var result = await _client.GetInfo();

            if (result == null)
            {
                await this.ShowMessageAsync("Connection Test", "Connection test failed! Did not receive info response!");
                return;
            }

            await this.ShowMessageAsync("Connection Test", $"Connection test success! Server responsed with: \"{result}\"");
        }

        private async void ConnectButton(object sender, RoutedEventArgs e)
        {
            if (_client.IsConnected) return;

            var result = await _client.ConnectToRCON();

            ConnectBtn.Content = result ? "Connected!" : "Connect";
        }

        private void SendNotice(object sender, RoutedEventArgs e)
        {
            this.ShowInputAsync("Notice", "Enter a notice").ContinueWith(task =>
            {
                //Entered and submitted
                if(!task.IsCompletedSuccessfully) return;
                _client.SendNotice(task.Result);
            });
        }

        private async void KillServer(object sender, RoutedEventArgs e)
        {
            var select = await this.ShowMessageAsync("Are you sure?", $"Are you sure you wish to shutdown the Palworld server?", MessageDialogStyle.AffirmativeAndNegative);
            if (select != MessageDialogResult.Affirmative) return;

            _client.DoQuit();
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            var resp = await _client.Save();

            await this.ShowMessageAsync("Save", resp);
        }

        private async void Kick(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
            {
                await this.ShowMessageAsync("Oops", "You need to select a player before using this command!");
                return;
            }

            var player = PlayerList.SelectedItem as Player;

            if (player == null) return;

            var select = await this.ShowMessageAsync("Are you sure?", $"Are you sure you wish to kick {player.PlayerName} ({player.SteamID})?", MessageDialogStyle.AffirmativeAndNegative);
            if (select != MessageDialogResult.Affirmative) return;

            var resp = await _client.KickPlayer(player.SteamID);
            if (!string.IsNullOrWhiteSpace(resp))
                await this.ShowMessageAsync("Kick", resp);
        }

        private async void Ban(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
            {
                await this.ShowMessageAsync("Oops", "You need to select a player before using this command!");
                return;
            }

            var player = PlayerList.SelectedItem as Player;

            if (player == null) return;

            var select = await this.ShowMessageAsync("Are you sure?", $"Are you sure you wish to ban {player.PlayerName} ({player.SteamID})? This cannot be undone remotely!", MessageDialogStyle.AffirmativeAndNegative);
            if (select != MessageDialogResult.Affirmative) return;

            var resp = await _client.BanPlayer(player.SteamID);
            if (!string.IsNullOrWhiteSpace(resp))
                await this.ShowMessageAsync("Ban", resp);
        }

        private async void CopyName(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
            {
                await this.ShowMessageAsync("Oops", "You need to select a player before using this command!");
                return;
            }

            var player = PlayerList.SelectedItem as Player;

            if (player == null) return;

            Clipboard.SetText(player.PlayerName);
        }

        private async void CopyCharacter(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
            {
                await this.ShowMessageAsync("Oops", "You need to select a player before using this command!");
                return;
            }

            var player = PlayerList.SelectedItem as Player;

            if (player == null) return;

            Clipboard.SetText(player.CharacterID);
        }

        private async void CopySteam(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
            {
                await this.ShowMessageAsync("Oops", "You need to select a player before using this command!");
                return;
            }

            var player = PlayerList.SelectedItem as Player;

            if (player == null) return;

            Clipboard.SetText(player.SteamID);
        }

        private void WorkerStartup()
        {
            //Startup player update thread?
            _worker = new BackgroundWorker();
            _worker.DoWork += PlayerUpdateWorker;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync();
        }

        private async void PlayerUpdateWorker(object sender, DoWorkEventArgs e)
        {
            while (!_worker.CancellationPending)
            {
                //Let's only update players every 10 seconds
                Thread.Sleep(3000);
                var list = await _client.GetPlayers();
                if (list != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var player in Players.Where(x => !list.Contains(x)).ToArray())
                            Players.Remove(player);

                        foreach (var player in list)
                        {
                            if(Players.Contains(player)) continue;
                            Players.Add(player);
                        }
                    });
                }
                Thread.Sleep(7000);
            }
        }
    }
}