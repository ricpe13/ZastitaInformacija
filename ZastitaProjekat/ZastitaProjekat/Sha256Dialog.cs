using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class Sha256Dialog : Form
    {
        private readonly TextBox txtPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
        private readonly TextBox txtHash = new()
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 11),
            WordWrap = true
        };

        private readonly Button btnCopy = new() { Text = "Kopiraj" };
        private readonly Button btnSave = new() { Text = "Sačuvaj u .txt" };
        private readonly Button btnClose = new() { Text = "Zatvori" };

        public Sha256Dialog(string filePath, string hexHash)
        {
            Text = "SHA-256 rezultat";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(660, 240);

            txtPath.Text = filePath;
            txtHash.Text = hexHash;

            var g = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10)
            };
            g.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            g.Controls.Add(new Label { Text = "Fajl:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            g.Controls.Add(txtPath, 1, 0);

            g.Controls.Add(new Label { Text = "SHA-256:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            g.Controls.Add(txtHash, 1, 1);

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            buttons.Controls.Add(btnClose);
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCopy);

            g.SetColumnSpan(buttons, 2);
            g.Controls.Add(buttons, 0, 3);

            Controls.Add(g);

            btnCopy.Click += (_, __) =>
            {
                try
                {
                    Clipboard.SetText(txtHash.Text);
                    MessageBox.Show("Heš je kopiran u klipbord.", "OK",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ne mogu da kopiram: " + ex.Message, "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnSave.Click += (_, __) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Title = "Sačuvaj SHA-256",
                    Filter = "Tekstualni fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
                    FileName = Path.GetFileName(filePath) + ".sha256.txt"
                };
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, txtHash.Text);
                    MessageBox.Show("Sačuvano.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnClose.Click += (_, __) => Close();
        }
    }
}
