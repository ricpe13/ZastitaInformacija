using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class DecryptWindow : Form
    {
        private readonly AppSettings _settings;


        private readonly TextBox txtInput = new() { ReadOnly = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        private readonly Button btnBrowseIn = new() { Text = "Odaberi fajl", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };

        private readonly TextBox txtOutput = new() { ReadOnly = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        private readonly Button btnBrowseOut = new() { Text = "Promeni izlaznu putanju", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };


        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 260, Margin = new Padding(0, 4, 0, 4) };
        private readonly Label lblDetected = new() { AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(6, 8, 0, 0) };


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


        private readonly Button btnDecrypt = new() { Text = "Dekodiraj", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 8, 14, 8) };
        private readonly Button btnClose = new() { Text = "Zatvori", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 8, 14, 8) };


        private readonly OpenFileDialog ofd = new()
        {
            Title = "Odaberi kodirani fajl",
            Filter = "Svi fajlovi (*.*)|*.*"
        };
        private readonly SaveFileDialog sfd = new()
        {
            Title = "Sačuvaj dešifrovan fajl",
            Filter = "Svi fajlovi (*.*)|*.*"
        };

        public DecryptWindow(AppSettings settings)
        {
            _settings = settings;

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Ručna dekripcija";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 650);


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
            cmbAlgo.Items.AddRange(new object[] { "Auto (po ekstenziji)", "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;
            grid.Controls.Add(cmbAlgo, 1, 1);
            grid.Controls.Add(lblDetected, 2, 1);


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
            buttons.Controls.Add(btnDecrypt);


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
            btnDecrypt.Click += (_, __) => DoDecrypt();
            btnClose.Click += (_, __) => Close();

            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateAlgoUi();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();


            UpdateAlgoUi();
            UpdateKeyBytes();
            UpdateNonceBytes();
            UpdateDetectedText(null);
        }



        private void BrowseInput()
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            txtInput.Text = ofd.FileName;


            string? algo = DetectAlgoFromExtension(txtInput.Text);
            UpdateDetectedText(algo);


            if (cmbAlgo.SelectedIndex == 0)
            {
                bool isCtr = algo == "LEA-CTR";
                txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
            }

            ProposeOutputPath();
        }

        private void BrowseOutput()
        {
            ProposeOutputPath();
            try
            {
                var dir = Path.GetDirectoryName(txtOutput.Text);
                sfd.InitialDirectory = Directory.Exists(dir!) ? dir : _settings.ReceivedFolder;
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
                string baseName = StripAlgoExtension(Path.GetFileName(txtInput.Text));
                if (string.Equals(baseName, Path.GetFileName(txtInput.Text), StringComparison.Ordinal))
                {

                    baseName += ".decrypted";
                }


                string? inputDir = Path.GetDirectoryName(txtInput.Text);
                string destDir =
                    (!string.IsNullOrEmpty(inputDir) && Directory.Exists(inputDir))
                        ? inputDir
                        : (Directory.Exists(_settings.ReceivedFolder) ? _settings.ReceivedFolder
                                                                      : Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                txtOutput.Text = Path.Combine(destDir, baseName);
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

        private void UpdateDetectedText(string? algo)
        {
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                lblDetected.Text = "";
                return;
            }

            if (algo is null)
            {
                algo = DetectAlgoFromExtension(txtInput.Text);
            }

            if (algo is null)
            {
                lblDetected.Text = "Detekcija: nepoznato";
                lblDetected.ForeColor = Color.DarkOrange;
            }
            else
            {
                lblDetected.Text = $"Detekcija: {algo}";
                lblDetected.ForeColor = Color.ForestGreen;
            }
        }



        private void DoDecrypt()
        {
            if (!File.Exists(txtInput.Text))
            {
                MessageBox.Show("Ulazni fajl nije izabran ili ne postoji.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            string? algo = null;
            var pick = cmbAlgo.SelectedItem?.ToString();
            if (pick == "Auto (po ekstenziji)")
            {
                algo = DetectAlgoFromExtension(txtInput.Text);
                if (algo is null)
                {
                    MessageBox.Show("Nije moguće automatski odrediti algoritam (ekstenzija nije .tea/.lea/.ctr). Izaberi ručno.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                algo = pick;
            }


            int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            if (keyLen != 16)
            {
                MessageBox.Show("Ključ mora biti tačno 16 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);


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
                byte[] encrypted = File.ReadAllBytes(txtInput.Text);
                byte[] dec = algo switch
                {
                    "TEA" => TEA.Decrypt(encrypted, key),
                    "LEA" => LEA.Decrypt(encrypted, key),
                    "LEA-CTR" => CTR.Process(encrypted, key, nonce!),
                    _ => throw new InvalidOperationException("Nepoznat algoritam.")
                };

                File.WriteAllBytes(txtOutput.Text, dec);
                MessageBox.Show("Fajl je uspešno dešifrovan:\n" + txtOutput.Text, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri dešifrovanju: " + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private static string? DetectAlgoFromExtension(string path)
        {
            string ext = (Path.GetExtension(path) ?? "").ToLowerInvariant();
            return ext switch
            {
                ".tea" => "TEA",
                ".lea" => "LEA",
                ".ctr" => "LEA-CTR",
                _ => null
            };
        }

        private static string StripAlgoExtension(string fileName)
        {
            string ext = (Path.GetExtension(fileName) ?? "").ToLowerInvariant();
            if (ext == ".tea" || ext == ".lea" || ext == ".ctr")
                return fileName.Substring(0, fileName.Length - ext.Length);
            return fileName;
        }
    }
}
