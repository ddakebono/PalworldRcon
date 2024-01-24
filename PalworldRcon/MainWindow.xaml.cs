using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PalworldRcon.Logging;
using PalworldRcon.Logging.Targets;
using PalworldRcon.Network.TCP;

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

            Log.Info("Startup");

            _settings = new Settings();
            _settings.LoadSettings();

            _client = new RCONClient(_settings);

            InitializeComponent();

            ConsoleBlock.DataContext = ConsoleTarget.Instance;

            Log.Info("Welcome to Bono's Palworld Rcon Tool!");
            Log.Warning("Using this console allows you to send any commands to the server, please ensure you know what you're about to send!");

            ServerBlock.DataContext = _client;
            ConnectBtn.DataContext = _client;

            _client.Disconnected += OnDisconnect;
            _client.OnAuth += OnAuth;

            RCONPassword.Password = _settings.RCONPassword;
            ServerAddress.Text = _settings.ServerAddress;
            ServerPort.Text = _settings.ServerPort.ToString();
        }

        private void OnDisconnect(TcpClient tcpClient, ConnectionCloseType connectionCloseType)
        {
            Log.Info("OnDisconnect called");

            Dispatcher.Invoke(() =>
            {
                ConnectBtn.Content = "Connect";
                Players.Clear();
            });

            if(_worker != null)
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

        private void ConsoleClick(object sender, RoutedEventArgs e)
        {
            ConsoleFlyout.IsOpen = !ConsoleFlyout.IsOpen;
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

            _settings.ServerAddress = address.ToString();
            _settings.ServerPort = port;
            _settings.RCONPassword = RCONPassword.Password;
            _settings.SaveSettings();
        }

        private async void TryConnection(object sender, RoutedEventArgs e)
        {
            SaveSettings(sender, e);

            if (!_client.ConnectToRCON())
            {
                await this.ShowMessageAsync("Oops!", "It seems you have invalid settings, please check your server connection settings and ensure you are using a valid IP and Port!");
            }

            _client.OnAuth += OnAuthConnTest;
        }

        private async void OnAuthConnTest(bool authSuccess)
        {
            _client.OnAuth -= OnAuthConnTest;

            var result = await _client.GetInfo();

            await Dispatcher.Invoke(async () =>
            {
                ConnectBtn.Content = authSuccess ? "Disconnect" : "Connect";

                if (result == null)
                {
                    await this.ShowMessageAsync("Connection Test", "Connection test failed! Did not receive info response!");
                    return;
                }

                await this.ShowMessageAsync("Connection Test", $"Connection test success! Server responsed with: \"{result}\"");
            });
        }

        private void OnAuth(bool state)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectBtn.Content = state ? "Disconnect" : "Connect";

                if (!state)
                {
                    this.ShowMessageAsync("Authentication Failed!", "Please check your rcon password and make sure it makes the one set in your PalWorldSettings.ini!");
                }
            });

            if (!state) return;

            //Startup player update thread?
            _worker = new BackgroundWorker();
            _worker.DoWork += PlayerUpdateWorker;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync();
        }

        private async void ConnectButton(object sender, RoutedEventArgs e)
        {
            if (_client.Status == ClientStatus.Connected)
            {
                _client.Disconnect();
                return;
            }

            if (!_client.ConnectToRCON())
            {
                await this.ShowMessageAsync("Oops!", "It seems you have invalid settings, please check your server connection settings and ensure you are using a valid IP and Port!");
            }
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
            var shutdownText = await this.ShowInputAsync("Are you sure?", $"Are you sure you wish to shutdown the Palworld server?\n\nEnter a shutdown notice:", new MetroDialogSettings {AffirmativeButtonText = "Shutdown", NegativeButtonText = "Cancel", DefaultText = "Server shutting down in 30 seconds!"});
            if (shutdownText == null) return;

            _client.DoQuit(shutdownText);
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
            if (!string.IsNullOrWhiteSpace(resp.Body))
                await this.ShowMessageAsync("Kick", resp.Body);
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
            if (!string.IsNullOrWhiteSpace(resp.Body))
                await this.ShowMessageAsync("Ban", resp.Body);
        }

        private async void CopyName(object sender, RoutedEventArgs e)
        {
            if (PlayerList.SelectedItem == null)
            {
                await this.ShowMessageAsync("Oops", "You need to select a player before using this command!");
                return;
            }

            var player = PlayerList.SelectedItem as Player;

            if (player == null || string.IsNullOrWhiteSpace(player.PlayerName)) return;

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

            if (player == null || string.IsNullOrWhiteSpace(player.CharacterID)) return;

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

            if (player == null || string.IsNullOrWhiteSpace(player.SteamID)) return;

            Clipboard.SetText(player.SteamID);
        }

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/ddakebono/PalworldRcon",
                UseShellExecute = true
            });
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

        private async void ConsoleInputKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Return)
            {
                //Send this as a raw rcon command
                var command = ConsoleInput.Text;
                ConsoleInput.Text = "";
                var resp = await _client.SendRawCommand(command);
                Log.Info(resp.Body);
            }
        }

        private void ConsoleBlockScrollUpdate(object sender, ScrollChangedEventArgs e)
        {
            if(e.OriginalSource is ScrollViewer sc)
                sc.ScrollToBottom();
        }
    }
}