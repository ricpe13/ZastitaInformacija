using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class MainForm : Form
    {

        private TextBox txtTarget = new() { Dock = DockStyle.Fill };
        private TextBox txtX = new() { Dock = DockStyle.Fill };
        private TextBox txtRecv = new() { Dock = DockStyle.Fill };

        private Button btnBrowseTarget = new() { Text = "..." };
        private Button btnBrowseX = new() { Text = "..." };
        private Button btnBrowseRecv = new() { Text = "..." };
        private Button btnSavePaths = new() { Text = "Sačuvaj putanje" };


        private Button btnFSW = new() { Text = "1) Pokreni FSW", Enabled = false };
        private Button btnEnc = new() { Text = "2) Kodiraj fajl ručno", Enabled = false };
        private Button btnDec = new() { Text = "3) Dekodiraj fajl ručno", Enabled = false };
        private Button btnSend = new() { Text = "4) Pošalji i kodiraj (TCP)", Enabled = false };
        private Button btnRecv = new() { Text = "5) Primi fajlove (TCP)", Enabled = false };
        private Button btnSha = new() { Text = "6) Izračunaj SHA-256", Enabled = false };

        private StatusStrip status = new();
        private ToolStripStatusLabel lblStatus = new() { Text = "Spremno." };

        private FolderBrowserDialog fbd = new();
        private OpenFileDialog ofd = new()
        {
            Title = "Odaberi fajl",
            Filter = "Svi fajlovi (*.*)|*.*"
        };

        private AppSettings settings;

        public MainForm()
        {

            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "Zastita informacija";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 560);


            status.Items.Add(lblStatus);

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

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Target:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            grid.Controls.Add(txtTarget, 1, 0);
            grid.Controls.Add(btnBrowseTarget, 2, 0);

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "X (šifrovani):", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            grid.Controls.Add(txtX, 1, 1);
            grid.Controls.Add(btnBrowseX, 2, 1);

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Primljeni:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 2);
            grid.Controls.Add(txtRecv, 1, 2);
            grid.Controls.Add(btnBrowseRecv, 2, 2);

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(btnSavePaths, 1, 3);


            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                AutoScroll = true
            };

            actions.Controls.Add(new Label
            {
                Text = "Operacije:",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(0, 0, 0, 4)
            });

            actions.Controls.Add(btnFSW);
            actions.Controls.Add(btnEnc);
            actions.Controls.Add(btnDec);
            actions.Controls.Add(btnSend);
            actions.Controls.Add(btnRecv);
            actions.Controls.Add(btnSha);

            var cmdButtons = new[] { btnFSW, btnEnc, btnDec, btnSend, btnRecv, btnSha };
            foreach (var b in cmdButtons)
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.Padding = new Padding(10, 8, 10, 8);
                b.Margin = new Padding(0, 4, 0, 4);
            }

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.Controls.Add(grid, 0, 0);
            main.Controls.Add(actions, 0, 1);
            main.Controls.Add(status, 0, 2);

            Controls.Add(main);


            settings = Settings.Load();
            txtTarget.Text = settings.TargetFolder;
            txtX.Text = settings.EncryptedFolder;
            txtRecv.Text = settings.ReceivedFolder;


            btnBrowseTarget.Click += (_, __) => BrowseInto(txtTarget);
            btnBrowseX.Click += (_, __) => BrowseInto(txtX);
            btnBrowseRecv.Click += (_, __) => BrowseInto(txtRecv);

            btnSavePaths.Click += (_, __) =>
            {
                try
                {
                    EnsureDir(txtTarget.Text);
                    EnsureDir(txtX.Text);
                    EnsureDir(txtRecv.Text);

                    settings.TargetFolder = txtTarget.Text;
                    settings.EncryptedFolder = txtX.Text;
                    settings.ReceivedFolder = txtRecv.Text;
                    Settings.Save(settings);

                    lblStatus.Text = "Putanje sačuvane.";
                    EnableFeatureButtons(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };


            bool pathsOk = Directory.Exists(settings.TargetFolder)
                        && Directory.Exists(settings.EncryptedFolder)
                        && Directory.Exists(settings.ReceivedFolder);
            EnableFeatureButtons(pathsOk);


            btnFSW.Click += (_, __) =>
            {
                using var win = new FswWindow(settings);
                win.ShowDialog(this);
            };

            btnEnc.Click += (_, __) =>
            {
                using var win = new EncryptWindow(settings);
                win.ShowDialog(this);
            };

            btnDec.Click += (_, __) =>
            {
                using var win = new DecryptWindow(settings);
                win.ShowDialog(this);
            };

            btnSend.Click += (_, __) =>
            {
                using var win = new SendWindow(settings);
                win.ShowDialog(this);
            };

            btnRecv.Click += (_, __) =>
            {
                using var win = new ReceiveWindow(settings);
                win.ShowDialog(this);
            };


            btnSha.Click += BtnSha_Click;
        }


        private void BtnSha_Click(object? sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            string path = ofd.FileName;
            try
            {
                string hex = ComputeSha256HexStreaming(path);
                using var dlg = new Sha256Dialog(path, hex);
                dlg.ShowDialog(this);

                lblStatus.Text = "SHA-256 izračunat.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri heširanju: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ComputeSha256HexStreaming(string filePath)
        {
            const int BUF = 1 * 1024 * 1024;
            using var sha = SHA256.Create();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BUF, FileOptions.SequentialScan);

            byte[] buffer = new byte[BUF];
            int read;
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                sha.TransformBlock(buffer, 0, read, null, 0);
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            return ToHex(sha.Hash!);
        }

        private static string ToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }



        private void EnableFeatureButtons(bool enabled)
        {
            btnFSW.Enabled = enabled;
            btnEnc.Enabled = enabled;
            btnDec.Enabled = enabled;
            btnSend.Enabled = enabled;
            btnRecv.Enabled = enabled;
            btnSha.Enabled = enabled;
        }

        private void BrowseInto(TextBox target)
        {
            try
            {
                if (Directory.Exists(target.Text))
                    fbd.SelectedPath = target.Text;
            }
            catch { }

            if (fbd.ShowDialog(this) == DialogResult.OK)
                target.Text = fbd.SelectedPath;
        }

        private static void EnsureDir(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Putanja ne može biti prazna.");
            Directory.CreateDirectory(path);
        }
    }
}
