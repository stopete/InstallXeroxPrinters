using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InstallDKLPrinters
{
    public partial class Form1 : Form
    {
        // Spinner animation frames for progress indication
        private string[] spinnerFrames = new[] { "|", "/", "-", "\\" };
        private int spinnerIndex = 0;
        private bool isInstalling = false;

        public Form1()
        {
            InitializeComponent();        // Initialize UI components
            InitializeStatusStrip();      // Set up the status bar
            InitializeTimer();            // Start the timer for UI updates
        }

        // Event handler for the "Install Printers" button click
        private async void btnInstallPrinters_Click(object sender, EventArgs e)
        {
            btnInstallPrinters.Enabled = false; // Disable button during installation
            progressBar1.Style = ProgressBarStyle.Marquee; // Show indeterminate progress
            progressBar1.MarqueeAnimationSpeed = 30;

            isInstalling = true;
            lblProgress.Text = "🖨️ Installing printers... Please wait |";

            // Run printer installation in a background thread
            await Task.Run(() => InstallPrinters());

            // Update UI after installation completes
            isInstalling = false;
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.Value = 100;
            lblProgress.Text = "✅ Printers installed successfully!";
            btnInstallPrinters.Enabled = true;

            // Prompt user to restart the system
            DialogResult result = MessageBox.Show(
                "Installation complete. To apply all printer settings, the computer needs to restart.\nDo you want to restart now?",
                "Restart Required",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.OK)
            {
                Log("🔁 Restarting system in 30 seconds...");
                Process.Start("shutdown", "/r /t 30"); // Schedule restart
            }
            else
            {
                Log("⏳ Restart skipped by user.");
            }
        }

        // Executes PowerShell scripts to install printers and apply settings
        private void InstallPrinters()
        {
            try
            {
                string workingDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\";

                // Define paths to PowerShell scripts
                string installPrintersScript = Path.Combine(workingDir, "installprinters.ps1");
                string setSecureScript = Path.Combine(workingDir, "SeSecurePrintSettings.ps1");

                // Run installprinters.ps1
                Log("▶️ Running installprinters.ps1...");
                RunCommand("powershell", $"-ExecutionPolicy Bypass -File \"{installPrintersScript}\"");

                // Run SeSecurePrintSettings.ps1
                Log("▶️ Running SeSecurePrintSettings.ps1...");
                RunCommand("powershell", $"-ExecutionPolicy Bypass -File \"{setSecureScript}\"");

                Log("✅ All scripts executed successfully.");
            }
            catch (Exception ex)
            {
                Log("❌ ERROR: " + ex.Message);
            }
        }

        // Returns an integer representing the processor architecture
        private int GetProcessorArchitecture()
        {
            Architecture arch = RuntimeInformation.OSArchitecture;

            if (arch == Architecture.X86)
                return 0;
            else if (arch == Architecture.X64)
                return 9;
            else if (arch == Architecture.Arm || arch == Architecture.Arm64)
                return 12;
            else
                return -1; // Unknown architecture
        }

        // Runs a command-line process and logs output/errors
        private void RunCommand(string exe, string args)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            // Log standard output and error asynchronously
            proc.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
            proc.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("⚠️ " + e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit(); // Wait for process to finish
        }

        // Logs messages to the text box with timestamp
        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Log(message)));
                return;
            }

            textBox1.AppendText($"{DateTime.Now:T} - {message}{Environment.NewLine}");
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret(); // Auto-scroll to bottom
        }

        // Initializes the status strip with date, time, and copyright
        private void InitializeStatusStrip()
        {
            StatusStrip statusStrip1 = new StatusStrip();

            ToolStripStatusLabel dateLabel = new ToolStripStatusLabel();
            ToolStripStatusLabel timeLabel = new ToolStripStatusLabel();
            ToolStripStatusLabel copyrightLabel = new ToolStripStatusLabel();

            dateLabel.Name = "dateLabel";
            dateLabel.Text = DateTime.Now.ToString("MMMM dd, yyyy");

            timeLabel.Name = "timeLabel";
            timeLabel.Text = DateTime.Now.ToString("hh:mm:ss tt");

            copyrightLabel.Name = "copyrightLabel";
            copyrightLabel.Text = "Topete © 2025";

            // Add spacing between items
            ToolStripStatusLabel spacer1 = new ToolStripStatusLabel() { Spring = true };
            ToolStripStatusLabel spacer2 = new ToolStripStatusLabel() { Spring = true };

            // Add items to StatusStrip
            statusStrip1.Items.Add(dateLabel);
            statusStrip1.Items.Add(spacer1);
            statusStrip1.Items.Add(timeLabel);
            statusStrip1.Items.Add(spacer2);
            statusStrip1.Items.Add(copyrightLabel);

            // Add StatusStrip to the form
            this.Controls.Add(statusStrip1);
        }

        // Initializes and starts the timer for UI updates
        private void InitializeTimer()
        {
            timer1 = new System.Windows.Forms.Timer
            {
                Interval = 250 // Faster tick for smoother spinner
            };
            timer1.Tick += Timer_Tick;
            timer1.Start();
        }

        // Timer tick event updates date/time and spinner animation
        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Update date and time labels in the status strip
            foreach (Control control in this.Controls)
            {
                if (control is StatusStrip statusStrip)
                {
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        if (item.Name == "dateLabel")
                            item.Text = DateTime.Now.ToString("MMMM dd, yyyy");
                        else if (item.Name == "timeLabel")
                            item.Text = DateTime.Now.ToString("hh:mm:ss tt");
                    }
                }
            }

            // Update spinner animation if installation is in progress
            if (isInstalling)
            {
                spinnerIndex = (spinnerIndex + 1) % spinnerFrames.Length;
                lblProgress.Text = $"🖨️ Installing printers... Please wait {spinnerFrames[spinnerIndex]}";
            }
        }
    }
}
