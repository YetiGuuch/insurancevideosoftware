using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

internal sealed class Launcher : Form
{
    private readonly Label status;
    private readonly Button open;
    private readonly System.Windows.Forms.Timer timer;
    private string session;
    private string url;
    private Process server;
    private DateTime deadline;
    private bool reportedFailure;
    private bool exiting;

    public Launcher()
    {
        Text = "Video Activity Review";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(470, 155);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        Font = SystemFonts.MessageBoxFont;

        status = new Label { Left = 22, Top = 20, Width = 426, Height = 56, Text = "Starting Video Activity Review…" };
        open = new Button { Left = 22, Top = 88, Width = 152, Height = 33, Text = "Open review window", Enabled = false };
        var exit = new Button { Left = 344, Top = 88, Width = 104, Height = 33, Text = "Exit" };
        Controls.AddRange(new Control[] { status, open, exit });
        open.Click += delegate { OpenBrowser(); };
        exit.Click += delegate { exiting = true; Close(); };
        FormClosing += delegate(object sender, FormClosingEventArgs args) {
            if (!exiting && args.CloseReason == CloseReason.UserClosing && server != null && !server.HasExited) {
                args.Cancel = true;
                WindowState = FormWindowState.Minimized;
            }
        };

        timer = new System.Windows.Forms.Timer { Interval = 250 };
        timer.Tick += delegate { CheckServer(); };
        Shown += delegate {
            try {
                StartServer(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VideoActivityReview"));
                timer.Start();
            } catch (Exception error) {
                status.Text = "Could not start the app. " + error.Message;
            }
        };
        FormClosed += delegate { StopServer(); };
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--verify") return Verify(args[1]);
        if (args.Length != 0) return 2;

        using (var unique = new Mutex(false, @"Local\VideoActivityReview")) {
            bool acquired;
            try { acquired = unique.WaitOne(0); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) {
                MessageBox.Show("Video Activity Review is already running. Use its Open review window button.", "Video Activity Review");
                return 0;
            }
            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Launcher());
            } finally {
                unique.ReleaseMutex();
            }
        }
        return 0;
    }

    private static int Verify(string data)
    {
        using (var app = new Launcher()) {
            try {
                app.StartServer(Path.GetFullPath(data));
                string address;
                while ((address = app.ReadAddress()) == null && !app.server.HasExited && DateTime.UtcNow < app.deadline) {
                    Thread.Sleep(100);
                }
                if (address == null) return 3;
                using (var client = new System.Net.WebClient()) {
                    client.Proxy = null;
                    var page = client.DownloadString(address);
                    return page.Contains("Claimant details") ? 0 : 4;
                }
            } catch {
                return 5;
            } finally {
                app.StopServer();
            }
        }
    }

    private static void Extract(string resource, string path, bool compressed)
    {
        using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
        using (var destination = File.Create(path)) {
            if (compressed) {
                using (var decoder = new GZipStream(source, CompressionMode.Decompress)) decoder.CopyTo(destination);
            } else {
                source.CopyTo(destination);
            }
        }
    }

    private void StartServer(string data)
    {
        session = Path.Combine(data, "session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(session);
        foreach (var asset in new[] { "index.html", "server.mjs", "engine.mjs", "NODE-LICENSE.txt" }) {
            Extract(asset, Path.Combine(session, asset), false);
        }
        var node = Path.Combine(session, "node.exe");
        Extract("node.exe.gz", node, true);

        var start = new ProcessStartInfo(node, "server.mjs") {
            WorkingDirectory = session, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.EnvironmentVariables.Remove("NODE_OPTIONS");
        start.EnvironmentVariables.Remove("VIDEO_REVIEW_OLLAMA");
        start.EnvironmentVariables["VIDEO_REVIEW_PORT"] = "0";
        start.EnvironmentVariables["VIDEO_REVIEW_DATA"] = data;
        start.EnvironmentVariables["VIDEO_REVIEW_READY_FILE"] = Path.Combine(session, "ready");
        server = Process.Start(start);
        server.BeginOutputReadLine();
        server.BeginErrorReadLine();
        deadline = DateTime.UtcNow.AddSeconds(30);
    }

    private string ReadAddress()
    {
        var path = Path.Combine(session, "ready");
        if (!File.Exists(path)) return null;
        Uri address;
        var value = File.ReadAllText(path).Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out address) ||
            address.Scheme != "http" || address.Host != "127.0.0.1" || address.AbsolutePath != "/") return null;
        return value;
    }

    private void CheckServer()
    {
        if (server.HasExited || (url == null && DateTime.UtcNow >= deadline)) {
            timer.Stop();
            open.Enabled = false;
            if (!reportedFailure) status.Text = "The local review window could not start. Close this app and try again.";
            return;
        }
        if (url != null) return;
        url = ReadAddress();
        if (url == null) return;
        open.Enabled = true;
        status.Text = "The review interface opens in your browser. You can minimize this launcher. Exit stops analysis and disconnects the browser.";
        OpenBrowser();
    }

    private void OpenBrowser()
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch {
            reportedFailure = true;
            status.Text = "Open this address in Edge or Chrome: " + url;
        }
    }

    private void StopServer()
    {
        timer.Stop();
        if (server != null) {
            try {
                if (!server.HasExited) {
                    server.StandardInput.Close();
                    if (!server.WaitForExit(3000)) {
                        server.Kill();
                        server.WaitForExit(3000);
                    }
                }
            } catch (InvalidOperationException) {
            } finally {
                server.Dispose();
                server = null;
            }
        }
        if (session != null && Directory.Exists(session)) {
            try { Directory.Delete(session, true); }
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
        }
    }
}
