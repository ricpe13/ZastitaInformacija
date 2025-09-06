using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class DecryptPromptDialog : Form
    {
        private readonly FileReceiver.ReceivedFileInfo _info;

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Visible = false, Width = 220 };
        private readonly Label lblAlgoDetected = new() { AutoSize = true, ForeColor = Color.ForestGreen, Visible = false };

        private readonly TextBox txtKey = new()
        {
            PlaceholderText = "Ključ – tačno 16 bajtova",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(420, 0)
        };
        private readonly Label lblKeyBytes = new() { AutoSize = true, ForeColor = Color.DimGray };

        private readonly TextBox txtNonce = new()
        {
            PlaceholderText = "Nonce – tačno 8 bajtova",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(320, 0),
            Visible = false
        };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Padding = new Padding(0, 6, 8, 0), Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, ForeColor = Color.DimGray, Visible = false };

        private readonly Button btnOk = new() { Text = "OK", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };
        private readonly Button btnCancel = new() { Text = "Otkaži", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 8, 12, 8) };

        private string _algoToUse = "TEA";

        public DecryptPromptDialog(FileReceiver.ReceivedFileInfo info)
        {
            _info = info;

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Dešifrovanje primljenog fajla";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 380);

            var title = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Text = $"Primljen fajl: {info.FileName}"
            };


            var algoRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            algoRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            algoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            algoRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            algoRow.Controls.Add(new Label { Text = "Algoritam:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);

            if (info.DetectedAlgorithm is string detected)
            {
                _algoToUse = detected;
                lblAlgoDetected.Text = detected;
                lblAlgoDetected.Visible = true;
                algoRow.Controls.Add(lblAlgoDetected, 1, 0);
            }
            else
            {
                cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
                cmbAlgo.SelectedIndex = 0;
                cmbAlgo.Visible = true;
                algoRow.Controls.Add(cmbAlgo, 1, 0);
                cmbAlgo.SelectedIndexChanged += (_, __) => UpdateCtrVisibility(GetSelectedAlgo());
            }


            var keyRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            keyRow.Controls.Add(new Label { Text = "Ključ (16):", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            keyRow.Controls.Add(txtKey, 1, 0);
            keyRow.Controls.Add(lblKeyBytes, 2, 0);


            var nonceRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            nonceRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nonceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            nonceRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            nonceRow.Controls.Add(lblNonce, 0, 0);
            nonceRow.Controls.Add(txtNonce, 1, 0);
            nonceRow.Controls.Add(lblNonceBytes, 2, 0);


            var outHint = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = $"Izlaz (podrazumevano): {_info.DefaultDecryptedPath}"
            };


            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnOk);

            var main = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12),
                AutoScroll = true
            };
            main.Controls.Add(title);
            main.Controls.Add(algoRow);
            main.Controls.Add(keyRow);
            main.Controls.Add(nonceRow);
            main.Controls.Add(outHint);
            main.Controls.Add(buttons);
            Controls.Add(main);


            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();
            btnOk.Click += (_, __) => OnOk();
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };


            UpdateKeyBytes();
            UpdateNonceBytes();
            UpdateCtrVisibility(info.DetectedAlgorithm ?? GetSelectedAlgo());
        }

        private string GetSelectedAlgo()
            => cmbAlgo.Visible ? (cmbAlgo.SelectedItem?.ToString() ?? "TEA") : (_info.DetectedAlgorithm ?? "TEA");

        private void UpdateCtrVisibility(string algo)
        {
            bool isCtr = algo == "LEA-CTR";
            txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
            _algoToUse = algo;
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

        private void OnOk()
        {
            int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            if (keyLen != 16)
            {
                MessageBox.Show("Ključ mora biti tačno 16 bajtova", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_algoToUse == "LEA-CTR")
            {
                int nLen = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
                if (nLen != 8)
                {
                    MessageBox.Show("Nonce mora biti tačno 8 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        public FileReceiver.DecryptParams GetParams()
        {
            var algo = _algoToUse;
            byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);
            byte[]? nonce = null;

            if (algo == "LEA-CTR")
                nonce = Encoding.UTF8.GetBytes(txtNonce.Text);


            return new FileReceiver.DecryptParams(algo, key, nonce, outputPath: null);
        }
    }
}
