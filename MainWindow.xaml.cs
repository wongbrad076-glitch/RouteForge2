using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;

namespace RouteForge;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<RouteResult> _results = new();
    private readonly ObservableCollection<RelayProfile> _relays = new();
    private readonly string _relayFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "RouteForge", "relays.json");

    public MainWindow()
    {
        InitializeComponent();
        ResultsGrid.ItemsSource = _results;
        RelayList.ItemsSource = _relays;
        LoadRelays();
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        var targets = TargetBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (targets.Length == 0) return;

        TestButton.IsEnabled = false;
        _results.Clear();
        StatusText.Text = "Testing latency, jitter, and packet loss...";

        foreach (var target in targets)
        {
            _results.Add(await MeasureAsync(target, 12));
        }

        var best = _results.Where(r => r.LossPercent < 100).OrderBy(r => r.Score).FirstOrDefault();
        StatusText.Text = best is null
            ? "No target responded. ICMP may be blocked, or the address may be unreachable."
            : $"Best measured target: {best.Target}, score {best.Score}. Lower is better.";
        TestButton.IsEnabled = true;
    }

    private static async Task<RouteResult> MeasureAsync(string target, int attempts)
    {
        var samples = new List<long>();
        using var ping = new Ping();

        for (int i = 0; i < attempts; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(target, 1200);
                if (reply.Status == IPStatus.Success)
                    samples.Add(reply.RoundtripTime);
            }
            catch { }

            await Task.Delay(120);
        }

        var loss = 100.0 * (attempts - samples.Count) / attempts;
        if (samples.Count == 0)
            return new RouteResult(target, 0, 0, 100, 10000);

        var avg = samples.Average();
        var jitter = samples.Count < 2
            ? 0
            : samples.Zip(samples.Skip(1), (a, b) => Math.Abs(a - b)).Average();

        // Loss is deliberately expensive because dropped packets hurt games badly.
        var score = avg + (jitter * 2.0) + (loss * 8.0);
        return new RouteResult(target,
            Math.Round(avg, 1),
            Math.Round(jitter, 1),
            Math.Round(loss, 1),
            Math.Round(score, 1));
    }

    private void AddRelayButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "WireGuard configuration (*.conf)|*.conf",
            Title = "Choose a WireGuard relay configuration"
        };

        if (dialog.ShowDialog() != true) return;

        var name = Path.GetFileNameWithoutExtension(dialog.FileName);
        if (_relays.Any(r => string.Equals(r.ConfigPath, dialog.FileName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText.Text = "That relay profile is already listed.";
            return;
        }

        _relays.Add(new RelayProfile(name, dialog.FileName));
        SaveRelays();
        StatusText.Text = $"Added relay profile: {name}";
    }

    private void ConnectRelayButton_Click(object sender, RoutedEventArgs e)
    {
        if (RelayList.SelectedItem is not RelayProfile relay)
        {
            StatusText.Text = "Select a relay profile first.";
            return;
        }

        RunWireGuard($"/installtunnelservice \"{relay.ConfigPath}\"",
            $"Connecting {relay.Name}...");
    }

    private void DisconnectRelayButton_Click(object sender, RoutedEventArgs e)
    {
        if (RelayList.SelectedItem is not RelayProfile relay)
        {
            StatusText.Text = "Select a relay profile first.";
            return;
        }

        RunWireGuard($"/uninstalltunnelservice \"{relay.Name}\"",
            $"Disconnecting {relay.Name}...");
    }

    private void RunWireGuard(string arguments, string status)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WireGuard", "wireguard.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WireGuard", "wireguard.exe")
        };
        var exe = candidates.FirstOrDefault(File.Exists);

        if (exe is null)
        {
            StatusText.Text = "WireGuard for Windows was not found. Install it, then reopen RouteForge.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(exe, arguments)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
            StatusText.Text = status;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"WireGuard action failed: {ex.Message}";
        }
    }

    private void LaunchGameButton_Click(object sender, RoutedEventArgs e)
    {
        var path = GamePathBox.Text.Trim().Trim('"');
        if (!File.Exists(path))
        {
            StatusText.Text = "The game executable path does not exist.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusText.Text = $"Launched {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not launch the game: {ex.Message}";
        }
    }

    private void LoadRelays()
    {
        try
        {
            if (!File.Exists(_relayFile)) return;
            var saved = JsonSerializer.Deserialize<List<RelayProfile>>(File.ReadAllText(_relayFile));
            if (saved is null) return;
            foreach (var relay in saved.Where(r => File.Exists(r.ConfigPath)))
                _relays.Add(relay);
        }
        catch
        {
            StatusText.Text = "Relay profile file could not be read.";
        }
    }

    private void SaveRelays()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_relayFile)!);
        File.WriteAllText(_relayFile,
            JsonSerializer.Serialize(_relays.ToList(), new JsonSerializerOptions { WriteIndented = true }));
    }
}

public record RouteResult(string Target, double AverageMs, double JitterMs,
                          double LossPercent, double Score);

public record RelayProfile(string Name, string ConfigPath);
