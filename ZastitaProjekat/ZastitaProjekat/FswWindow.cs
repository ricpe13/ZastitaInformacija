using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class FswWindow : Form
    {
        private readonly AppSettings _settings;

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtKey = new()
        {
            PlaceholderText = "Ključ – tačno 16 bajtova",
            MinimumSize = new Size(520, 0),
            Dock = DockStyle.Fill
        };
        private readonly Label lblKeyBytes = new() { AutoSize = true, ForeColor = Color.DimGray };

        private readonly TextBox txtNonce = new()
        {
            PlaceholderText = "Nonce – tačno 8 bajtova",
            Visible = false,
            MinimumSize = new Size(360, 0),
            Dock = DockStyle.Fill
        };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Padding = new Padding(0, 6, 8, 0), Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, ForeColor = Color.DimGray, Visible = false };

        private readonly Button btnStart = new() { Text = "Start nadgledanja", AutoSize = true };
        private readonly Button btnStop = new() { Text = "Stop", AutoSize = true, Enabled = false };
        private readonly Button btnClear = new() { Text = "Očisti log", AutoSize = true };
        private readonly Button btnClose = new() { Text = "Zatvori", AutoSize = true };


        private readonly TextBox txtLog = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10)
        };

        private FileSystemWatcher? _watcher;
        private volatile bool _running = false;

        private static readonly HashSet<string> _processing = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _gate = new();

        public FswWindow(AppSettings settings)
        {
            _settings = settings;

            Text = "FSW – Nadgledanje foldera";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 650);
            AutoScaleMode = AutoScaleMode.Dpi;


            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true,
                Padding = new Padding(10),
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            grid.Controls.Add(new Label { Text = "Algoritam:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;
            grid.Controls.Add(cmbAlgo, 1, 0);


            grid.Controls.Add(new Label { Text = "Ključ (16):", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            grid.Controls.Add(txtKey, 1, 1);
            grid.Controls.Add(lblKeyBytes, 2, 1);


            grid.Controls.Add(lblNonce, 0, 2);
            grid.Controls.Add(txtNonce, 1, 2);
            grid.Controls.Add(lblNonceBytes, 2, 2);


            var lblPaths = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = $"Target:  {_settings.TargetFolder}{Environment.NewLine}X:       {_settings.EncryptedFolder}",
                Padding = new Padding(0, 8, 0, 0)
            };
            grid.Controls.Add(new Label { Text = "Putanje:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 3);
            grid.Controls.Add(lblPaths, 1, 3);
            grid.SetColumnSpan(lblPaths, 2);


            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10),
                AutoSize = true
            };
            foreach (var b in new[] { btnStart, btnStop, btnClear, btnClose })
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.Padding = new Padding(12, 8, 12, 8);
                b.Margin = new Padding(0, 4, 8, 4);
            }
            buttons.Controls.AddRange(new Control[] { btnStart, btnStop, btnClear, btnClose });


            var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            logPanel.Controls.Add(txtLog);


            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.Controls.Add(grid, 0, 0);
            main.Controls.Add(buttons, 0, 1);
            main.Controls.Add(logPanel, 0, 2);

            Controls.Add(main);


            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateCtrVisibility();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();

            btnStart.Click += (_, __) => StartWatch();
            btnStop.Click += (_, __) => StopWatch();
            btnClear.Click += (_, __) => txtLog.Clear();
            btnClose.Click += (_, __) => Close();

            FormClosing += (_, e) => StopWatch();

            UpdateCtrVisibility();
            UpdateKeyBytes();
            UpdateNonceBytes();

            Append($"[FSW] Spreman. Posmatram: {_settings.TargetFolder}");
        }

        private void UpdateCtrVisibility()
        {
            bool isCtr = (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");
            txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
        }

        private void UpdateKeyBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            lblKeyBytes.Text = $"{n} bajtova";
            lblKeyBytes.ForeColor = (n == 16) ? Color.ForestGreen : Color.Firebrick;
        }

        private void UpdateNonceBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
            lblNonceBytes.Text = $"{n} bajtova";
            lblNonceBytes.ForeColor = (n == 8) ? Color.ForestGreen : Color.Firebrick;
        }

        private void StartWatch()
        {
            if (_running) { Append("[FSW] Već radi."); return; }

            int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            if (keyLen != 16)
            {
                MessageBox.Show("Ključ mora biti tačno 16 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(_settings.TargetFolder) || !Directory.Exists(_settings.EncryptedFolder))
            {
                MessageBox.Show("Putanje nisu validne. Otvori glavnu formu i sačuvaj putanje.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _watcher = new FileSystemWatcher(_settings.TargetFolder)
            {
                IncludeSubdirectories = false,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size
            };
            _watcher.Created += OnFileCreated;
            _watcher.Error += (s, e) => Append($"[FSW] Greška watchera: {e.GetException().Message}");
            _watcher.EnableRaisingEvents = true;

            _running = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            Append("[FSW] Nadgledanje pokrenuto.");
        }

        private void StopWatch()
        {
            if (!_running) return;

            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileCreated;
                    _watcher.Dispose();
                    _watcher = null;
                }
            }
            catch { }

            _running = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            Append("[FSW] Zaustavljeno.");
        }

        private void OnFileCreated(object? sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;

            string ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
            if (ext is ".tmp" or ".part" or ".partial" or ".crdownload") return;

            lock (_gate)
            {
                if (_processing.Contains(path)) return;
                _processing.Add(path);
            }

            Task.Run(() => ProcessNewFileSafe(path));
        }

        private void ProcessNewFileSafe(string filePath)
        {
            try
            {
                if (!WaitForFileReady(filePath, maxWaitMs: 30000, settleMs: 400, stableReadsRequired: 2))
                {
                    Append($"[FSW] Fajl nije stabilan ni posle timeout-a: {filePath}");
                    return;
                }

                byte[] data = File.ReadAllBytes(filePath);

                var algo = cmbAlgo.SelectedItem?.ToString() ?? "TEA";
                byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);
                byte[]? nonce = null;
                string outExt;

                switch (algo)
                {
                    case "TEA":
                        data = TEA.Encrypt(data, key);
                        outExt = ".tea";
                        break;
                    case "LEA":
                        data = LEA.Encrypt(data, key);
                        outExt = ".lea";
                        break;
                    case "LEA-CTR":
                        int nLen = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
                        if (nLen != 8)
                        {
                            Append("[FSW] Nonce nije 8 bajtova za LEA-CTR. Fajl preskočen.");
                            return;
                        }
                        nonce = Encoding.UTF8.GetBytes(txtNonce.Text);
                        data = CTR.Process(data, key, nonce);
                        outExt = ".ctr";
                        break;
                    default:
                        Append("[FSW] Nepoznat algoritam. Fajl preskočen.");
                        return;
                }

                string fileName = Path.GetFileName(filePath);
                Directory.CreateDirectory(_settings.EncryptedFolder);
                string outputPath = Path.Combine(_settings.EncryptedFolder, fileName + outExt);

                File.WriteAllBytes(outputPath, data);
                Append($"[FSW] '{fileName}' → {outputPath}");
            }
            catch (Exception ex)
            {
                Append("[FSW] Greška pri šifrovanju: " + ex.Message);
            }
            finally
            {
                lock (_gate) { _processing.Remove(filePath); }
            }
        }

        private static bool WaitForFileReady(string path, int maxWaitMs, int settleMs, int stableReadsRequired)
        {
            var sw = Stopwatch.StartNew();
            long lastLen = -1;
            int stableCount = 0;

            while (sw.ElapsedMilliseconds < maxWaitMs)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        Thread.Sleep(settleMs);
                        continue;
                    }

                    FileInfo fi = new(path);
                    long len = fi.Length;

                    if (len == lastLen)
                    {
                        stableCount++;
                        if (stableCount >= stableReadsRequired)
                            return true;
                    }
                    else
                    {
                        stableCount = 0;
                        lastLen = len;
                    }
                }
                catch
                {

                }

                Thread.Sleep(settleMs);
            }

            return false;
        }

        private void Append(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Append), line);
                return;
            }
            txtLog.AppendText((line?.EndsWith(Environment.NewLine) == true) ? line : line + Environment.NewLine);
        }
    }
}
