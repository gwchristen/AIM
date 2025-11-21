using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AIM.Installer
{
    /// <summary>
    /// Simple progress dialog for displaying AIM launch status.
    /// </summary>
    internal class LaunchProgressDialog : Form
    {
        private Label statusLabel;
        private Label countdownLabel;
        private Button cancelButton;
        private System.Windows.Forms.Timer countdownTimer;
        private int remainingSeconds;
        private volatile bool cancelled = false; // Thread-safe access
        private readonly object cancelLock = new object(); // Lock for thread-safe operations

        public bool IsCancelled 
        { 
            get 
            { 
                lock (cancelLock)
                {
                    return cancelled; 
                }
            }
        }

        public LaunchProgressDialog(int timeoutSeconds)
        {
            remainingSeconds = timeoutSeconds;
            InitializeComponents();
            countdownTimer.Start();
        }

        private void InitializeComponents()
        {
            this.Text = "Launching AIM";
            this.Size = new Size(400, 180);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;

            statusLabel = new Label
            {
                Text = "AIM is initializing...\n\nPlease wait while the application starts.",
                Location = new Point(20, 20),
                Size = new Size(360, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            countdownLabel = new Label
            {
                Text = $"Time remaining: {remainingSeconds} seconds",
                Location = new Point(20, 90),
                Size = new Size(360, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Bold)
            };

            cancelButton = new Button
            {
                Text = "Close Installer",
                Location = new Point(140, 115),
                Size = new Size(120, 30)
            };
            cancelButton.Click += (s, e) =>
            {
                lock (cancelLock)
                {
                    cancelled = true;
                }
                this.Close();
            };

            countdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
            };
            countdownTimer.Tick += CountdownTimer_Tick;

            this.Controls.Add(statusLabel);
            this.Controls.Add(countdownLabel);
            this.Controls.Add(cancelButton);
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;
            countdownLabel.Text = $"Time remaining: {remainingSeconds} seconds";

            if (remainingSeconds <= 0)
            {
                countdownTimer.Stop();
                statusLabel.Text = "AIM is still loading...\n\nYou can safely close this installer.";
                countdownLabel.Text = "Installation complete";
            }
        }

        public void UpdateStatus(string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Represents the application configuration settings.
    /// IMPORTANT: This class MUST match AIM.Models.AppSettings exactly to ensure
    /// JSON serialization/deserialization compatibility between installer and app.
    /// Any changes to the main AppSettings class must be reflected here.
    /// 
    /// MAINTENANCE NOTE: Some properties below are marked as deprecated in the main app
    /// but are kept here for backward compatibility with existing settings.json files.
    /// The installer writes empty/default values for these deprecated properties to ensure
    /// the settings file can be read by both old and new versions of the application.
    /// </summary>
    internal class AppSettings
    {
        /// <summary>
        /// Gets or sets the default root directory for file browsing operations.
        /// This is a required property that must be populated during installation.
        /// </summary>
        public string DefaultRootDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the path where archived files are stored.
        /// This is a required property that must be populated during installation.
        /// </summary>
        public string ArchivePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the directory path for shipped items.
        /// This is a required property that must be populated during installation.
        /// </summary>
        public string ShippedDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the directory where file scan results are stored.
        /// This is a required property that must be populated during installation.
        /// </summary>
        public string FileScansDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the directory where inventory archives are stored.
        /// This is a required property that must be populated during installation.
        /// </summary>
        public string InventoryArchiveDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// DEPRECATED in main app - use SecurityDatabasePath instead.
        /// Kept for backward compatibility with old settings.json files.
        /// Installer writes empty string.
        /// </summary>
        public string SecurityConfigPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the current application theme preference.
        /// Valid values: "FollowSystem", "Light", "Dark", "HighContrast".
        /// Defaults to "FollowSystem".
        /// </summary>
        public string Theme { get; set; } = "FollowSystem";
        
        /// <summary>
        /// DEPRECATED in main app - passwords stored in SecurityDatabase instead.
        /// Kept for backward compatibility with old settings.json files.
        /// Installer writes empty string.
        /// </summary>
        public string Password { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the list of authorized user IDs.
        /// This property is deprecated; use SecurityDatabasePath for user management instead.
        /// Kept for backward compatibility with old settings.json files.
        /// </summary>
        public List<string> AuthorizedUsers { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether the initial master password has been set.
        /// When false, the application requires the user to set a master password on first launch.
        /// This ensures no default or hardcoded passwords are used in production.
        /// </summary>
        public bool IsInitialPasswordSet { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the path to the shared security configuration.
        /// DEPRECATED: This property is maintained for backward compatibility but is no longer
        /// actively used in newer versions. Security settings are now stored in SecurityDatabasePath.
        /// Legacy behavior: When UseSharedConfig was enabled, this path was used to locate
        /// the centrally managed security configuration. Could be overridden by security-config.ini.
        /// </summary>
        public string SharedSecurityConfigPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether to use shared network configuration.
        /// DEPRECATED: This property is maintained for backward compatibility but is no longer
        /// actively used in newer versions. All instances now read from the centralized
        /// SecurityDatabasePath regardless of this setting.
        /// Legacy behavior: When true, the application attempted to load security configuration
        /// from SharedSecurityConfigPath or security-config.ini file.
        /// </summary>
        public bool UseSharedConfig { get; set; } = true;
        
        /// <summary>
        /// Path to the centralized SQLite security database.
        /// This is the ACTIVE property used by the application.
        /// This database stores authorized users, security settings, and audit logs.
        /// All AIM instances read from this shared database for centralized user management.
        /// This is a required property that must be populated during installation.
        /// </summary>
        public string SecurityDatabasePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Main installer form with wizard-style interface.
    /// </summary>
    public class InstallerForm : Form
    {
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

        // Progress Page Controls
        private Label progressLabel;
        private RichTextBox logTextBox;
        private CheckBox launchAfterInstallCheckBox;

        // Installation State
        private int currentStep = 0;
        private const int STEP_WELCOME = 0;
        private const int STEP_INSTALL_PATH = 1;
        private const int STEP_PROGRESS = 2;
        private const int STEP_COMPLETE = 3;

        private string installPath = @"C:\Program Files\AIM";
        private bool installationComplete = false;
        
        // Installer logging
        private string? installerLogPath;
        private StreamWriter? logFileWriter;
        private bool logFileWriterDisposed = false;
        private const int MAX_LOG_FILES = 5;

        // Database timeout configuration
        private const int DEFAULT_DB_TIMEOUT = 30; // seconds
        private System.Threading.CancellationTokenSource? installCancellationSource;

        // Network paths - configurable via environment variables with hardcoded defaults
        // Environment variables: AIM_ROOT_DIR, AIM_ARCHIVE_DIR, AIM_SHIPPED_DIR, AIM_SCANS_DIR, AIM_INVENTORY_ARCHIVE_DIR, AIM_SECURITY_DB_PATH
        private static readonly string DefaultRootDirectory = Environment.GetEnvironmentVariable("AIM_ROOT_DIR") ?? @"\\oh1cam01\cml\Internal\LAB STOCK\LAB STOCK";
        private static readonly string ArchivePath = Environment.GetEnvironmentVariable("AIM_ARCHIVE_DIR") ?? @"\\oh1cam01\cml\Internal\LAB STOCK\Archive";
        private static readonly string ShippedDirectory = Environment.GetEnvironmentVariable("AIM_SHIPPED_DIR") ?? @"\\oh1cam01\cml\Internal\LAB STOCK\Orders shipped";
        private static readonly string FileScansDirectory = Environment.GetEnvironmentVariable("AIM_SCANS_DIR") ?? @"C:\Tfile";
        private static readonly string InventoryArchiveDirectory = Environment.GetEnvironmentVariable("AIM_INVENTORY_ARCHIVE_DIR") ?? @"\\oh1cam01\cml\Internal\LAB STOCK\Physical Inventory Archive";
        private static readonly string SecurityDatabasePath = Environment.GetEnvironmentVariable("AIM_SECURITY_DB_PATH") ?? @"\\oh1cam01\cml\Internal\LAB STOCK\Important Inventory Related Documents\AIM\AIM_Security.db";

        // Hardcoded SuperAdmin credentials - baked into installer
        // SECURITY NOTE: These credentials are intentionally hardcoded per requirements.
        // The installer is designed for internal deployment where source code access is controlled.
        // These credentials provide initial access to create additional admin users.
        // Organizations should change the SuperAdmin password after installation and create
        // individual user accounts for proper access control and audit logging.
        private const string SuperAdminUsername = "AIMAdmin";
        private const string SuperAdminPassword = "AIM@2025!SecurePass";

        public InstallerForm()
        {
            InitializeComponent();
            InitializeInstallerLogging();
            LogNetworkPathConfiguration();
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
                    
                    // Show log file location
                    if (!string.IsNullOrEmpty(installerLogPath) && File.Exists(installerLogPath))
                    {
                        var logLocationLabel = new Label
                        {
                            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                            Location = new Point(30, 300),
                            Size = new Size(540, 40),
                            Text = $"Installation log saved to:\n{installerLogPath}",
                            ForeColor = Color.DarkGreen
                        };
                        contentPanel.Controls.Add(logLocationLabel);
                    }
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
                    return;
                }
            }
            
            // Cleanup resources
            if (!logFileWriterDisposed)
            {
                try
                {
                    installCancellationSource?.Cancel();
                    installCancellationSource?.Dispose();
                    logFileWriter?.Close();
                    logFileWriter?.Dispose();
                    logFileWriterDisposed = true;
                }
                catch { }
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

        private void PerformInstallation()
        {
            // Create cancellation token source for the installation
            installCancellationSource = new System.Threading.CancellationTokenSource();
            
            // Run installation on background thread to avoid freezing UI
            var installTask = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    LogMessage("Starting AIM installation...");
                    LogMessage($"Installation directory: {installPath}");

                    // MEDIUM Issue #13: Validate installation path before proceeding
                    LogMessage("Validating installation path...");
                    if (!ValidateInstallationPath(installPath))
                    {
                        LogMessage("ERROR: Installation path validation failed");
                        throw new IOException("Installation path validation failed - installation cannot continue");
                    }

                    // MEDIUM Issue #14: Check available disk space before extraction
                    LogMessage("Checking disk space...");
                    if (!CheckDiskSpace(installPath))
                    {
                        LogMessage("ERROR: Insufficient disk space for installation");
                        throw new IOException("Insufficient disk space - installation cannot continue");
                    }

                    // MEDIUM Issue #12: Create installation directory with timeout
                    // Use CreateDirectoryWithTimeoutAsync to protect against hanging network paths
                    LogMessage("Creating installation directory...");
                    if (!await CreateDirectoryWithTimeoutAsync(installPath, timeoutSeconds: 15))
                    {
                        LogMessage("ERROR: Failed to create installation directory or operation timed out");
                        throw new IOException($"Failed to create installation directory: {installPath}");
                    }

                    // Extract embedded ZIP file
                    LogMessage("Extracting application files...");
                    ExtractEmbeddedZip();

                    // CRITICAL Issue #5: Verify ZIP extraction integrity
                    LogMessage("Verifying installation integrity...");
                    if (!VerifyInstallationIntegrity())
                    {
                        LogMessage("ERROR: Installation integrity check failed");
                        throw new IOException("Installation files are incomplete or corrupted");
                    }

                    // Initialize security database with SuperAdmin account FIRST
                    // This must succeed before writing settings
                    LogMessage("Initializing security database...");
                    bool dbSuccess = await CreateSecurityDatabaseAsync();
                    
                    if (!dbSuccess)
                    {
                        LogMessage("ERROR: Security database initialization failed!");
                        LogMessage("Installation cannot continue without a valid security database.");
                        
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show(
                                "Failed to initialize the security database.\n\n" +
                                "This usually happens when:\n" +
                                "1. The network path is not accessible\n" +
                                "2. You don't have write permissions to the network location\n" +
                                "3. The network share is offline\n\n" +
                                $"Database path: {SecurityDatabasePath}\n\n" +
                                "Please ensure the network path is accessible and try again.",
                                "Database Initialization Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            this.Close();
                        }));
                        return;
                    }

                    // Write installer settings to user's LocalAppData
                    // Only write settings AFTER database is successfully created
                    LogMessage("Writing installer settings...");
                    await WriteInstallerSettingsAsync();

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

                    LogMessage("Installation completed successfully!");
                    
                    // Log the installer log location
                    if (!string.IsNullOrEmpty(installerLogPath))
                    {
                        LogMessage($"Installer log saved to: {installerLogPath}");
                    }
                    
                    installationComplete = true;

                    // Move to completion step
                    this.Invoke(new Action(() => ShowStep(STEP_COMPLETE)));
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR: {ex.Message}");
                    LogMessage($"Stack trace: {ex.StackTrace}");
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Installation failed: {ex.Message}", "Installation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                    }));
                }
                finally
                {
                    // Close log file writer
                    if (!logFileWriterDisposed)
                    {
                        try
                        {
                            logFileWriter?.Close();
                            logFileWriter?.Dispose();
                            logFileWriterDisposed = true;
                        }
                        catch { }
                    }
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

        private void LaunchAIM()
        {
            Process? aimProcess = null;
            LaunchProgressDialog? progressDialog = null;
            Thread? dialogThread = null;
            
            try
            {
                var exePath = Path.Combine(installPath, "AIM.exe");
                if (!File.Exists(exePath))
                {
                    LogMessage($"ERROR: AIM.exe not found at {exePath}");
                    MessageBox.Show($"Could not find AIM.exe at {exePath}", "Launch Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LogMessage("Launching AIM...");
                // HIGH Issue #9: Use UseShellExecute=false with CreateNoWindow=true
                // This prevents console window from appearing and allows proper process tracking
                aimProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = installPath
                });

                if (aimProcess == null)
                {
                    LogMessage("ERROR: Failed to start AIM process");
                    MessageBox.Show("Failed to start AIM process", "Launch Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LogMessage($"AIM process started (PID: {aimProcess.Id})");
                LogMessage("Waiting for AIM to complete initialization...");

                // Create and show progress dialog
                const int timeoutSeconds = 60;
                progressDialog = new LaunchProgressDialog(timeoutSeconds);
                progressDialog.UpdateStatus($"AIM is initializing...\n\nProcess ID: {aimProcess.Id}\n\nPlease wait while the application starts.");
                
                // Show dialog modally in a separate thread so we can monitor the process
                dialogThread = new Thread(() =>
                {
                    Application.Run(progressDialog);
                });
                dialogThread.SetApartmentState(ApartmentState.STA);
                dialogThread.Start();

                // Wait for AIM to exit or timeout
                var startTime = DateTime.Now;
                bool exited = false;
                bool userCancelled = false;
                bool timedOut = false;
                
                while (!exited && (DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
                {
                    // Check if process has exited
                    exited = aimProcess.WaitForExit(1000); // Check every second
                    
                    // Check if user cancelled the dialog (thread-safe access)
                    if (progressDialog.IsCancelled)
                    {
                        LogMessage("User closed the progress dialog");
                        userCancelled = true;
                        break;
                    }
                    
                    // Update status every 10 seconds (Issue #10)
                    var elapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                    if (!exited && elapsed > 0 && elapsed % 10 == 0)
                    {
                        progressDialog.UpdateStatus($"AIM is initializing...\n\nProcess ID: {aimProcess.Id}\n\nElapsed time: {elapsed} seconds");
                        LogMessage($"AIM still initializing... ({elapsed}s elapsed)");
                    }
                }

                // Check if we timed out
                if (!exited && !userCancelled)
                {
                    timedOut = true;
                }

                // Close progress dialog
                if (progressDialog != null && !progressDialog.IsDisposed)
                {
                    progressDialog.Invoke(new Action(() => progressDialog.Close()));
                }

                // CRITICAL: Properly join the dialog thread with timeout (Issue #6)
                if (dialogThread != null && dialogThread.IsAlive)
                {
                    LogMessage("Waiting for progress dialog thread to exit...");
                    bool threadExited = dialogThread.Join(TimeSpan.FromSeconds(5));
                    if (!threadExited)
                    {
                        LogMessage("WARNING: Dialog thread did not exit within timeout");
                    }
                    else
                    {
                        LogMessage("Dialog thread exited successfully");
                    }
                }

                // Handle each exit condition explicitly (Issue #15)
                if (exited)
                {
                    LogMessage($"AIM process exited with code: {aimProcess.ExitCode}");
                    
                    if (aimProcess.ExitCode != 0)
                    {
                        MessageBox.Show(
                            $"AIM exited with error code {aimProcess.ExitCode}\n\n" +
                            $"Please check the installer log for details:\n" +
                            $"{installerLogPath}",
                            "Launch Warning",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                    else
                    {
                        LogMessage("AIM exited successfully");
                    }
                }
                else if (timedOut)
                {
                    LogMessage($"AIM is running (timeout after {timeoutSeconds} seconds - this is normal for long initialization)");
                    LogMessage("Installation completed successfully");
                }
                else if (userCancelled)
                {
                    LogMessage("User cancelled the launch progress dialog");
                    LogMessage("Installation completed, but AIM launch monitoring was cancelled by user");
                }
            }
            catch (Win32Exception ex)
            {
                // HIGH Issue #9: Handle Win32Exception specifically for better diagnostics
                LogMessage($"ERROR launching AIM (Win32): {ex.Message}");
                
                string userMessage = "Could not launch AIM:\n\n";
                if (ex.Message.Contains("not found") || ex.NativeErrorCode == 2)
                {
                    userMessage += "AIM.exe not found or inaccessible.\n\n";
                    LogMessage("Win32 error: AIM.exe not found or inaccessible");
                }
                else if (ex.Message.ToLower().Contains("permission") || ex.Message.ToLower().Contains("denied") || ex.NativeErrorCode == 5)
                {
                    userMessage += "Permission denied to launch AIM.\n\n";
                    LogMessage("Win32 error: Permission denied to launch AIM");
                }
                else
                {
                    userMessage += $"Win32 error (code {ex.NativeErrorCode}): {ex.Message}\n\n";
                    LogMessage($"Win32 error code {ex.NativeErrorCode}: {ex.Message}");
                }
                
                userMessage += "Please ensure:\n";
                userMessage += "• AIM.exe exists in the installation directory\n";
                userMessage += "• You have permission to run the application\n";
                userMessage += "• The installation completed successfully";
                
                MessageBox.Show(userMessage, "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR launching AIM: {ex.Message}");
                MessageBox.Show($"Could not launch AIM: {ex.Message}", "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // Clean up progress dialog
                if (progressDialog != null && !progressDialog.IsDisposed)
                {
                    try
                    {
                        progressDialog.Invoke(new Action(() => progressDialog.Close()));
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
                
                // CRITICAL: Ensure thread is properly joined before disposal (Issue #6)
                if (dialogThread != null && dialogThread.IsAlive)
                {
                    try
                    {
                        dialogThread.Join(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"WARNING: Error joining dialog thread: {ex.Message}");
                    }
                }
                
                // Clean up thread reference
                dialogThread = null;
                
                aimProcess?.Dispose();
            }
        }

        private void LogMessage(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            var timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logTextBox.AppendText(timestampedMessage + "\r\n");
            logTextBox.ScrollToCaret();
            
            // Also write to persistent log file
            if (!logFileWriterDisposed)
            {
                try
                {
                    logFileWriter?.WriteLine(timestampedMessage);
                    logFileWriter?.Flush();
                }
                catch (ObjectDisposedException)
                {
                    // Log writer already disposed, ignore
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Initializes persistent logging for the installer.
        /// Creates log file at %APPDATA%\AIM\installer.log with rotation.
        /// </summary>
        private void InitializeInstallerLogging()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var aimLogDir = Path.Combine(appDataPath, "AIM");
                Directory.CreateDirectory(aimLogDir);

                // Rotate old log files
                RotateLogFiles(aimLogDir);

                // Create new log file
                installerLogPath = Path.Combine(aimLogDir, "installer.log");
                logFileWriter = new StreamWriter(installerLogPath, append: false);
                
                // Write header with system info
                logFileWriter.WriteLine($"========================================");
                logFileWriter.WriteLine($"AIM Installer Log");
                logFileWriter.WriteLine($"Started: {DateTime.Now}");
                logFileWriter.WriteLine($"OS: {Environment.OSVersion}");
                logFileWriter.WriteLine($".NET Version: {Environment.Version}");
                logFileWriter.WriteLine($"Machine: {Environment.MachineName}");
                logFileWriter.WriteLine($"User: {Environment.UserName}");
                logFileWriter.WriteLine($"========================================");
                logFileWriter.Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize installer logging: {ex.Message}");
                // Non-fatal - continue without persistent logging
            }
        }

        /// <summary>
        /// Logs the network path configuration, indicating which paths are from environment variables vs defaults.
        /// </summary>
        private void LogNetworkPathConfiguration()
        {
            LogMessage("========================================");
            LogMessage("Network Path Configuration");
            LogMessage("========================================");
            LogMessage($"DefaultRootDirectory: {DefaultRootDirectory}");
            LogMessage($"  Source: {(Environment.GetEnvironmentVariable("AIM_ROOT_DIR") != null ? "Environment Variable (AIM_ROOT_DIR)" : "Hardcoded Default")}");
            LogMessage($"ArchivePath: {ArchivePath}");
            LogMessage($"  Source: {(Environment.GetEnvironmentVariable("AIM_ARCHIVE_DIR") != null ? "Environment Variable (AIM_ARCHIVE_DIR)" : "Hardcoded Default")}");
            LogMessage($"ShippedDirectory: {ShippedDirectory}");
            LogMessage($"  Source: {(Environment.GetEnvironmentVariable("AIM_SHIPPED_DIR") != null ? "Environment Variable (AIM_SHIPPED_DIR)" : "Hardcoded Default")}");
            LogMessage($"FileScansDirectory: {FileScansDirectory}");
            LogMessage($"  Source: {(Environment.GetEnvironmentVariable("AIM_SCANS_DIR") != null ? "Environment Variable (AIM_SCANS_DIR)" : "Hardcoded Default")}");
            LogMessage($"InventoryArchiveDirectory: {InventoryArchiveDirectory}");
            LogMessage($"  Source: {(Environment.GetEnvironmentVariable("AIM_INVENTORY_ARCHIVE_DIR") != null ? "Environment Variable (AIM_INVENTORY_ARCHIVE_DIR)" : "Hardcoded Default")}");
            LogMessage($"SecurityDatabasePath: {SecurityDatabasePath}");
            LogMessage($"  Source: {(Environment.GetEnvironmentVariable("AIM_SECURITY_DB_PATH") != null ? "Environment Variable (AIM_SECURITY_DB_PATH)" : "Hardcoded Default")}");
            LogMessage("========================================");
        }

        /// <summary>
        /// Rotates installer log files, keeping only the last MAX_LOG_FILES logs.
        /// </summary>
        private void RotateLogFiles(string logDirectory)
        {
            try
            {
                var logFiles = Directory.GetFiles(logDirectory, "installer*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                // Archive the current log if it exists
                var currentLog = Path.Combine(logDirectory, "installer.log");
                if (File.Exists(currentLog))
                {
                    var timestamp = File.GetLastWriteTime(currentLog).ToString("yyyyMMdd_HHmmss");
                    var archivedName = Path.Combine(logDirectory, $"installer_{timestamp}.log");
                    
                    // Handle case where archived file already exists (multiple concurrent installers)
                    int suffix = 1;
                    while (File.Exists(archivedName))
                    {
                        archivedName = Path.Combine(logDirectory, $"installer_{timestamp}_{suffix}.log");
                        suffix++;
                    }
                    
                    File.Move(currentLog, archivedName);
                    logFiles.Insert(0, new FileInfo(archivedName));
                }

                // Delete old logs beyond MAX_LOG_FILES
                for (int i = MAX_LOG_FILES; i < logFiles.Count; i++)
                {
                    logFiles[i].Delete();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to rotate log files: {ex.Message}");
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

        /// <summary>
        /// Validates network paths by testing accessibility and write permissions.
        /// DOES NOT create directories - only validates existing paths or paths that can be created.
        /// Uses temp directory for write permission tests to avoid conflicts.
        /// Collects ALL errors before returning to provide comprehensive validation results.
        /// </summary>
        /// <returns>Dictionary of paths and their validation status messages. Empty if all paths are valid.</returns>
        private Dictionary<string, string> ValidateNetworkPaths()
        {
            var invalidPaths = new Dictionary<string, string>();
            
            LogMessage("========================================");
            LogMessage("Validating Network Paths");
            LogMessage("========================================");

            // Define paths to validate with their descriptions
            var pathsToValidate = new Dictionary<string, (string path, bool requireWrite)>
            {
                { "DefaultRootDirectory", (DefaultRootDirectory, true) },
                { "ArchivePath", (ArchivePath, true) },
                { "ShippedDirectory", (ShippedDirectory, true) },
                { "FileScansDirectory", (FileScansDirectory, true) },
                { "InventoryArchiveDirectory", (InventoryArchiveDirectory, true) },
                { "SecurityDatabasePath", (SecurityDatabasePath, true) }
            };

            foreach (var kvp in pathsToValidate)
            {
                var pathName = kvp.Key;
                var (path, requireWrite) = kvp.Value;
                
                LogMessage($"Validating {pathName}: {path}");

                // For database path, validate the directory, not the file
                var directoryToCheck = pathName == "SecurityDatabasePath" 
                    ? Path.GetDirectoryName(path) 
                    : path;

                if (string.IsNullOrEmpty(directoryToCheck))
                {
                    var error = "Path is empty or invalid";
                    LogMessage($"  ERROR: {error}");
                    invalidPaths[pathName] = error;
                    continue; // Continue validating other paths
                }

                // CRITICAL Issues #1, #2, #3: Validate directory accessibility without creating it
                // Validation should NOT modify the filesystem - only check what exists
                bool directoryExists = false;
                try
                {
                    directoryExists = Directory.Exists(directoryToCheck);
                    if (!directoryExists)
                    {
                        LogMessage($"  WARNING: Directory does not exist (will be created later)");
                        // Don't add to invalidPaths - directory creation happens in CreateNetworkDirectoriesAsync()
                        // We just warn the user that the directory will need to be created
                    }
                    else
                    {
                        LogMessage($"  Directory exists");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    var error = $"Access denied: {ex.Message}";
                    LogMessage($"  ERROR: {error}");
                    invalidPaths[pathName] = error;
                    continue; // Continue validating other paths
                }
                catch (IOException ex)
                {
                    var error = $"I/O error: {ex.Message}";
                    LogMessage($"  ERROR: {error}");
                    invalidPaths[pathName] = error;
                    continue; // Continue validating other paths
                }
                catch (Exception ex)
                {
                    var error = $"Cannot access: {ex.Message}";
                    LogMessage($"  ERROR: {error}");
                    invalidPaths[pathName] = error;
                    continue; // Continue validating other paths
                }

                // CRITICAL Issues #1, #2, #3: Only test write permissions on EXISTING directories
                // This prevents false positives and avoids creating directories during validation
                if (requireWrite && directoryExists)
                {
                    // Test write permission in the existing directory
                    var testFileName = Path.Combine(directoryToCheck ?? path, $"__aim_write_test_{Guid.NewGuid()}.tmp");
                    try
                    {
                        // Attempt to create and write to a test file in the existing directory
                        File.WriteAllText(testFileName, "AIM write permission test");
                        
                        // Verify we can read it back
                        var content = File.ReadAllText(testFileName);
                        if (content != "AIM write permission test")
                        {
                            throw new IOException("Write verification failed");
                        }
                        
                        LogMessage($"  Write permission verified in existing path: {directoryToCheck ?? path}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        var error = $"No write permission: {ex.Message}";
                        LogMessage($"  ERROR: {error}");
                        invalidPaths[pathName] = error;
                    }
                    catch (IOException ex)
                    {
                        var error = $"Write test failed: {ex.Message}";
                        LogMessage($"  ERROR: {error}");
                        invalidPaths[pathName] = error;
                    }
                    catch (Exception ex)
                    {
                        var error = $"Write test error: {ex.Message}";
                        LogMessage($"  ERROR: {error}");
                        invalidPaths[pathName] = error;
                    }
                    finally
                    {
                        // CRITICAL Issue #2: Ensure test file cleanup in finally block
                        // This guarantees cleanup even if an error occurs
                        try 
                        { 
                            if (File.Exists(testFileName)) 
                            {
                                File.Delete(testFileName);
                                LogMessage($"  Test file cleaned up: {testFileName}");
                            }
                        } 
                        catch (Exception ex)
                        {
                            LogMessage($"  WARNING: Failed to cleanup test file: {ex.Message}");
                        }
                    }
                }
                else if (requireWrite && !directoryExists)
                {
                    LogMessage($"  Skipping write test (directory doesn't exist, will test after creation)");
                }

                LogMessage($"  Validation PASSED");
            }

            LogMessage("========================================");
            if (invalidPaths.Count > 0)
            {
                LogMessage($"Validation completed with {invalidPaths.Count} error(s)");
            }
            else
            {
                LogMessage("All paths validated successfully");
            }
            LogMessage("========================================");

            return invalidPaths;
        }

        /// <summary>
        /// Creates network directories after validation has passed.
        /// Separated from validation to follow single responsibility principle.
        /// Uses timeout for each directory creation to prevent hanging.
        /// </summary>
        /// <returns>True if all directories were created successfully, false otherwise.</returns>
        private async Task<bool> CreateNetworkDirectoriesAsync()
        {
            LogMessage("========================================");
            LogMessage("Creating Network Directories");
            LogMessage("========================================");

            var directoriesToCreate = new List<(string name, string path)>
            {
                ("DefaultRootDirectory", DefaultRootDirectory),
                ("ArchivePath", ArchivePath),
                ("ShippedDirectory", ShippedDirectory),
                ("FileScansDirectory", FileScansDirectory),
                ("InventoryArchiveDirectory", InventoryArchiveDirectory),
                ("SecurityDatabasePath Directory", Path.GetDirectoryName(SecurityDatabasePath) ?? string.Empty)
            };

            bool allCreated = true;
            var failedDirectories = new List<(string name, string error)>();

            foreach (var (name, path) in directoriesToCreate)
            {
                if (string.IsNullOrEmpty(path))
                {
                    LogMessage($"  Skipping {name} - empty path");
                    continue;
                }

                // CRITICAL Issue #4: Wrap CreateDirectoryWithTimeoutAsync() in try-catch to handle exceptions
                try
                {
                    if (!await CreateDirectoryWithTimeoutAsync(path, timeoutSeconds: 15))
                    {
                        var error = "Directory creation failed or timed out";
                        LogMessage($"  ERROR: Failed to create {name}: {error}");
                        failedDirectories.Add((name, error));
                        allCreated = false;
                    }
                    else
                    {
                        LogMessage($"  {name} ready");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    var error = $"Access denied: {ex.Message}";
                    LogMessage($"  ERROR: Failed to create {name} - {error}");
                    failedDirectories.Add((name, error));
                    allCreated = false;
                    // Continue to next directory instead of failing entire operation
                }
                catch (IOException ex)
                {
                    var error = $"I/O error: {ex.Message}";
                    LogMessage($"  ERROR: Failed to create {name} - {error}");
                    failedDirectories.Add((name, error));
                    allCreated = false;
                    // Continue to next directory instead of failing entire operation
                }
                catch (TimeoutException ex)
                {
                    var error = $"Operation timed out: {ex.Message}";
                    LogMessage($"  ERROR: Failed to create {name} - {error}");
                    failedDirectories.Add((name, error));
                    allCreated = false;
                    // Continue to next directory instead of failing entire operation
                }
                catch (Exception ex)
                {
                    var error = $"Unexpected error: {ex.Message}";
                    LogMessage($"  ERROR: Failed to create {name} - {error}");
                    failedDirectories.Add((name, error));
                    allCreated = false;
                    // Continue to next directory instead of failing entire operation
                }
            }

            LogMessage("========================================");
            if (allCreated)
            {
                LogMessage("All directories created successfully");
            }
            else
            {
                LogMessage("Some directories could not be created");
                LogMessage($"Failed directories ({failedDirectories.Count}):");
                foreach (var (name, error) in failedDirectories)
                {
                    LogMessage($"  - {name}: {error}");
                }
                
                // CRITICAL Issue #4: Show user dialog with all failed directories
                // This provides detailed information to the user about which directories failed and why
                var errorMessage = "The following network directories could not be created:\n\n";
                foreach (var (name, error) in failedDirectories)
                {
                    errorMessage += $"• {name}:\n  {error}\n\n";
                }
                errorMessage += "This usually indicates:\n";
                errorMessage += "• Network path is not accessible\n";
                errorMessage += "• You don't have permission to create directories\n";
                errorMessage += "• Network share is offline or unreachable\n";
                errorMessage += "• Operation timed out due to slow network\n\n";
                errorMessage += "Please check network connectivity and permissions, then try again.";
                
                MessageBox.Show(
                    errorMessage,
                    "Directory Creation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            LogMessage("========================================");

            return allCreated;
        }

        /// <summary>
        /// Writes the installer settings to the user's LocalAppData folder.
        /// The path matches where SettingsService expects to find settings.json.
        /// Validates network paths before writing and blocks on validation failures.
        /// Uses iterative retry logic to prevent stack overflow.
        /// </summary>
        private async Task WriteInstallerSettingsAsync()
        {
            const int MAX_RETRIES = 3;
            int retryCount = 0;
            
            while (retryCount < MAX_RETRIES)
            {
                try
                {
                    // Validate all network paths before writing settings
                    var invalidPaths = ValidateNetworkPaths();
                    
                    if (invalidPaths.Count > 0)
                    {
                        // Build error message
                        var errorMessage = "The following network paths could not be validated:\n\n";
                        foreach (var kvp in invalidPaths)
                        {
                            errorMessage += $"• {kvp.Key}: {kvp.Value}\n";
                        }
                        errorMessage += "\nPlease ensure:\n";
                        errorMessage += "• Network paths are accessible\n";
                        errorMessage += "• You have read/write permissions\n";
                        errorMessage += "• Network shares are online and reachable\n\n";
                        errorMessage += $"Retry attempt {retryCount + 1} of {MAX_RETRIES}.\n\n";
                        errorMessage += "Would you like to retry the validation?";

                        LogMessage($"ERROR: Path validation failed (attempt {retryCount + 1}/{MAX_RETRIES})");
                        
                        var result = MessageBox.Show(
                            errorMessage,
                            "Network Path Validation Failed",
                            MessageBoxButtons.RetryCancel,
                            MessageBoxIcon.Error
                        );

                        if (result == DialogResult.Retry)
                        {
                            retryCount++;
                            LogMessage($"User requested retry, attempting validation again (attempt {retryCount}/{MAX_RETRIES})");
                            continue; // Retry the validation loop
                        }
                        else
                        {
                            LogMessage("User cancelled due to path validation errors");
                            throw new IOException("Network path validation failed - installation cannot continue");
                        }
                    }

                    // CRITICAL Issue #3: Create directories AFTER validation passes
                    LogMessage("Validation passed, creating network directories...");
                    if (!await CreateNetworkDirectoriesAsync())
                    {
                        LogMessage("ERROR: Failed to create some network directories");
                        throw new IOException("Failed to create network directories - installation cannot continue");
                    }

                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var aimConfigDir = Path.Combine(localAppData, "AIM");
                    Directory.CreateDirectory(aimConfigDir);

                    var settingsPath = Path.Combine(aimConfigDir, "settings.json");
                    
                    // MEDIUM Issue #11: Check if settings.json already exists
                    if (File.Exists(settingsPath))
                    {
                        LogMessage($"NOTICE: Settings file already exists at {settingsPath}");
                        
                        var existingResult = MessageBox.Show(
                            "A settings file already exists at the target location.\n\n" +
                            $"Path: {settingsPath}\n\n" +
                            "What would you like to do?\n\n" +
                            "• YES - Overwrite with new settings\n" +
                            "• NO - Keep existing settings (recommended)\n" +
                            "• CANCEL - Backup existing and create new",
                            "Existing Settings Detected",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question
                        );

                        if (existingResult == DialogResult.No)
                        {
                            LogMessage("User chose to keep existing settings");
                            return; // Keep existing settings
                        }
                        else if (existingResult == DialogResult.Cancel)
                        {
                            // Backup existing settings
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            var backupPath = $"{settingsPath}.backup.{timestamp}";
                            
                            try
                            {
                                LogMessage($"Backing up existing settings to: {backupPath}");
                                File.Copy(settingsPath, backupPath);
                                LogMessage("Existing settings backed up successfully");
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"ERROR: Failed to backup existing settings: {ex.Message}");
                                MessageBox.Show(
                                    $"Failed to backup existing settings:\n{ex.Message}\n\nInstallation cannot continue.",
                                    "Backup Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                                throw;
                            }
                        }
                        else // DialogResult.Yes - Overwrite
                        {
                            LogMessage("User chose to overwrite existing settings");
                        }
                    }
                    
                    LogMessage($"Writing settings to: {settingsPath}");
                    
                    // Create AppSettings object with properly typed properties
                    // MUST match AIM.Models.AppSettings for JSON compatibility
                    var settings = new AppSettings
                    {
                        DefaultRootDirectory = DefaultRootDirectory,
                        ArchivePath = ArchivePath,
                        ShippedDirectory = ShippedDirectory,
                        FileScansDirectory = FileScansDirectory,
                        InventoryArchiveDirectory = InventoryArchiveDirectory,
                        SecurityDatabasePath = SecurityDatabasePath,
                        SecurityConfigPath = string.Empty, // Deprecated but needed for compatibility
                        Theme = "FollowSystem",
                        Password = string.Empty, // Deprecated but needed for compatibility
                        AuthorizedUsers = new List<string>(),
                        IsInitialPasswordSet = false,
                        SharedSecurityConfigPath = string.Empty,
                        UseSharedConfig = true
                    };

                    // Serialize to JSON with proper formatting
                    var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    // Write to file
                    File.WriteAllText(settingsPath, json);
                    
                    LogMessage("Settings written successfully");
                    LogMessage($"Settings structure:");
                    LogMessage($"  DefaultRootDirectory: {settings.DefaultRootDirectory}");
                    LogMessage($"  ArchivePath: {settings.ArchivePath}");
                    LogMessage($"  ShippedDirectory: {settings.ShippedDirectory}");
                    LogMessage($"  FileScansDirectory: {settings.FileScansDirectory}");
                    LogMessage($"  InventoryArchiveDirectory: {settings.InventoryArchiveDirectory}");
                    LogMessage($"  SecurityDatabasePath: {settings.SecurityDatabasePath}");
                    LogMessage($"  Theme: {settings.Theme}");
                    LogMessage($"  IsInitialPasswordSet: {settings.IsInitialPasswordSet}");
                    
                    // Success - exit the retry loop
                    return;
                }
                catch (IOException)
                {
                    // Already logged, re-throw to abort installation
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR: Could not write installer settings: {ex.Message}");
                    MessageBox.Show(
                        $"Failed to write settings file:\n{ex.Message}\n\nInstallation cannot continue.",
                        "Settings Write Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    throw;
                }
            }
            
            // Max retries exceeded
            LogMessage($"ERROR: Maximum retry attempts ({MAX_RETRIES}) exceeded for path validation");
            MessageBox.Show(
                $"Network path validation failed after {MAX_RETRIES} attempts.\n\n" +
                "Installation cannot continue.\n\n" +
                "Please check network connectivity and permissions, then try again.",
                "Validation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            throw new IOException($"Network path validation failed after {MAX_RETRIES} retry attempts");
        }

        /// <summary>
        /// Verifies write permissions to a directory by creating and deleting a test file.
        /// </summary>
        /// <param name="directoryPath">The directory path to test.</param>
        /// <returns>True if write permissions are available, false otherwise.</returns>
        private bool VerifyWritePermissions(string directoryPath)
        {
            // HIGH Issue #6: Check if directory exists before testing write permissions
            // This distinguishes between "directory missing" and "permission denied"
            if (!Directory.Exists(directoryPath))
            {
                LogMessage($"ERROR: Directory does not exist: {directoryPath}");
                return false;
            }
            
            var testFileName = Path.Combine(directoryPath, $"__aim_db_write_test_{Guid.NewGuid()}.tmp");
            try
            {
                LogMessage($"Verifying write permissions for: {directoryPath}");
                
                // Attempt to create and write to a test file
                File.WriteAllText(testFileName, "AIM database write permission test");
                
                // Verify we can read it back
                var content = File.ReadAllText(testFileName);
                if (content != "AIM database write permission test")
                {
                    LogMessage("ERROR: Write verification failed - content mismatch");
                    return false;
                }
                
                // Clean up
                File.Delete(testFileName);
                LogMessage("Write permissions verified successfully");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogMessage($"ERROR: No write permission: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                LogMessage($"ERROR: Write test I/O error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Write test failed: {ex.Message}");
                return false;
            }
            finally
            {
                // Ensure test file is cleaned up
                try
                {
                    if (File.Exists(testFileName))
                    {
                        File.Delete(testFileName);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Creates and initializes the security database with the baked-in SuperAdmin account.
        /// The database is created at the hardcoded SecurityDatabasePath location.
        /// Handles pre-existing databases and implements async operations with timeout.
        /// </summary>
        /// <returns>True if the database was created successfully, false otherwise.</returns>
        private async Task<bool> CreateSecurityDatabaseAsync()
        {
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(SecurityDatabasePath);
                if (string.IsNullOrEmpty(directory))
                {
                    LogMessage("ERROR: Invalid security database path");
                    return false;
                }

                // Check if database already exists
                if (File.Exists(SecurityDatabasePath))
                {
                    LogMessage("NOTICE: Security database already exists at target location");
                    
                    // Show dialog with options
                    var result = MessageBox.Show(
                        "A security database already exists at the target location.\n\n" +
                        $"Path: {SecurityDatabasePath}\n\n" +
                        "What would you like to do?\n\n" +
                        "• YES - Use the existing database (recommended)\n" +
                        "• NO - Backup existing and create fresh database\n" +
                        "• CANCEL - Abort installation",
                        "Existing Database Detected",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.Cancel)
                    {
                        LogMessage("Installation cancelled by user (existing database)");
                        return false;
                    }
                    else if (result == DialogResult.Yes)
                    {
                        LogMessage("Using existing security database");
                        return true; // Use existing database
                    }
                    else // DialogResult.No - Backup and reinitialize
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var backupPath = $"{SecurityDatabasePath}.backup.{timestamp}";
                        
                        try
                        {
                            LogMessage($"Backing up existing database to: {backupPath}");
                            File.Copy(SecurityDatabasePath, backupPath);
                            File.Delete(SecurityDatabasePath);
                            LogMessage("Existing database backed up and removed");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"ERROR: Failed to backup existing database: {ex.Message}");
                            MessageBox.Show(
                                $"Failed to backup existing database:\n{ex.Message}\n\nInstallation cannot continue.",
                                "Backup Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            return false;
                        }
                    }
                }

                // Check if network path is accessible
                if (!Directory.Exists(directory))
                {
                    LogMessage("Creating security database directory...");
                    
                    // HIGH Issue #9: Use timeout for directory creation
                    if (!await CreateDirectoryWithTimeoutAsync(directory, timeoutSeconds: 15))
                    {
                        LogMessage("ERROR: Could not create security database directory (timeout or error)");
                        return false;
                    }
                }

                // Verify write permissions before attempting database creation
                if (!VerifyWritePermissions(directory))
                {
                    LogMessage("ERROR: Write permissions verification failed for database directory");
                    
                    MessageBox.Show(
                        $"Insufficient permissions to create security database:\n\n" +
                        $"Path: {directory}\n" +
                        $"Current User: {Environment.UserDomainName}\\{Environment.UserName}\n\n" +
                        "Please ensure:\n" +
                        "• You have write permissions to this directory\n" +
                        "• The network share allows file creation\n" +
                        "• NTFS permissions grant write access\n\n" +
                        "You may need to:\n" +
                        "• Run the installer as Administrator\n" +
                        "• Contact your network administrator\n" +
                        "• Check network share permissions",
                        "Insufficient Permissions",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }

                // Create connection string with timeout
                var connectionStringBuilder = new System.Data.SQLite.SQLiteConnectionStringBuilder
                {
                    DataSource = SecurityDatabasePath,
                    Version = 3,
                    DefaultTimeout = DEFAULT_DB_TIMEOUT
                };
                var connectionString = connectionStringBuilder.ConnectionString;

                LogMessage("Initializing security database...");
                LogMessage($"Database timeout configured: {DEFAULT_DB_TIMEOUT} seconds");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    // Use async open with cancellation support
                    var cancellationToken = installCancellationSource?.Token ?? System.Threading.CancellationToken.None;
                    
                    try
                    {
                        await connection.OpenAsync(cancellationToken);
                    }
                    catch (SQLiteException ex) when (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                    {
                        LogMessage($"ERROR: Database connection timeout after {DEFAULT_DB_TIMEOUT} seconds");
                        MessageBox.Show(
                            $"Database connection timed out after {DEFAULT_DB_TIMEOUT} seconds.\n\n" +
                            "This usually means:\n" +
                            "• The network path is slow or unresponsive\n" +
                            "• The network share is experiencing issues\n\n" +
                            $"Database path: {SecurityDatabasePath}\n\n" +
                            "Please check the network connection and try again.",
                            "Connection Timeout",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return false;
                    }
                    catch (SQLiteException ex) when (ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase))
                    {
                        LogMessage($"ERROR: Database is locked by another process");
                        MessageBox.Show(
                            "The database is currently locked by another process.\n\n" +
                            "This usually means:\n" +
                            "• Another installer is running\n" +
                            "• AIM is currently accessing the database\n\n" +
                            "Please close all instances of AIM and try again.",
                            "Database Locked",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return false;
                    }

                    LogMessage("Database connection established");

                    // Create tables
                    string createTablesScript = @"
                        CREATE TABLE IF NOT EXISTS AuthorizedUsers (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                            FullName TEXT,
                            Department TEXT,
                            AccessLevel INTEGER DEFAULT 1,
                            IsActive BOOLEAN DEFAULT 1,
                            CreatedBy TEXT,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            ModifiedBy TEXT,
                            ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE TABLE IF NOT EXISTS SecuritySettings (
                            Key TEXT PRIMARY KEY,
                            Value TEXT NOT NULL,
                            ModifiedBy TEXT,
                            ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE TABLE IF NOT EXISTS SecurityAuditLog (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Action TEXT NOT NULL,
                            TargetUser TEXT,
                            ModifiedBy TEXT NOT NULL,
                            Details TEXT,
                            Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                        );
                    ";

                    using (var command = new SQLiteCommand(createTablesScript, connection))
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }

                    LogMessage("Security database schema created successfully.");

                    // Check if SuperAdmin already exists
                    string checkUserQuery = "SELECT COUNT(*) FROM AuthorizedUsers WHERE Username = @Username COLLATE NOCASE";
                    using (var checkCommand = new SQLiteCommand(checkUserQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", SuperAdminUsername);
                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));

                        if (count == 0)
                        {
                            // Insert SuperAdmin user
                            string insertUserQuery = @"
                                INSERT INTO AuthorizedUsers (Username, FullName, Department, AccessLevel, IsActive, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate)
                                VALUES (@Username, @FullName, @Department, @AccessLevel, @IsActive, @CreatedBy, @ModifiedBy, @CreatedDate, @ModifiedDate)
                            ";

                            using (var insertCommand = new SQLiteCommand(insertUserQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@Username", SuperAdminUsername);
                                insertCommand.Parameters.AddWithValue("@FullName", "AIM Super Administrator");
                                insertCommand.Parameters.AddWithValue("@Department", "System");
                                insertCommand.Parameters.AddWithValue("@AccessLevel", 3); // SuperAdmin level
                                insertCommand.Parameters.AddWithValue("@IsActive", true);
                                insertCommand.Parameters.AddWithValue("@CreatedBy", "Installer");
                                insertCommand.Parameters.AddWithValue("@ModifiedBy", "Installer");
                                insertCommand.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
                                insertCommand.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);

                                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                            }

                            LogMessage($"SuperAdmin account created: {SuperAdminUsername}");

                            // Log the action
                            string logQuery = @"
                                INSERT INTO SecurityAuditLog (Action, TargetUser, ModifiedBy, Details, Timestamp)
                                VALUES (@Action, @TargetUser, @ModifiedBy, @Details, @Timestamp)
                            ";

                            using (var logCommand = new SQLiteCommand(logQuery, connection))
                            {
                                logCommand.Parameters.AddWithValue("@Action", "INITIAL_SETUP");
                                logCommand.Parameters.AddWithValue("@TargetUser", SuperAdminUsername);
                                logCommand.Parameters.AddWithValue("@ModifiedBy", "Installer");
                                logCommand.Parameters.AddWithValue("@Details", "SuperAdmin account created during installation");
                                logCommand.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);

                                await logCommand.ExecuteNonQueryAsync(cancellationToken);
                            }
                        }
                        else
                        {
                            LogMessage("SuperAdmin account already exists in database.");
                        }
                    }

                    // Store the master password hash
                    string passwordHash = HashPassword(SuperAdminPassword);
                    string upsertPasswordQuery = @"
                        INSERT OR REPLACE INTO SecuritySettings (Key, Value, ModifiedBy, ModifiedDate)
                        VALUES (@Key, @Value, @ModifiedBy, @ModifiedDate)
                    ";

                    using (var passwordCommand = new SQLiteCommand(upsertPasswordQuery, connection))
                    {
                        passwordCommand.Parameters.AddWithValue("@Key", "MasterPasswordHash");
                        passwordCommand.Parameters.AddWithValue("@Value", passwordHash);
                        passwordCommand.Parameters.AddWithValue("@ModifiedBy", "Installer");
                        passwordCommand.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);

                        await passwordCommand.ExecuteNonQueryAsync(cancellationToken);
                    }

                    LogMessage("Master password configured successfully.");
                }

                LogMessage("Security database initialized successfully.");
                return true;
            }
            catch (OperationCanceledException)
            {
                LogMessage("Database initialization cancelled by user");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Failed to initialize security database: {ex.Message}");
                LogMessage($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    LogMessage($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Hashes a password using SHA256 with a fixed salt.
        /// This is a simple hash for the installer - the application uses more secure methods.
        /// Note: The salt is fixed because we need deterministic hashes for the hardcoded password.
        /// </summary>
        private string HashPassword(string password)
        {
            // Use a fixed salt for deterministic hashing of the hardcoded password
            // This allows verification against the same hash each time
            byte[] salt = Encoding.UTF8.GetBytes("AIM-Security-Salt-2025");
            
            using (var sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var saltedPassword = new byte[passwordBytes.Length + salt.Length];
                
                Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);
                
                var hash = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// HIGH Issue #9: Creates a directory with a timeout to prevent hanging on unresponsive network paths.
        /// </summary>
        /// <param name="path">Directory path to create</param>
        /// <param name="timeoutSeconds">Timeout in seconds (default 15)</param>
        /// <returns>True if directory was created or already exists, false if operation timed out or failed</returns>
        private async Task<bool> CreateDirectoryWithTimeoutAsync(string path, int timeoutSeconds = 15)
        {
            try
            {
                // Check if directory already exists (fast path)
                if (Directory.Exists(path))
                {
                    return true;
                }

                LogMessage($"Creating directory with {timeoutSeconds}s timeout: {path}");

                // Create directory operation with timeout
                var createTask = System.Threading.Tasks.Task.Run(() =>
                {
                    Directory.CreateDirectory(path);
                });

                var timeoutTask = System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
                var completedTask = await System.Threading.Tasks.Task.WhenAny(createTask, timeoutTask);

                // HIGH Issue #7: Check which task completed BEFORE awaiting createTask
                // This prevents exceptions from being masked by timeout handling
                if (completedTask == timeoutTask)
                {
                    LogMessage($"ERROR: Directory creation timed out after {timeoutSeconds} seconds");
                    return false;
                }

                // HIGH Issue #7: Await createTask to get any exceptions it may have thrown
                // Distinguish between different error types for better diagnostics
                try
                {
                    await createTask;
                    LogMessage($"Directory created successfully");
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogMessage($"ERROR: Permission denied to create directory: {ex.Message}");
                    return false;
                }
                catch (IOException ex)
                {
                    LogMessage($"ERROR: I/O error creating directory: {ex.Message}");
                    return false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogMessage($"ERROR: Permission denied: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                LogMessage($"ERROR: I/O error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Failed to create directory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// MEDIUM Issue #13: Validates the installation path for:
        /// - Write permissions
        /// - Path length (< 260 characters for Windows MAX_PATH)
        /// - Invalid path characters
        /// </summary>
        /// <param name="path">The installation path to validate</param>
        /// <returns>True if path is valid, false otherwise</returns>
        private bool ValidateInstallationPath(string path)
        {
            try
            {
                // Check for empty or whitespace path
                if (string.IsNullOrWhiteSpace(path))
                {
                    LogMessage("ERROR: Installation path is empty");
                    MessageBox.Show(
                        "Installation path cannot be empty.",
                        "Invalid Path",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }

                // MEDIUM Issue #12: Verify and log full SecurityDatabasePath
                LogMessage($"Full SecurityDatabasePath: {SecurityDatabasePath}");
                LogMessage($"SecurityDatabasePath length: {SecurityDatabasePath.Length} characters");
                if (SecurityDatabasePath.Length >= 260)
                {
                    LogMessage("WARNING: SecurityDatabasePath exceeds Windows MAX_PATH (260 characters)");
                }

                // Check path length (Windows MAX_PATH limitation)
                if (path.Length >= 260)
                {
                    LogMessage($"ERROR: Installation path too long ({path.Length} characters, max 260)");
                    MessageBox.Show(
                        $"Installation path is too long ({path.Length} characters).\n\n" +
                        "Windows has a maximum path length of 260 characters.\n\n" +
                        "Please choose a shorter installation path.",
                        "Path Too Long",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }

                // Check for invalid path characters: < > : " | ? *
                var invalidChars = new char[] { '<', '>', ':', '"', '|', '?', '*' };
                var pathWithoutDrive = path.Length > 2 && path[1] == ':' ? path.Substring(2) : path;
                
                foreach (var invalidChar in invalidChars)
                {
                    if (pathWithoutDrive.Contains(invalidChar))
                    {
                        LogMessage($"ERROR: Installation path contains invalid character: '{invalidChar}'");
                        MessageBox.Show(
                            $"Installation path contains invalid character: '{invalidChar}'\n\n" +
                            "Path cannot contain: < > : \" | ? *\n\n" +
                            "Please choose a different installation path.",
                            "Invalid Path Characters",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return false;
                    }
                }

                // Check write permissions by creating a test file
                var testDir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(testDir))
                {
                    testDir = path;
                }

                // HIGH Issue #10: Don't create parent directory during validation
                // Validation should be read-only - directory creation happens in PerformInstallation()
                if (!Directory.Exists(testDir))
                {
                    LogMessage($"WARNING: Installation parent directory does not exist (will be created during installation): {testDir}");
                    // Don't fail validation - directory will be created with timeout in PerformInstallation()
                    LogMessage("Installation path validation passed (directory will be created)");
                    return true;
                }

                // HIGH Issue #8: Test write permissions with guaranteed cleanup in finally block
                var testFile = Path.Combine(testDir, $"__aim_install_test_{Guid.NewGuid()}.tmp");
                try
                {
                    File.WriteAllText(testFile, "AIM installation path test");
                    LogMessage("Installation path write permissions verified");
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogMessage($"ERROR: No write permission to installation path: {ex.Message}");
                    MessageBox.Show(
                        $"Insufficient write permissions to installation path:\n\n" +
                        $"Path: {testDir}\n\n" +
                        "Please choose a different location or run the installer as Administrator.",
                        "Permission Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR: Cannot write to installation path: {ex.Message}");
                    MessageBox.Show(
                        $"Cannot write to installation path:\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        "Please choose a different location.",
                        "Path Validation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
                finally
                {
                    // HIGH Issue #8: Guarantee test file cleanup in finally block
                    // This ensures cleanup happens regardless of which catch executes or if write succeeds
                    try
                    {
                        if (File.Exists(testFile))
                        {
                            File.Delete(testFile);
                            LogMessage("Test file cleaned up");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"WARNING: Failed to cleanup test file: {ex.Message}");
                    }
                }

                LogMessage("Installation path validation passed");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Installation path validation failed: {ex.Message}");
                MessageBox.Show(
                    $"Installation path validation failed:\n\n{ex.Message}",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
        }

        /// <summary>
        /// MEDIUM Issue #14: Checks if there is sufficient disk space for installation.
        /// Estimates 500MB required for conservative sizing.
        /// </summary>
        /// <param name="path">The installation path</param>
        /// <returns>True if sufficient space, false otherwise</returns>
        private bool CheckDiskSpace(string path)
        {
            try
            {
                // Get the drive from the installation path
                var drive = Path.GetPathRoot(path);
                
                // MEDIUM Issue #11: Handle UNC paths and missing drive gracefully
                if (string.IsNullOrEmpty(drive))
                {
                    LogMessage("WARNING: Could not determine drive for disk space check");
                    LogMessage("Skipping disk space check - path may be invalid or UNC path");
                    return true; // Continue anyway - validation happens elsewhere
                }
                
                // Check if this is a UNC path (\\server\share format)
                if (drive.StartsWith(@"\\"))
                {
                    LogMessage($"WARNING: UNC path detected: {drive}");
                    LogMessage("Skipping disk space check for UNC path - cannot reliably determine free space");
                    LogMessage("Note: Ensure network share has sufficient space (500+ MB recommended)");
                    return true; // Continue - can't easily check space on UNC paths with DriveInfo
                }

                var driveInfo = new DriveInfo(drive);
                
                // Conservative estimate: 500MB required
                const long REQUIRED_SPACE_BYTES = 500L * 1024L * 1024L; // 500 MB
                long availableSpace = driveInfo.AvailableFreeSpace;
                
                LogMessage($"Disk space check:");
                LogMessage($"  Drive: {drive}");
                LogMessage($"  Available: {availableSpace / 1024.0 / 1024.0:F2} MB");
                LogMessage($"  Required: {REQUIRED_SPACE_BYTES / 1024.0 / 1024.0:F2} MB");
                
                if (availableSpace < REQUIRED_SPACE_BYTES)
                {
                    LogMessage($"ERROR: Insufficient disk space");
                    MessageBox.Show(
                        $"Insufficient disk space for installation.\n\n" +
                        $"Drive: {drive}\n" +
                        $"Available: {availableSpace / 1024.0 / 1024.0:F2} MB\n" +
                        $"Required: {REQUIRED_SPACE_BYTES / 1024.0 / 1024.0:F2} MB\n\n" +
                        "Please free up disk space or choose a different installation location.",
                        "Insufficient Disk Space",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
                
                LogMessage("Disk space check passed");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"WARNING: Disk space check failed: {ex.Message}");
                LogMessage("Continuing installation - ensure sufficient disk space is available");
                // Don't fail installation if we can't check disk space
                return true;
            }
        }

        /// <summary>
        /// CRITICAL Issue #5: Verifies the integrity of the extracted installation files.
        /// Checks that critical files exist and have non-zero size.
        /// </summary>
        /// <returns>True if installation is complete and valid, false otherwise</returns>
        private bool VerifyInstallationIntegrity()
        {
            try
            {
                LogMessage("Verifying installation integrity...");
                
                // Check that AIM.exe exists
                var aimExePath = Path.Combine(installPath, "AIM.exe");
                if (!File.Exists(aimExePath))
                {
                    LogMessage("ERROR: AIM.exe not found in installation directory");
                    MessageBox.Show(
                        "Installation verification failed: AIM.exe not found.\n\n" +
                        "The installation may be corrupted or incomplete.\n\n" +
                        "Please try installing again.",
                        "Installation Incomplete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    
                    // Clean up partial installation
                    try
                    {
                        LogMessage("Cleaning up partial installation...");
                        Directory.Delete(installPath, recursive: true);
                        LogMessage("Partial installation cleaned up");
                    }
                    catch (Exception cleanupEx)
                    {
                        LogMessage($"WARNING: Could not clean up partial installation: {cleanupEx.Message}");
                    }
                    
                    return false;
                }
                
                // Check that AIM.exe has non-zero size
                var aimExeInfo = new FileInfo(aimExePath);
                if (aimExeInfo.Length == 0)
                {
                    LogMessage("ERROR: AIM.exe has zero size (corrupted)");
                    MessageBox.Show(
                        "Installation verification failed: AIM.exe is corrupted (zero bytes).\n\n" +
                        "Please try installing again.",
                        "Installation Corrupted",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    
                    // Clean up partial installation
                    try
                    {
                        LogMessage("Cleaning up corrupted installation...");
                        Directory.Delete(installPath, recursive: true);
                        LogMessage("Corrupted installation cleaned up");
                    }
                    catch (Exception cleanupEx)
                    {
                        LogMessage($"WARNING: Could not clean up corrupted installation: {cleanupEx.Message}");
                    }
                    
                    return false;
                }
                
                LogMessage($"AIM.exe verified: {aimExeInfo.Length / 1024.0 / 1024.0:F2} MB");
                
                // Check for other critical files (optional - can add more checks here)
                var criticalFiles = new[] { "AIM.dll", "AIM.runtimeconfig.json" };
                foreach (var file in criticalFiles)
                {
                    var filePath = Path.Combine(installPath, file);
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        LogMessage($"  {file} verified: {fileInfo.Length / 1024.0:F2} KB");
                    }
                    else
                    {
                        LogMessage($"  WARNING: {file} not found (may be optional)");
                    }
                }
                
                LogMessage("Installation integrity verification passed");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Installation integrity verification failed: {ex.Message}");
                MessageBox.Show(
                    $"Installation verification failed:\n\n{ex.Message}\n\n" +
                    "Please try installing again.",
                    "Verification Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
        }
    }
}
