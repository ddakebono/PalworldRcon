using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PalworldRcon.Logging;
using PalworldRcon.Logging.Targets;
using PalworldRcon.Logic;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace PalworldRcon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static MainWindow Instance;
        public ObservableCollection<Player> Players { get; } = new();
        public Notifier Notifier { get; private set; }

        private Settings _settings;
        private RCONClient _client;
        private Task _worker;
        private bool _stopping;

        public MainWindow()
        {
            Instance = this;

            Log.Info("Startup");

            Notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomLeft,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

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
            DebugMode.IsChecked = _settings.DebugMode;
        }

        private void OnDisconnect()
        {
            Log.Info("OnDisconnect called");

            Dispatcher.Invoke(() =>
            {
                ConnectBtn.Content = "Connect";
                Players.Clear();
            });
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
            if(DebugMode.IsChecked.HasValue)
                _settings.DebugMode = DebugMode.IsChecked.Value;
            _settings.SaveSettings();

            Notifier.ShowSuccess("Settings Saved!");
        }

        private async void TryConnection(object sender, RoutedEventArgs e)
        {
            SaveSettings(sender, e);

            var success = await _client.ConnectToRCON(true);

            if (success)
            {
                await this.ShowMessageAsync("Connection Test", $"Successfully connected to Palworld server!");
            }
            else
            {
                await this.ShowMessageAsync("Connection Test", "Connection test failed! Did not receive info response!");
            }

            ConnectBtn.Content = "Connect";
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

            //Setup player update task
            _worker = Task.Run(PlayerUpdateWorker);
        }

        private async void ConnectButton(object sender, RoutedEventArgs e)
        {
            if (_client.IsConnectedAndAuthed.HasValue && _client.IsConnectedAndAuthed.Value)
            {
                //Disconnect!
                _client.Disconnect();
                return;
            }

            if (!await _client.ConnectToRCON())
            {
                await this.ShowMessageAsync("Oops!", "It seems you have invalid settings, please check your server connection settings and ensure you are using a valid IP and Port!");
                return;
            }

            Notifier.ShowSuccess("Connected to Palworld server!");
        }

        private void SendNotice(object sender, RoutedEventArgs e)
        {
            this.ShowInputAsync("Notice", "Enter a notice").ContinueWith(async task =>
            {
                //Entered and submitted
                if(!task.IsCompletedSuccessfully) return;
                var resp = await _client.SendNotice(task.Result);
                Notifier.ShowSuccess(resp);
            });
        }

        private async void KillServer(object sender, RoutedEventArgs e)
        {
            var shutdownText = await this.ShowInputAsync("Are you sure?", $"Are you sure you wish to shutdown the Palworld server?\n\nEnter a shutdown notice:", new MetroDialogSettings {AffirmativeButtonText = "Shutdown", NegativeButtonText = "Cancel", DefaultText = "Server shutting down in 30 seconds!"});
            if (shutdownText == null) return;

            var resp = await _client.DoQuit(shutdownText);

            Notifier.ShowInformation(resp);
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            var resp = await _client.Save();

            Notifier.ShowSuccess(resp);
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
                Notifier.ShowSuccess(resp);
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
                Notifier.ShowSuccess(resp);
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

            Notifier.ShowSuccess("Copied name to clipboard!");
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

            Notifier.ShowInformation("Character ID copied!");
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

            Notifier.ShowInformation("Steam ID copied!");
        }

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/ddakebono/PalworldRcon",
                UseShellExecute = true
            });
        }

        private async Task PlayerUpdateWorker()
        {
            bool firstPoll = false;

            while (!_stopping)
            {
                if(firstPoll)
                    await Task.Delay(10000);

                firstPoll = true;

                if (!_client.IsConnectedAndAuthed.HasValue || !_client.IsConnectedAndAuthed.Value) continue;

                //Let's only update players every 10 seconds
                var list = _client.GetPlayers().GetAwaiter().GetResult();

                if (list == null) continue;



                foreach (var player in Players.Where(x => list.Players.All(np => np.SteamID != x.SteamID)).ToList())
                {
                    Dispatcher.Invoke(() => Players.Remove(player));

                    await _client.SendNotice($"{player.PlayerName} has disconnected!");
                    Dispatcher.Invoke(() => Notifier.ShowInformation($"Player {player.PlayerName} has disconnected!"));
                }

                foreach (var player in list.Players)
                {
                    if (Players.Any(p => p.SteamID == player.SteamID)) continue;
                    Dispatcher.Invoke(() => Players.Add(player));

                    await _client.SendNotice($"{player.PlayerName} has connected!");
                    Notifier.ShowInformation($"Player {player.PlayerName} has connected!");
                }
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
                Log.Info(resp);
            }
        }

        private void ConsoleBlockScrollUpdate(object sender, ScrollChangedEventArgs e)
        {
            if(e.OriginalSource is ScrollViewer sc)
                sc.ScrollToBottom();
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            _stopping = true;
        }
    }
}