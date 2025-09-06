using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class ReceiveWindow : Form
    {
        private readonly AppSettings _settings;

        private readonly TextBox txtPort = new() { Text = "5000", Dock = DockStyle.Left, Width = 120 };

        private readonly TextBox txtIps = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 90,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9)
        };


        private readonly CheckBox chkAuto = new()
        {
            Text = "Dešifruj pri prijemu fajla (tražiće ključ/nonce posle prijema)",
            Checked = true,
            AutoSize = true
        };
        private readonly Label lblHint = new()
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Kada stigne fajl, algoritam se prepoznaje iz ekstenzije (.tea/.lea/.ctr), pa će se tražiti ključ (i nonce za CTR)."
        };

        private readonly Button btnStart = new() { Text = "Start", AutoSize = true };
        private readonly Button btnStop = new() { Text = "Stop", AutoSize = true, Enabled = false };
        private readonly Button btnClear = new() { Text = "Očisti log", AutoSize = true };
        private readonly Button btnClose = new() { Text = "Zatvori", AutoSize = true };

        private readonly TextBox txtLog = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            WordWrap = true
        };

        private FileReceiver? _receiver;

        public ReceiveWindow(AppSettings settings)
        {
            _settings = settings;

            Text = "TCP prijem";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 640);
            AutoScaleMode = AutoScaleMode.Dpi;


            var info = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(10) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            info.Controls.Add(new Label { Text = "Lokalne IP adrese:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            info.Controls.Add(txtIps, 1, 0);
            info.Controls.Add(new Label { Text = "Port za prijem:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            info.Controls.Add(txtPort, 1, 1);


            var grp = new GroupBox { Text = "Auto-dešifrovanje", Dock = DockStyle.Top, Padding = new Padding(10), AutoSize = true };
            var g = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true };
            g.Controls.Add(chkAuto, 0, 0);
            g.Controls.Add(lblHint, 0, 1);
            grp.Controls.Add(g);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10), AutoSize = true };
            buttons.Controls.Add(btnStart);
            buttons.Controls.Add(btnStop);
            buttons.Controls.Add(btnClear);
            buttons.Controls.Add(btnClose);

            var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            logPanel.Controls.Add(txtLog);

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4 };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.Controls.Add(info, 0, 0);
            main.Controls.Add(grp, 0, 1);
            main.Controls.Add(buttons, 0, 2);
            main.Controls.Add(logPanel, 0, 3);
            Controls.Add(main);


            btnStart.Click += (_, __) => StartReceive();
            btnStop.Click += (_, __) => StopReceive();
            btnClear.Click += (_, __) => txtLog.Clear();
            btnClose.Click += (_, __) => Close();

            FormClosing += (_, e) =>
            {
                if (_receiver != null)
                {
                    try { _receiver.StopGui(); } catch { }
                    _receiver = null;
                }
            };


            FillLocalIps();
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

        private void StartReceive()
        {
            if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port nije validan (1-65535).", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_receiver != null)
            {
                AppendLog("[Receiver] Već je pokrenut.");
                return;
            }

            try
            {
                _receiver = new FileReceiver(port, _settings.ReceivedFolder);
                _receiver.SetLogger(AppendLog);

                if (chkAuto.Checked)
                {
                    _receiver.ConfigureAutoDecrypt(true, info =>
                    {

                        using var dlg = new DecryptPromptDialog(info);
                        var res = dlg.ShowDialog(this);
                        if (res != DialogResult.OK) return null;
                        return dlg.GetParams();
                    });
                }
                else
                {
                    _receiver.ConfigureAutoDecrypt(false, null);
                }

                _receiver.StartGui();

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                AppendLog($"[Receiver] Prijem započet. Fajlovi će se čuvati u: {_settings.ReceivedFolder}");
            }
            catch (Exception ex)
            {
                _receiver = null;
                MessageBox.Show("Ne mogu da startujem prijem: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopReceive()
        {
            if (_receiver == null) return;
            try
            {
                _receiver.StopGui();
                AppendLog("[Receiver] Stop komanda poslana.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri zaustavljanju: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _receiver = null;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), text);
                return;
            }
            txtLog.AppendText(text.EndsWith(Environment.NewLine) ? text : text + Environment.NewLine);
        }
    }
}
