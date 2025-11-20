using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace AIM.Installer
{
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

        // Hardcoded network paths - baked into installer
        private const string DefaultRootDirectory = @"\\oh1cam01\cml\Internal\LAB STOCK\LAB STOCK";
        private const string ArchivePath = @"\\oh1cam01\cml\Internal\LAB STOCK\Archive";
        private const string ShippedDirectory = @"\\oh1cam01\cml\Internal\LAB STOCK\Orders shipped";
        private const string FileScansDirectory = @"C:\Tfile";
        private const string InventoryArchiveDirectory = @"\\oh1cam01\cml\Internal\LAB STOCK\Physical Inventory Archive";
        private const string SecurityDatabasePath = @"\\oh1cam01\cml\Internal\LAB STOCK\Important Inventory Related Documents\AIM\AIM_Security.db";

        // Hardcoded SuperAdmin credentials - baked into installer
        private const string SuperAdminUsername = "AIMAdmin";
        private const string SuperAdminPassword = "AIM@2025!SecurePass";

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

                    // Write installer settings to user's LocalAppData
                    LogMessage("Writing installer settings...");
                    WriteInstallerSettings();

                    // Initialize security database with SuperAdmin account
                    LogMessage("Initializing security database...");
                    CreateSecurityDatabase();

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
        /// Writes the installer settings to the user's LocalAppData folder.
        /// The path matches where SettingsService expects to find settings.json.
        /// All directory paths are hardcoded as constants in the installer.
        /// </summary>
        private void WriteInstallerSettings()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // Simplified path for AIM settings
                var aimConfigDir = Path.Combine(localAppData, "AIM");
                Directory.CreateDirectory(aimConfigDir);

                var settingsPath = Path.Combine(aimConfigDir, "settings.json");
                
                // Create AppSettings object with hardcoded network paths
                var settings = new Dictionary<string, object>
                {
                    { "DefaultRootDirectory", DefaultRootDirectory },
                    { "ArchivePath", ArchivePath },
                    { "ShippedDirectory", ShippedDirectory },
                    { "FileScansDirectory", FileScansDirectory },
                    { "InventoryArchiveDirectory", InventoryArchiveDirectory },
                    { "SecurityDatabasePath", SecurityDatabasePath },
                    { "Theme", "FollowSystem" },
                    { "AuthorizedUsers", new string[] { } },
                    { "IsInitialPasswordSet", false }
                };

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
        /// Creates and initializes the security database with the baked-in SuperAdmin account.
        /// The database is created at the hardcoded SecurityDatabasePath location.
        /// </summary>
        private void CreateSecurityDatabase()
        {
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(SecurityDatabasePath);
                if (string.IsNullOrEmpty(directory))
                {
                    LogMessage("Warning: Invalid security database path");
                    return;
                }

                // Check if network path is accessible
                if (!Directory.Exists(directory))
                {
                    LogMessage("Creating security database directory...");
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Warning: Could not create security database directory: {ex.Message}");
                        LogMessage("Security database will need to be initialized manually.");
                        return;
                    }
                }

                // Create connection string
                var connectionString = $"Data Source={SecurityDatabasePath};Version=3;";

                LogMessage("Initializing security database...");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

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
                        command.ExecuteNonQuery();
                    }

                    LogMessage("Security database schema created successfully.");

                    // Check if SuperAdmin already exists
                    string checkUserQuery = "SELECT COUNT(*) FROM AuthorizedUsers WHERE Username = @Username COLLATE NOCASE";
                    using (var checkCommand = new SQLiteCommand(checkUserQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", SuperAdminUsername);
                        var count = Convert.ToInt32(checkCommand.ExecuteScalar());

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

                                insertCommand.ExecuteNonQuery();
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

                                logCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            LogMessage("SuperAdmin account already exists.");
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

                        passwordCommand.ExecuteNonQuery();
                    }

                    LogMessage("Master password configured successfully.");
                }

                LogMessage("Security database initialized successfully.");
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not initialize security database: {ex.Message}");
                LogMessage("Security database will need to be initialized manually.");
            }
        }

        /// <summary>
        /// Hashes a password using SHA256.
        /// This is a simple hash for the installer - the application uses more secure methods.
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
