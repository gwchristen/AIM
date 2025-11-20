using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace AIM.Installer
{
    /// <summary>
    /// Main installer form with wizard-style interface.
    /// </summary>
    public class InstallerForm : Form
    {
        // SECURITY NOTE: This passphrase is obfuscated but NOT cryptographically protected.
        // It can be extracted by examining the installer binary or memory.
        // This obfuscation only prevents casual discovery.
        // For production deployments, consider using Azure Key Vault, domain certificates,
        // or other enterprise secret management solutions.
        
        // Obfuscated passphrase constant (XOR with key + Base64)
        // Original passphrase should be set during build process
        // Example: "MySecureP@ssphrase2024!" would be obfuscated
        // This value was generated using PassphraseObfuscationExample.cs with the correct XOR key
        private const string ObfuscatedPassphrase = "6EUt9CGNH071fA3iMpAfStZZTKFwzEw=";
        
        // UI Controls
        private Panel topPanel;
        private Label titleLabel;
        private Label descriptionLabel;
        private Panel contentPanel;
        private Panel buttonPanel;
        private Button backButton;
        private Button nextButton;
        private Button cancelButton;
        private ProgressBar progressBar;

        // Welcome Page Controls
        private Label welcomeLabel;
        private Label welcomeMessageLabel;

        // Installation Path Page Controls
        private Label installPathLabel;
        private TextBox installPathTextBox;
        private Button browseButton;
        private CheckBox desktopShortcutCheckBox;
        private CheckBox startMenuShortcutCheckBox;

        // Shared Security Path Page Controls
        private Label sharedSecurityLabel;
        private CheckBox enableSharedSecurityCheckBox;
        private TextBox sharedSecurityPathTextBox;
        private Button browseSharedSecurityButton;

        // Progress Page Controls
        private Label progressLabel;
        private RichTextBox logTextBox;
        private CheckBox launchAfterInstallCheckBox;

        // Installation State
        private int currentStep = 0;
        private const int STEP_WELCOME = 0;
        private const int STEP_INSTALL_PATH = 1;
        private const int STEP_SHARED_SECURITY = 2;
        private const int STEP_PROGRESS = 3;
        private const int STEP_COMPLETE = 4;

        private string installPath = @"C:\Program Files\AIM";
        private string? sharedSecurityPath = null;
        private bool installationComplete = false;

        // Network path constants for deployment
        private const string DEFAULT_ROOT_DIRECTORY = @"\\oh1cam01\cml\Internal\LAB STOCK\LAB STOCK";
        private const string ARCHIVE_PATH = @"\\oh1cam01\cml\Internal\LAB STOCK\Archive";
        private const string SHIPPED_DIRECTORY = @"\\oh1cam01\cml\Internal\LAB STOCK\Orders shipped";
        private const string FILE_SCANS_DIRECTORY = @"C:\Tfile";
        private const string INVENTORY_ARCHIVE_DIRECTORY = @"\\oh1cam01\cml\Internal\LAB STOCK\Physical Inventory Archive";

        public InstallerForm()
        {
            InitializeComponent();
            ShowStep(STEP_WELCOME);
        }

        private void InitializeComponent()
        {
            this.topPanel = new Panel();
            this.titleLabel = new Label();
            this.descriptionLabel = new Label();
            this.contentPanel = new Panel();
            this.buttonPanel = new Panel();
            this.backButton = new Button();
            this.nextButton = new Button();
            this.cancelButton = new Button();
            this.progressBar = new ProgressBar();

            // Welcome Page
            this.welcomeLabel = new Label();
            this.welcomeMessageLabel = new Label();

            // Install Path Page
            this.installPathLabel = new Label();
            this.installPathTextBox = new TextBox();
            this.browseButton = new Button();
            this.desktopShortcutCheckBox = new CheckBox();
            this.startMenuShortcutCheckBox = new CheckBox();

            // Shared Security Page
            this.sharedSecurityLabel = new Label();
            this.enableSharedSecurityCheckBox = new CheckBox();
            this.sharedSecurityPathTextBox = new TextBox();
            this.browseSharedSecurityButton = new Button();

            // Progress Page
            this.progressLabel = new Label();
            this.logTextBox = new RichTextBox();
            this.launchAfterInstallCheckBox = new CheckBox();

            this.SuspendLayout();

            // topPanel
            this.topPanel.BackColor = Color.White;
            this.topPanel.BorderStyle = BorderStyle.FixedSingle;
            this.topPanel.Controls.Add(this.titleLabel);
            this.topPanel.Controls.Add(this.descriptionLabel);
            this.topPanel.Dock = DockStyle.Top;
            this.topPanel.Location = new Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new Size(600, 80);
            this.topPanel.TabIndex = 0;

            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.titleLabel.Location = new Point(20, 15);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(200, 25);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "AIM Installer";

            // descriptionLabel
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.Location = new Point(20, 45);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new Size(400, 15);
            this.descriptionLabel.TabIndex = 1;
            this.descriptionLabel.Text = "Welcome to the AIM installation wizard";

            // contentPanel
            this.contentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.contentPanel.Location = new Point(0, 80);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new Size(600, 320);
            this.contentPanel.TabIndex = 1;

            // buttonPanel
            this.buttonPanel.BorderStyle = BorderStyle.FixedSingle;
            this.buttonPanel.Controls.Add(this.progressBar);
            this.buttonPanel.Controls.Add(this.backButton);
            this.buttonPanel.Controls.Add(this.nextButton);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Dock = DockStyle.Bottom;
            this.buttonPanel.Location = new Point(0, 400);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new Size(600, 60);
            this.buttonPanel.TabIndex = 2;

            // progressBar
            this.progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.progressBar.Location = new Point(12, 10);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(576, 10);
            this.progressBar.TabIndex = 0;
            this.progressBar.Visible = false;

            // backButton
            this.backButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.backButton.Location = new Point(288, 25);
            this.backButton.Name = "backButton";
            this.backButton.Size = new Size(90, 25);
            this.backButton.TabIndex = 1;
            this.backButton.Text = "< Back";
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Click += BackButton_Click;

            // nextButton
            this.nextButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.nextButton.Location = new Point(384, 25);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new Size(90, 25);
            this.nextButton.TabIndex = 2;
            this.nextButton.Text = "Next >";
            this.nextButton.UseVisualStyleBackColor = true;
            this.nextButton.Click += NextButton_Click;

            // cancelButton
            this.cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.cancelButton.Location = new Point(498, 25);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(90, 25);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += CancelButton_Click;

            // InstallerForm
            this.ClientSize = new Size(600, 460);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.buttonPanel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InstallerForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AIM Installer";
            this.FormClosing += InstallerForm_FormClosing;
            this.ResumeLayout(false);

            InitializeWelcomePage();
            InitializeInstallPathPage();
            InitializeSharedSecurityPage();
            InitializeProgressPage();
        }

        private void InitializeWelcomePage()
        {
            // welcomeLabel
            this.welcomeLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.welcomeLabel.Location = new Point(30, 40);
            this.welcomeLabel.Name = "welcomeLabel";
            this.welcomeLabel.Size = new Size(540, 30);
            this.welcomeLabel.TabIndex = 0;
            this.welcomeLabel.Text = "Welcome to the AIM Installation Wizard";

            // welcomeMessageLabel
            this.welcomeMessageLabel.Location = new Point(30, 90);
            this.welcomeMessageLabel.Name = "welcomeMessageLabel";
            this.welcomeMessageLabel.Size = new Size(540, 180);
            this.welcomeMessageLabel.TabIndex = 1;
            this.welcomeMessageLabel.Text = 
                "This wizard will guide you through the installation of AIM (Asset Inventory Management).\n\n" +
                "AIM is a comprehensive Windows desktop application for managing, tracking, and auditing asset inventory.\n\n" +
                "Features:\n" +
                "• Enterprise-grade security with audit logging\n" +
                "• Modern theming support (Light, Dark, High Contrast)\n" +
                "• Directory operations and file management\n" +
                "• Advanced search capabilities\n" +
                "• Form generation and batch operations\n\n" +
                "Click 'Next' to continue.";
        }

        private void InitializeInstallPathPage()
        {
            // installPathLabel
            this.installPathLabel.AutoSize = true;
            this.installPathLabel.Location = new Point(30, 40);
            this.installPathLabel.Name = "installPathLabel";
            this.installPathLabel.Size = new Size(200, 15);
            this.installPathLabel.TabIndex = 0;
            this.installPathLabel.Text = "Select installation directory:";

            // installPathTextBox
            this.installPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.installPathTextBox.Location = new Point(30, 65);
            this.installPathTextBox.Name = "installPathTextBox";
            this.installPathTextBox.Size = new Size(450, 23);
            this.installPathTextBox.TabIndex = 1;
            this.installPathTextBox.Text = installPath;

            // browseButton
            this.browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.browseButton.Location = new Point(490, 64);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new Size(80, 25);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "Browse...";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += BrowseButton_Click;

            // desktopShortcutCheckBox
            this.desktopShortcutCheckBox.AutoSize = true;
            this.desktopShortcutCheckBox.Checked = true;
            this.desktopShortcutCheckBox.Location = new Point(30, 110);
            this.desktopShortcutCheckBox.Name = "desktopShortcutCheckBox";
            this.desktopShortcutCheckBox.Size = new Size(180, 19);
            this.desktopShortcutCheckBox.TabIndex = 3;
            this.desktopShortcutCheckBox.Text = "Create Desktop shortcut";
            this.desktopShortcutCheckBox.UseVisualStyleBackColor = true;

            // startMenuShortcutCheckBox
            this.startMenuShortcutCheckBox.AutoSize = true;
            this.startMenuShortcutCheckBox.Checked = true;
            this.startMenuShortcutCheckBox.Location = new Point(30, 140);
            this.startMenuShortcutCheckBox.Name = "startMenuShortcutCheckBox";
            this.startMenuShortcutCheckBox.Size = new Size(180, 19);
            this.startMenuShortcutCheckBox.TabIndex = 4;
            this.startMenuShortcutCheckBox.Text = "Create Start Menu shortcut";
            this.startMenuShortcutCheckBox.UseVisualStyleBackColor = true;
        }

        private void InitializeSharedSecurityPage()
        {
            // sharedSecurityLabel
            this.sharedSecurityLabel.Location = new Point(30, 40);
            this.sharedSecurityLabel.Name = "sharedSecurityLabel";
            this.sharedSecurityLabel.Size = new Size(540, 60);
            this.sharedSecurityLabel.TabIndex = 0;
            this.sharedSecurityLabel.Text = 
                "Shared Security Configuration (Optional)\n\n" +
                "You can optionally configure a shared security path for centralized authentication.\n" +
                "If you skip this step, AIM will use local security settings.";

            // enableSharedSecurityCheckBox
            this.enableSharedSecurityCheckBox.AutoSize = true;
            this.enableSharedSecurityCheckBox.Location = new Point(30, 110);
            this.enableSharedSecurityCheckBox.Name = "enableSharedSecurityCheckBox";
            this.enableSharedSecurityCheckBox.Size = new Size(250, 19);
            this.enableSharedSecurityCheckBox.TabIndex = 1;
            this.enableSharedSecurityCheckBox.Text = "Enable shared security configuration";
            this.enableSharedSecurityCheckBox.UseVisualStyleBackColor = true;
            this.enableSharedSecurityCheckBox.CheckedChanged += EnableSharedSecurityCheckBox_CheckedChanged;

            // sharedSecurityPathTextBox
            this.sharedSecurityPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.sharedSecurityPathTextBox.Enabled = false;
            this.sharedSecurityPathTextBox.Location = new Point(30, 140);
            this.sharedSecurityPathTextBox.Name = "sharedSecurityPathTextBox";
            this.sharedSecurityPathTextBox.Size = new Size(450, 23);
            this.sharedSecurityPathTextBox.TabIndex = 2;

            // browseSharedSecurityButton
            this.browseSharedSecurityButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.browseSharedSecurityButton.Enabled = false;
            this.browseSharedSecurityButton.Location = new Point(490, 139);
            this.browseSharedSecurityButton.Name = "browseSharedSecurityButton";
            this.browseSharedSecurityButton.Size = new Size(80, 25);
            this.browseSharedSecurityButton.TabIndex = 3;
            this.browseSharedSecurityButton.Text = "Browse...";
            this.browseSharedSecurityButton.UseVisualStyleBackColor = true;
            this.browseSharedSecurityButton.Click += BrowseSharedSecurityButton_Click;
        }

        private void InitializeProgressPage()
        {
            // progressLabel
            this.progressLabel.AutoSize = true;
            this.progressLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.progressLabel.Location = new Point(30, 30);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new Size(150, 19);
            this.progressLabel.TabIndex = 0;
            this.progressLabel.Text = "Installation Progress:";

            // logTextBox
            this.logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.logTextBox.BackColor = Color.Black;
            this.logTextBox.Font = new Font("Consolas", 9F);
            this.logTextBox.ForeColor = Color.Lime;
            this.logTextBox.Location = new Point(30, 60);
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.Size = new Size(540, 200);
            this.logTextBox.TabIndex = 1;
            this.logTextBox.Text = "";

            // launchAfterInstallCheckBox
            this.launchAfterInstallCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.launchAfterInstallCheckBox.AutoSize = true;
            this.launchAfterInstallCheckBox.Checked = true;
            this.launchAfterInstallCheckBox.Location = new Point(30, 275);
            this.launchAfterInstallCheckBox.Name = "launchAfterInstallCheckBox";
            this.launchAfterInstallCheckBox.Size = new Size(180, 19);
            this.launchAfterInstallCheckBox.TabIndex = 2;
            this.launchAfterInstallCheckBox.Text = "Launch AIM after installation";
            this.launchAfterInstallCheckBox.UseVisualStyleBackColor = true;
            this.launchAfterInstallCheckBox.Visible = false;
        }

        private void ShowStep(int step)
        {
            currentStep = step;
            contentPanel.Controls.Clear();

            switch (step)
            {
                case STEP_WELCOME:
                    titleLabel.Text = "Welcome";
                    descriptionLabel.Text = "Welcome to the AIM installation wizard";
                    contentPanel.Controls.Add(welcomeLabel);
                    contentPanel.Controls.Add(welcomeMessageLabel);
                    backButton.Enabled = false;
                    nextButton.Enabled = true;
                    nextButton.Text = "Next >";
                    break;

                case STEP_INSTALL_PATH:
                    titleLabel.Text = "Installation Directory";
                    descriptionLabel.Text = "Choose where to install AIM";
                    contentPanel.Controls.Add(installPathLabel);
                    contentPanel.Controls.Add(installPathTextBox);
                    contentPanel.Controls.Add(browseButton);
                    contentPanel.Controls.Add(desktopShortcutCheckBox);
                    contentPanel.Controls.Add(startMenuShortcutCheckBox);
                    backButton.Enabled = true;
                    nextButton.Enabled = true;
                    nextButton.Text = "Next >";
                    break;

                case STEP_SHARED_SECURITY:
                    titleLabel.Text = "Shared Security";
                    descriptionLabel.Text = "Configure optional shared security settings";
                    contentPanel.Controls.Add(sharedSecurityLabel);
                    contentPanel.Controls.Add(enableSharedSecurityCheckBox);
                    contentPanel.Controls.Add(sharedSecurityPathTextBox);
                    contentPanel.Controls.Add(browseSharedSecurityButton);
                    backButton.Enabled = true;
                    nextButton.Enabled = true;
                    nextButton.Text = "Install";
                    break;

                case STEP_PROGRESS:
                    titleLabel.Text = "Installing";
                    descriptionLabel.Text = "Please wait while AIM is being installed...";
                    contentPanel.Controls.Add(progressLabel);
                    contentPanel.Controls.Add(logTextBox);
                    contentPanel.Controls.Add(launchAfterInstallCheckBox);
                    backButton.Enabled = false;
                    nextButton.Enabled = false;
                    cancelButton.Enabled = false;
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    PerformInstallation();
                    break;

                case STEP_COMPLETE:
                    titleLabel.Text = "Installation Complete";
                    descriptionLabel.Text = "AIM has been successfully installed";
                    launchAfterInstallCheckBox.Visible = true;
                    backButton.Enabled = false;
                    nextButton.Enabled = true;
                    nextButton.Text = "Finish";
                    cancelButton.Enabled = false;
                    progressBar.Visible = false;
                    break;
            }
        }

        private void BackButton_Click(object? sender, EventArgs e)
        {
            if (currentStep > 0)
            {
                ShowStep(currentStep - 1);
            }
        }

        private void NextButton_Click(object? sender, EventArgs e)
        {
            if (currentStep == STEP_INSTALL_PATH)
            {
                installPath = installPathTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(installPath))
                {
                    MessageBox.Show("Please select an installation directory.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (currentStep == STEP_SHARED_SECURITY)
            {
                if (enableSharedSecurityCheckBox.Checked)
                {
                    sharedSecurityPath = sharedSecurityPathTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(sharedSecurityPath))
                    {
                        MessageBox.Show("Please select a shared security path or disable the option.", "Validation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    sharedSecurityPath = null;
                }
            }
            else if (currentStep == STEP_COMPLETE)
            {
                if (launchAfterInstallCheckBox.Checked)
                {
                    LaunchAIM();
                }
                this.Close();
                return;
            }

            if (currentStep < STEP_COMPLETE)
            {
                ShowStep(currentStep + 1);
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel the installation?", "Confirm Cancel",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void InstallerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!installationComplete && currentStep > STEP_WELCOME && currentStep < STEP_COMPLETE)
            {
                if (MessageBox.Show("Installation is not complete. Are you sure you want to exit?", "Confirm Exit",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        private void BrowseButton_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select installation directory";
                dialog.SelectedPath = installPathTextBox.Text;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    installPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseSharedSecurityButton_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select shared security directory";
                dialog.SelectedPath = sharedSecurityPathTextBox.Text;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    sharedSecurityPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void EnableSharedSecurityCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            sharedSecurityPathTextBox.Enabled = enableSharedSecurityCheckBox.Checked;
            browseSharedSecurityButton.Enabled = enableSharedSecurityCheckBox.Checked;
        }

        private void PerformInstallation()
        {
            // Run installation on background thread to avoid freezing UI
            var installTask = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    LogMessage("Starting AIM installation...");
                    LogMessage($"Installation directory: {installPath}");

                    // Create installation directory
                    LogMessage("Creating installation directory...");
                    Directory.CreateDirectory(installPath);

                    // Extract embedded ZIP file
                    LogMessage("Extracting application files...");
                    ExtractEmbeddedZip();

                    // Copy Deploy-AIM.ps1 script
                    LogMessage("Copying deployment script...");
                    CopyDeployScript();

                    // Write installer settings to user's LocalAppData
                    LogMessage("Writing installer settings...");
                    WriteInstallerSettings();
                    
                    // Create security-config.ini if shared security is configured
                    if (!string.IsNullOrWhiteSpace(sharedSecurityPath))
                    {
                        LogMessage("Creating security config file...");
                        CreateSecurityConfigIni();
                    }

                    // Create shortcuts
                    if (desktopShortcutCheckBox.Checked)
                    {
                        LogMessage("Creating desktop shortcut...");
                        CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AIM");
                    }

                    if (startMenuShortcutCheckBox.Checked)
                    {
                        LogMessage("Creating start menu shortcut...");
                        var startMenuPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Programs), "AIM");
                        Directory.CreateDirectory(startMenuPath);
                        CreateShortcut(startMenuPath, "AIM");
                    }

                    // Run Deploy-AIM.ps1 to configure paths and directories
                    LogMessage("Running deployment configuration (Deploy-AIM.ps1)...");
                    RunDeployScript();

                    LogMessage("Installation completed successfully!");
                    installationComplete = true;

                    // Move to completion step
                    this.Invoke(new Action(() => ShowStep(STEP_COMPLETE)));
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR: {ex.Message}");
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Installation failed: {ex.Message}", "Installation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                    }));
                }
            });
        }

        private void ExtractEmbeddedZip()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("AIM-Published.zip"));

            if (resourceName == null)
            {
                throw new InvalidOperationException("Embedded AIM application ZIP not found. " +
                    "The installer may have been built incorrectly.");
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not read embedded ZIP resource.");
                }

                var tempZipPath = Path.Combine(Path.GetTempPath(), "AIM-Published.zip");
                try
                {
                    using (var fileStream = File.Create(tempZipPath))
                    {
                        stream.CopyTo(fileStream);
                    }

                    LogMessage($"Extracting to: {installPath}");
                    ZipFile.ExtractToDirectory(tempZipPath, installPath, overwriteFiles: true);
                    LogMessage("Extraction completed.");
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
            }
        }

        private void CopyDeployScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("Deploy-AIM.ps1"));

            if (resourceName == null)
            {
                LogMessage("Warning: Deploy-AIM.ps1 script not found in installer resources.");
                return;
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    LogMessage("Warning: Could not read Deploy-AIM.ps1 resource.");
                    return;
                }

                var destPath = Path.Combine(installPath, "Deploy-AIM.ps1");
                using (var fileStream = File.Create(destPath))
                {
                    stream.CopyTo(fileStream);
                }
                LogMessage("Deploy-AIM.ps1 copied successfully.");
            }
        }

        private void CreateShortcut(string directory, string shortcutName)
        {
            try
            {
                var shell = Type.GetTypeFromProgID("WScript.Shell");
                if (shell == null) return;

                dynamic? wsh = Activator.CreateInstance(shell);
                if (wsh == null) return;

                var shortcutPath = Path.Combine(directory, $"{shortcutName}.lnk");
                dynamic? shortcut = wsh.CreateShortcut(shortcutPath);
                if (shortcut == null) return;

                var exePath = Path.Combine(installPath, "AIM.exe");
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = installPath;
                shortcut.Description = "AIM - Asset Inventory Management";
                shortcut.Save();

                LogMessage($"Shortcut created: {shortcutPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not create shortcut: {ex.Message}");
            }
        }

        private void RunDeployScript()
        {
            try
            {
                var scriptPath = Path.Combine(installPath, "Deploy-AIM.ps1");
                if (!File.Exists(scriptPath))
                {
                    LogMessage("Warning: Deploy-AIM.ps1 not found. Skipping deployment configuration.");
                    return;
                }

                // Build PowerShell arguments with network paths
                var arguments = new List<string>
                {
                    "-ExecutionPolicy", "Bypass",
                    "-File", $"\"{scriptPath}\"",
                    "-AIMInstallPath", $"\"{installPath}\"",
                    "-DefaultRootDirectory", $"\"{DEFAULT_ROOT_DIRECTORY}\"",
                    "-ArchivePath", $"\"{ARCHIVE_PATH}\"",
                    "-ShippedDirectory", $"\"{SHIPPED_DIRECTORY}\"",
                    "-FileScansDirectory", $"\"{FILE_SCANS_DIRECTORY}\"",
                    "-InventoryArchiveDirectory", $"\"{INVENTORY_ARCHIVE_DIRECTORY}\""
                };

                // Add shared security parameters if configured
                if (!string.IsNullOrWhiteSpace(sharedSecurityPath))
                {
                    // Use the embedded passphrase instead of prompting
                    string passphrase = DeobfuscatePassphrase(ObfuscatedPassphrase);
                    LogMessage("Using shared security configuration with embedded passphrase...");
                    
                    arguments.Add("-SharedSecurityPath");
                    arguments.Add($"\"{sharedSecurityPath}\"");
                    arguments.Add("-Passphrase");
                    arguments.Add($"\"{passphrase}\"");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = string.Join(" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        LogMessage("Warning: Could not start PowerShell process.");
                        return;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                        LogMessage(output);

                    if (!string.IsNullOrWhiteSpace(error))
                        LogMessage($"PowerShell Error: {error}");

                    if (process.ExitCode == 0)
                        LogMessage("Deployment configuration completed successfully.");
                    else
                        LogMessage($"Deployment configuration exited with code: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not run deployment script: {ex.Message}");
            }
        }

        private void LaunchAIM()
        {
            try
            {
                var exePath = Path.Combine(installPath, "AIM.exe");
                if (File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        WorkingDirectory = installPath
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not launch AIM: {ex.Message}", "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LogMessage(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            logTextBox.ScrollToCaret();
        }

        /// <summary>
        /// Obfuscates a passphrase using simple XOR with a key and Base64 encoding.
        /// WARNING: This is obfuscation, NOT encryption. It only prevents casual discovery.
        /// </summary>
        /// <param name="passphrase">The passphrase to obfuscate</param>
        /// <returns>The obfuscated passphrase as a Base64 string</returns>
        private string ObfuscatePassphrase(string passphrase)
        {
            // Simple XOR key - must match the one used in SecurityService
            byte[] xorKey = new byte[] { 0xA5, 0x3C, 0x7E, 0x91, 0x42, 0xF8, 0x6D, 0x2B };
            
            byte[] data = Encoding.UTF8.GetBytes(passphrase);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= xorKey[i % xorKey.Length];
            }
            
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// Deobfuscates a passphrase that was obfuscated with ObfuscatePassphrase.
        /// </summary>
        /// <param name="obfuscated">The obfuscated passphrase string</param>
        /// <returns>The original passphrase</returns>
        private string DeobfuscatePassphrase(string obfuscated)
        {
            if (string.IsNullOrEmpty(obfuscated))
                return string.Empty;

            try
            {
                // Simple XOR key - must match the one used in SecurityService
                byte[] xorKey = new byte[] { 0xA5, 0x3C, 0x7E, 0x91, 0x42, 0xF8, 0x6D, 0x2B };
                
                byte[] data = Convert.FromBase64String(obfuscated);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= xorKey[i % xorKey.Length];
                }
                
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not deobfuscate passphrase: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Writes the installer settings to the user's LocalAppData folder.
        /// This includes the shared security path and obfuscated passphrase.
        /// </summary>
        private void WriteInstallerSettings()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var aimConfigDir = Path.Combine(localAppData, "AIM");
                Directory.CreateDirectory(aimConfigDir);

                var settingsPath = Path.Combine(aimConfigDir, "settings.json");
                
                // Create complete AppSettings object with network paths
                var settings = new Dictionary<string, object>
                {
                    { "DefaultRootDirectory", DEFAULT_ROOT_DIRECTORY },
                    { "ArchivePath", ARCHIVE_PATH },
                    { "ShippedDirectory", SHIPPED_DIRECTORY },
                    { "FileScansDirectory", FILE_SCANS_DIRECTORY },
                    { "InventoryArchiveDirectory", INVENTORY_ARCHIVE_DIRECTORY },
                    { "SecurityConfigPath", "" },
                    { "Theme", "FollowSystem" },
                    { "Password", "" },
                    { "AuthorizedUsers", new string[] { } },
                    { "IsInitialPasswordSet", true },
                    { "UseSharedConfig", !string.IsNullOrWhiteSpace(sharedSecurityPath) },
                    { "SharedSecurityConfigPath", sharedSecurityPath ?? "" },
                    { "Passphrase", !string.IsNullOrWhiteSpace(sharedSecurityPath) ? ObfuscatedPassphrase : "" }
                };

                if (!string.IsNullOrWhiteSpace(sharedSecurityPath))
                {
                    LogMessage("Writing passphrase to settings (obfuscated)...");
                }

                // Write settings as JSON
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(settingsPath, json);
                
                LogMessage($"Settings written to: {settingsPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not write installer settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a security-config.ini file in the installation directory
        /// pointing to the shared security path.
        /// </summary>
        private void CreateSecurityConfigIni()
        {
            if (string.IsNullOrWhiteSpace(sharedSecurityPath))
                return;

            try
            {
                var configIniPath = Path.Combine(installPath, "security-config.ini");
                var content = $"# AIM Shared Security Configuration\r\n" +
                             $"# This file points to the centralized security configuration\r\n" +
                             $"SharedSecurityPath={sharedSecurityPath}\r\n";
                
                File.WriteAllText(configIniPath, content);
                LogMessage($"Created security-config.ini at: {configIniPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not create security-config.ini: {ex.Message}");
            }
        }
    }
}
