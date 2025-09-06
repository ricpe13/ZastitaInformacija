using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class EncryptWindow : Form
    {
        private readonly AppSettings _settings;


        private readonly TextBox txtInput = new() { ReadOnly = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        private readonly Button btnBrowseIn = new() { Text = "Odaberi fajl", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 240, Margin = new Padding(0, 4, 0, 4) };

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
            Visible = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Padding = new Padding(0, 8, 8, 0), Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, ForeColor = Color.DimGray, Visible = false, Padding = new Padding(6, 8, 0, 0) };

        private readonly TextBox txtOutput = new() { ReadOnly = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        private readonly Button btnBrowseOut = new() { Text = "Promeni izlaznu putanju", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };

        private readonly Button btnEncrypt = new() { Text = "Kodiraj", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 8, 14, 8) };
        private readonly Button btnClose = new() { Text = "Zatvori", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 8, 14, 8) };

        private readonly OpenFileDialog ofd = new()
        {
            Title = "Odaberi ulazni fajl",
            Filter = "Svi fajlovi (*.*)|*.*"
        };

        private readonly SaveFileDialog sfd = new()
        {
            Title = "Sačuvaj kodirani fajl",
            Filter = "Svi fajlovi (*.*)|*.*"
        };

        public EncryptWindow(AppSettings settings)
        {
            _settings = settings;


            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Ručna enkripcija";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 620);


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
            grid.Controls.Add(new Label { Text = "Ulaz:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 0);
            grid.Controls.Add(txtInput, 1, 0);
            grid.Controls.Add(btnBrowseIn, 2, 0);


            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Algoritam:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 1);
            cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;
            grid.Controls.Add(cmbAlgo, 1, 1);


            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Ključ (16):", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 2);
            grid.Controls.Add(txtKey, 1, 2);
            grid.Controls.Add(lblKeyBytes, 2, 2);


            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(lblNonce, 0, 3);
            grid.Controls.Add(txtNonce, 1, 3);
            grid.Controls.Add(lblNonceBytes, 2, 3);


            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = "Izlaz:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) }, 0, 4);
            grid.Controls.Add(txtOutput, 1, 4);
            grid.Controls.Add(btnBrowseOut, 2, 4);


            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
                AutoSize = true
            };
            buttons.Controls.Add(btnClose);
            buttons.Controls.Add(btnEncrypt);


            var filler = new Panel { Dock = DockStyle.Fill };


            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.Controls.Add(grid, 0, 0);
            main.Controls.Add(buttons, 0, 1);
            main.Controls.Add(filler, 0, 2);
            Controls.Add(main);


            btnBrowseIn.Click += (_, __) => BrowseInput();
            btnBrowseOut.Click += (_, __) => BrowseOutput();
            btnEncrypt.Click += (_, __) => DoEncrypt();
            btnClose.Click += (_, __) => Close();

            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateAlgoUi();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();


            UpdateAlgoUi();
            UpdateKeyBytes();
            UpdateNonceBytes();
        }

        private void BrowseInput()
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            txtInput.Text = ofd.FileName;
            ProposeOutputPath();
        }

        private void BrowseOutput()
        {
            ProposeOutputPath();
            try
            {
                sfd.InitialDirectory = Directory.Exists(Path.GetDirectoryName(txtOutput.Text)!)
                    ? Path.GetDirectoryName(txtOutput.Text)
                    : _settings.EncryptedFolder;
            }
            catch { }
            sfd.FileName = Path.GetFileName(txtOutput.Text);
            if (sfd.ShowDialog(this) == DialogResult.OK)
                txtOutput.Text = sfd.FileName;
        }

        private void UpdateAlgoUi()
        {
            bool isCtr = (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");
            txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
            ProposeOutputPath();
        }

        private void ProposeOutputPath()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtInput.Text) || !File.Exists(txtInput.Text))
                {
                    txtOutput.Text = "";
                    return;
                }
                string ext = cmbAlgo.SelectedItem?.ToString() switch
                {
                    "TEA" => ".tea",
                    "LEA" => ".lea",
                    "LEA-CTR" => ".ctr",
                    _ => ".enc"
                };
                string name = Path.GetFileName(txtInput.Text) + ext;
                string destDir = Directory.Exists(_settings.EncryptedFolder) ? _settings.EncryptedFolder : Path.GetDirectoryName(txtInput.Text)!;
                string dest = Path.Combine(destDir, name);
                txtOutput.Text = dest;
            }
            catch { txtOutput.Text = ""; }
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

        private void DoEncrypt()
        {

            if (!File.Exists(txtInput.Text))
            {
                MessageBox.Show("Ulazni fajl nije izabran ili ne postoji.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            if (keyLen != 16)
            {
                MessageBox.Show("Ključ mora biti tačno 16 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);

            string algo = cmbAlgo.SelectedItem?.ToString() ?? "TEA";
            byte[]? nonce = null;
            if (algo == "LEA-CTR")
            {
                int nonceLen = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
                if (nonceLen != 8)
                {
                    MessageBox.Show("Nonce mora biti tačno 8 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                nonce = Encoding.UTF8.GetBytes(txtNonce.Text);
            }

            if (string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                MessageBox.Show("Izlazna putanja nije definisana.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(txtOutput.Text)!);
                byte[] input = File.ReadAllBytes(txtInput.Text);
                byte[] enc;

                switch (algo)
                {
                    case "TEA":
                        enc = TEA.Encrypt(input, key);
                        break;
                    case "LEA":
                        enc = LEA.Encrypt(input, key);
                        break;
                    case "LEA-CTR":
                        enc = CTR.Process(input, key, nonce!);
                        break;
                    default:
                        throw new InvalidOperationException("Nepoznat algoritam.");
                }

                File.WriteAllBytes(txtOutput.Text, enc);
                MessageBox.Show("Fajl je uspešno kodiran:\n" + txtOutput.Text, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri kodiranju: " + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
