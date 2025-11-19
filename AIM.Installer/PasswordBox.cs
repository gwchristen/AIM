using System;
using System.Drawing;
using System.Windows.Forms;

namespace AIM.Installer
{
    /// <summary>
    /// Custom password text box control with show/hide functionality.
    /// </summary>
    public class PasswordBox : UserControl
    {
        private TextBox textBox;
        private CheckBox showPasswordCheckBox;

        public PasswordBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.textBox = new TextBox();
            this.showPasswordCheckBox = new CheckBox();
            this.SuspendLayout();

            // textBox
            this.textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.textBox.Location = new Point(0, 0);
            this.textBox.Name = "textBox";
            this.textBox.Size = new Size(300, 23);
            this.textBox.TabIndex = 0;
            this.textBox.UseSystemPasswordChar = true;

            // showPasswordCheckBox
            this.showPasswordCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.showPasswordCheckBox.AutoSize = true;
            this.showPasswordCheckBox.Location = new Point(310, 2);
            this.showPasswordCheckBox.Name = "showPasswordCheckBox";
            this.showPasswordCheckBox.Size = new Size(108, 19);
            this.showPasswordCheckBox.TabIndex = 1;
            this.showPasswordCheckBox.Text = "Show Password";
            this.showPasswordCheckBox.UseVisualStyleBackColor = true;
            this.showPasswordCheckBox.CheckedChanged += ShowPasswordCheckBox_CheckedChanged;

            // PasswordBox
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.showPasswordCheckBox);
            this.Name = "PasswordBox";
            this.Size = new Size(420, 26);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ShowPasswordCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            textBox.UseSystemPasswordChar = !showPasswordCheckBox.Checked;
        }

        /// <summary>
        /// Gets or sets the password text.
        /// </summary>
        public string Password
        {
            get => textBox.Text;
            set => textBox.Text = value;
        }

        /// <summary>
        /// Clears the password text.
        /// </summary>
        public void Clear()
        {
            textBox.Clear();
        }

        /// <summary>
        /// Sets focus to the password text box.
        /// </summary>
        public new void Focus()
        {
            textBox.Focus();
        }
    }
}
