using System;
using System.Drawing;
using System.Windows.Forms;

namespace AIM.Installer
{
    /// <summary>
    /// Dialog for prompting the user to enter a passphrase for shared security configuration.
    /// </summary>
    public class PassphrasePrompt : Form
    {
        private Label promptLabel;
        private PasswordBox passphraseBox;
        private Button okButton;
        private Button cancelButton;

        public PassphrasePrompt()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.promptLabel = new Label();
            this.passphraseBox = new PasswordBox();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();

            // promptLabel
            this.promptLabel.AutoSize = true;
            this.promptLabel.Location = new Point(12, 12);
            this.promptLabel.Name = "promptLabel";
            this.promptLabel.Size = new Size(420, 30);
            this.promptLabel.TabIndex = 0;
            this.promptLabel.Text = "Enter a passphrase to secure the shared security configuration.\n" +
                                   "This passphrase will be required when running Deploy-AIM.ps1:";

            // passphraseBox
            this.passphraseBox.Location = new Point(12, 50);
            this.passphraseBox.Name = "passphraseBox";
            this.passphraseBox.Size = new Size(420, 26);
            this.passphraseBox.TabIndex = 1;

            // okButton
            this.okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.okButton.Location = new Point(276, 90);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += OkButton_Click;

            // cancelButton
            this.cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.cancelButton.DialogResult = DialogResult.Cancel;
            this.cancelButton.Location = new Point(357, 90);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;

            // PassphrasePrompt
            this.AcceptButton = this.okButton;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new Size(444, 125);
            this.Controls.Add(this.promptLabel);
            this.Controls.Add(this.passphraseBox);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PassphrasePrompt";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Security Passphrase";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(passphraseBox.Password))
            {
                MessageBox.Show("Please enter a passphrase.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passphraseBox.Focus();
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Gets the entered passphrase.
        /// </summary>
        public string Passphrase => passphraseBox.Password;

        /// <summary>
        /// Shows the passphrase prompt dialog.
        /// </summary>
        /// <param name="owner">The owner window.</param>
        /// <param name="passphrase">The entered passphrase if OK was clicked.</param>
        /// <returns>True if OK was clicked, false otherwise.</returns>
        public static bool ShowDialog(IWin32Window? owner, out string passphrase)
        {
            using (var prompt = new PassphrasePrompt())
            {
                var result = prompt.ShowDialog(owner);
                passphrase = prompt.Passphrase;
                return result == DialogResult.OK;
            }
        }
    }
}
