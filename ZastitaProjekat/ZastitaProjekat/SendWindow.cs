using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class SendWindow : Form
    {
        private readonly AppSettings _settings;


        private readonly TextBox txtIp = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        private readonly TextBox txtPort = new() { Dock = DockStyle.Left, Width = 140, Margin = new Padding(0, 4, 0, 4), Text = "5000" };


        private readonly TextBox txtFile = new() { ReadOnly = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        private readonly Button btnBrowse = new() { Text = "Odaberi fajl", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };


        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 260, Margin = new Padding(0, 4, 0, 4) };


        private readonly TextBox txtKey = new()
        {
            PlaceholderText = "Ključ – tačno 16 bajtova",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        private readonly Label lblKeyBytes = new() { AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(6, 8, 0, 0) };

        private readonly TextBox txtNonce = new()
        {
            PlaceholderText = "Nonce – tačno 8 bajtova",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            Visible = false
        };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Padding = new Padding(0, 8, 8, 0), Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(6, 8, 0, 0), Visible = false };


        private readonly Label lblAlreadyEnc = new()
        {
            AutoSize = true,
            ForeColor = Color.ForestGreen,
            Padding = new Padding(0, 8, 0, 0),
            Visible = false,
            Text = "Detektovano: fajl je već šifrovan (.tea/.lea/.ctr) – slanje bez unosa ključa/nonce."
        };


        private readonly TextBox txtIps = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 90,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9)
        };


        private readonly Button btnSend = new() { Text = "Pošalji", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 8, 14, 8) };
        private readonly Button btnClose = new() { Text = "Zatvori", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 8, 14, 8) };
        private readonly Button btnClear = new() { Text = "Očisti log", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };

        private readonly TextBox txtLog = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            WordWrap = true
        };

        private readonly OpenFileDialog ofd = new()
        {
            Title = "Odaberi fajl za slanje",
            Filter = "Svi fajlovi (*.*)|*.*"
        };

        public SendWindow(AppSettings settings)
        {
            _settings = settings;

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "TCP slanje";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 620);


            var top = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(12) };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            top.Controls.Add(new Label { Text = "Lokalne IP adrese:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 0);
            top.Controls.Add(txtIps, 1, 0);

            top.Controls.Add(new Label { Text = "IP primaoca:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 1);
            top.Controls.Add(txtIp, 1, 1);

            top.Controls.Add(new Label { Text = "Port primaoca:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 2);
            top.Controls.Add(txtPort, 1, 2);


            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true,
                Padding = new Padding(12),
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Fajl:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 0);
            grid.Controls.Add(txtFile, 1, 0);
            grid.Controls.Add(btnBrowse, 2, 0);


            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Algoritam:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 1);
            cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;
            grid.Controls.Add(cmbAlgo, 1, 1);
            grid.Controls.Add(lblAlreadyEnc, 2, 1);


            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Ključ (16):", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 2);
            grid.Controls.Add(txtKey, 1, 2);
            grid.Controls.Add(lblKeyBytes, 2, 2);

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(lblNonce, 0, 3);
            grid.Controls.Add(txtNonce, 1, 3);
            grid.Controls.Add(lblNonceBytes, 2, 3);


            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
                AutoSize = true
            };
            foreach (var b in new[] { btnClose, btnSend, btnClear })
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.Margin = new Padding(6, 4, 0, 4);
            }
            buttons.Controls.Add(btnClose);
            buttons.Controls.Add(btnSend);
            buttons.Controls.Add(btnClear);


            var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            logPanel.Controls.Add(txtLog);


            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4 };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.Controls.Add(top, 0, 0);
            main.Controls.Add(grid, 0, 1);
            main.Controls.Add(buttons, 0, 2);
            main.Controls.Add(logPanel, 0, 3);
            Controls.Add(main);


            btnBrowse.Click += (_, __) => BrowseFile();
            btnSend.Click += (_, __) => DoSend();
            btnClose.Click += (_, __) => Close();
            btnClear.Click += (_, __) => txtLog.Clear();

            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateCtrVisibility();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();


            FillLocalIps();
            UpdateKeyBytes();
            UpdateNonceBytes();
            UpdateCtrVisibility();
        }



        private void FillLocalIps()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var props = ni.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !addr.Address.ToString().StartsWith("127."))
                        {
                            sb.AppendLine($"{ni.Name} ({ni.Description}): {addr.Address}");
                        }
                    }
                }
                txtIps.Text = sb.ToString();
            }
            catch { txtIps.Text = "(nije moguće očitati IP adrese)"; }
        }

        private void BrowseFile()
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            txtFile.Text = ofd.FileName;
            UpdateForSelectedFile();
        }

        private void UpdateForSelectedFile()
        {
            bool alreadyEnc = IsAlreadyEncrypted(txtFile.Text);
            lblAlreadyEnc.Visible = alreadyEnc;


            cmbAlgo.Enabled = !alreadyEnc;
            txtKey.Enabled = !alreadyEnc;
            lblKeyBytes.Enabled = !alreadyEnc;

            bool showNonce = !alreadyEnc && (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");
            txtNonce.Visible = showNonce;
            lblNonce.Visible = showNonce;
            lblNonceBytes.Visible = showNonce;
            txtNonce.Enabled = showNonce;
            lblNonceBytes.Enabled = showNonce;
        }

        private void UpdateCtrVisibility()
        {
            bool alreadyEnc = IsAlreadyEncrypted(txtFile.Text);
            bool isCtr = !alreadyEnc && (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");

            txtNonce.Visible = isCtr;
            lblNonce.Visible = isCtr;
            lblNonceBytes.Visible = isCtr;

            txtNonce.Enabled = isCtr;
            lblNonceBytes.Enabled = isCtr;
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



        private void DoSend()
        {

            string ip = txtIp.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("Unesi IP adresu primaoca.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port nije validan (1–65535).", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (!File.Exists(txtFile.Text))
            {
                MessageBox.Show("Fajl nije izabran ili ne postoji.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool alreadyEnc = IsAlreadyEncrypted(txtFile.Text);
            string algorithm = "AUTO";
            byte[]? key = null;
            byte[]? nonce = null;

            if (!alreadyEnc)
            {
                algorithm = cmbAlgo.SelectedItem?.ToString() ?? "TEA";

                int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
                if (keyLen != 16)
                {
                    MessageBox.Show("Ključ mora biti tačno 16 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                key = Encoding.UTF8.GetBytes(txtKey.Text);

                if (algorithm == "LEA-CTR")
                {
                    int nonceLen = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
                    if (nonceLen != 8)
                    {
                        MessageBox.Show("Nonce mora biti tačno 8 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    nonce = Encoding.UTF8.GetBytes(txtNonce.Text);
                }
            }

            try
            {
                Append($"[Sender] Povezivanje na {ip}:{port} …");
                var sender = new FileSender(ip, port);


                bool ok = sender.TrySend(txtFile.Text, algorithm, key, nonce, out var err);

                if (ok)
                {
                    Append($"[Sender] Završeno slanje: {Path.GetFileName(txtFile.Text)}");
                }
                else
                {
                    Append(err ?? "[Sender] Slanje NIJE uspelo.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri slanju: " + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Append("[Sender] Greška: " + ex.Message);
            }
        }



        private static bool IsAlreadyEncrypted(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string ext = (Path.GetExtension(path) ?? "").ToLowerInvariant();
            return ext is ".tea" or ".lea" or ".ctr";
        }

        private void Append(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Append), line);
                return;
            }
            txtLog.AppendText(line.EndsWith(Environment.NewLine) ? line : line + Environment.NewLine);
        }
    }
}
