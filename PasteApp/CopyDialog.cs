using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Design;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace PasteApp
{
    public partial class CopyDialog : Form
    {
        private Stopwatch stopwatch = new Stopwatch();

        private TableLayoutPanel tlp;
        private Label lblDescription;
        private TableLayoutPanel tlpHorizontal;
        private Label lblPercentage;
        private Button btnPause;
        private Button btnCancel;
        private ProgressBar pbProgressBar;
        private Label lblSpeed;
        private Label lblCurrentFileName;
        private Label lblTimeRemaining;
        private Label lblItemsRemaining;

        private readonly long totalFileCount;
        private readonly long totalFileSize;
        private long filesProcessed;
        private long bytesCopied;
        private double speed;

        private double currentFilePercentage;
        private long currentFileSize;
        private string currentFile;

        private Regex reNewFile = new Regex(@"\s*(\d+)\s+(.+)", RegexOptions.Compiled);
        private Regex rePercUpdate = new Regex(@"\s*(\d{1,3}(\.\d)?)%", RegexOptions.Compiled);

        private bool isPaused = false;

        public CopyDialog(long totalFileCount, long totalFileSize, string sourceDir, string destDir)
        {
            this.totalFileCount = totalFileCount;
            this.totalFileSize = totalFileSize;
            this.filesProcessed = 0;
            this.bytesCopied = 0;
            this.currentFilePercentage = 0;
            this.currentFileSize = 0;
            this.speed = 0.00001;            

            InitializeComponent();
            InitializeLabels(totalFileCount, totalFileSize, sourceDir, destDir);
            stopwatch.Start();
        }

        public void RobocopyOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (String.IsNullOrEmpty(outLine.Data))
                return;

            if (totalFileSize == 0 || totalFileCount == 0)
            {
                // We act as if everything is copied. 
                filesProcessed = totalFileCount;
                bytesCopied = totalFileSize;
               
                this.Close();
                return;
            }

            // Check if new file is being processed.
            Match m = reNewFile.Match(outLine.Data);
            if (m.Success && File.Exists(m.Groups[2].Value))
            {
                currentFileSize = Convert.ToInt64(m.Groups[1].Value);
                currentFilePercentage = 0;
                currentFile = m.Groups[2].Value;
                lblCurrentFileName.Text = "Name: " + currentFile;
            }

            // Check if file percentage is updated.
            m = rePercUpdate.Match(outLine.Data);
            if (m.Success)
            {
                double currentFilePercentage = Convert.ToDouble(m.Groups[1].Value);
                long filesRemaining;
                long bytesRemaining;
                long newPercentage;
                long timeRemaining;

                if (Math.Abs(currentFilePercentage - 100.0) < 0.0001)
                {
                    filesProcessed++;
                    bytesCopied += currentFileSize;

                    filesRemaining = totalFileCount - filesProcessed;
                    bytesRemaining = totalFileSize - bytesCopied;
                    newPercentage = (long)Math.Round(100.0 * bytesCopied / totalFileSize);
                    speed = 0.00001 + (bytesCopied * 1000.0 / stopwatch.ElapsedMilliseconds); // Speed in B/s    
                    timeRemaining = (long)(bytesRemaining / speed);

                }
                else
                {
                    filesRemaining = totalFileCount - filesProcessed;
                    bytesRemaining = totalFileSize - bytesCopied - (long)(currentFilePercentage / 100 * currentFileSize);
                    newPercentage = (long)Math.Round(100.0 * (totalFileSize - bytesRemaining) / totalFileSize);
                    speed = 0.00001 + ((totalFileSize - bytesRemaining) * 1000.0 / stopwatch.ElapsedMilliseconds); // Speed in B/s 
                    timeRemaining = (long)(bytesRemaining / speed);

                }

                lblItemsRemaining.Text = "Items remaining: " + filesRemaining + " (" + BytesCompactForm(bytesRemaining) + ")";
                this.Text = newPercentage + "% complete";
                lblPercentage.Text = this.Text;
                pbProgressBar.Value = Convert.ToInt32(newPercentage);
                lblSpeed.Text = "Speed: " + SpeedCompactForm(speed);
                lblTimeRemaining.Text = "Time remaining: About " + TimeRemainingCompactForm(timeRemaining);

                // When folders and files are copied, close the form. 
                if (filesProcessed == totalFileCount)
                    this.Close();
            }

            this.Refresh();
        }

        public void RobocopyErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // TODO
            if (outLine.Data == null)
                return;

            this.Refresh();
        }

        private string BytesCompactForm(long bytes)
        {
            long orderOfMagnitude = 1;
            if (bytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", bytes) + "B";
            orderOfMagnitude *= 1024;
            if (bytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", (Convert.ToDouble(bytes) / orderOfMagnitude)) + "KB";
            orderOfMagnitude *= 1024;
            if (bytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", (Convert.ToDouble(bytes) / orderOfMagnitude)) + "MB";
            orderOfMagnitude *= 1024;
            if (bytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", (Convert.ToDouble(bytes) / orderOfMagnitude)) + "GB";
            orderOfMagnitude *= 1024;
            return "" + string.Format("{0:0.00}", (Convert.ToDouble(bytes) / orderOfMagnitude)) + "TB";
        }

        private string SpeedCompactForm(double speedInBytes)
        {
            long orderOfMagnitude = 1;
            if (speedInBytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", speedInBytes) + " B/s";
            orderOfMagnitude *= 1024;
            if (speedInBytes < orderOfMagnitude * 1024) 
                return "" + string.Format("{0:0.00}", speedInBytes / orderOfMagnitude) + " KB/s";
            orderOfMagnitude *= 1024;
            if (speedInBytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", speedInBytes / orderOfMagnitude) + " MB/s";
            orderOfMagnitude *= 1024;
            if (speedInBytes < orderOfMagnitude * 1024)
                return "" + string.Format("{0:0.00}", speedInBytes / orderOfMagnitude) + "GB/s";
            orderOfMagnitude *= 1024;
            return "" + string.Format("{0:0.00}", speedInBytes / orderOfMagnitude) + "TB/s";

        }

        private string TimeRemainingCompactForm(long timeRemaining)
        {
            if (timeRemaining >= TimeSpan.MaxValue.TotalSeconds)
                return String.Format("{0} days", timeRemaining / (60 * 60 * 24));

            TimeSpan ts = TimeSpan.FromSeconds(Convert.ToDouble(timeRemaining));
            if (ts.TotalDays >= 1)
                return "" + ts.Days + " days and " + ts.Hours + " hours";
            else if (ts.Hours != 0)
                return "" + ts.Hours + " hours and " + ts.Minutes + " minutes";
            else if (ts.Minutes != 0)
                return "" + ts.Minutes + " minutes";
            else
                return "" + ts.Seconds + " seconds";

        }

        public void InitializeLabels(long fileCount, long totalFileSize, string sourceDir, string destDir)
        {
            this.lblDescription.Text = "Copying " + fileCount + " items from " + sourceDir + " to " + destDir;
            this.lblPercentage.Text = "0% complete";
            this.Text = this.lblPercentage.Text;
            this.lblItemsRemaining.Text = "Items remaining: " + fileCount + "(" + BytesCompactForm(totalFileSize) + ")";
            this.lblTimeRemaining.Text = "Time remaining:";
            this.lblCurrentFileName.Text = "Name: ";
            this.lblSpeed.Text = "Speed: ";
            this.pbProgressBar.Maximum = 100; // 100%
        }

        private void InitializeComponent()
        {
            this.tlp = new System.Windows.Forms.TableLayoutPanel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.pbProgressBar = new System.Windows.Forms.ProgressBar();
            this.lblCurrentFileName = new System.Windows.Forms.Label();
            this.lblTimeRemaining = new System.Windows.Forms.Label();
            this.lblItemsRemaining = new System.Windows.Forms.Label();
            this.tlpHorizontal = new System.Windows.Forms.TableLayoutPanel();
            this.lblPercentage = new System.Windows.Forms.Label();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.tlp.SuspendLayout();
            this.tlpHorizontal.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlp
            // 
            this.tlp.ColumnCount = 1;
            this.tlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp.Controls.Add(this.lblDescription, 0, 0);
            this.tlp.Controls.Add(this.pbProgressBar, 0, 2);
            this.tlp.Controls.Add(this.lblCurrentFileName, 0, 4);
            this.tlp.Controls.Add(this.lblTimeRemaining, 0, 5);
            this.tlp.Controls.Add(this.lblItemsRemaining, 0, 6);
            this.tlp.Controls.Add(this.tlpHorizontal, 0, 1);
            this.tlp.Controls.Add(this.lblSpeed, 0, 3);
            this.tlp.Location = new System.Drawing.Point(25, 15);
            this.tlp.Name = "tlp";
            this.tlp.RowCount = 7;
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp.Size = new System.Drawing.Size(400, 190);
            this.tlp.TabIndex = 0;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(3, 5);
            this.lblDescription.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(10, 13);
            this.lblDescription.TabIndex = 0;
            this.lblDescription.Text = " ";
            // 
            // pbProgressBar
            // 
            this.pbProgressBar.Location = new System.Drawing.Point(3, 69);
            this.pbProgressBar.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.pbProgressBar.Name = "pbProgressBar";
            this.pbProgressBar.Size = new System.Drawing.Size(394, 21);
            this.pbProgressBar.TabIndex = 2;
            // 
            // lblCurrentFileName
            // 
            this.lblCurrentFileName.AutoSize = true;
            this.lblCurrentFileName.Location = new System.Drawing.Point(3, 123);
            this.lblCurrentFileName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.lblCurrentFileName.Name = "lblCurrentFileName";
            this.lblCurrentFileName.Size = new System.Drawing.Size(41, 13);
            this.lblCurrentFileName.TabIndex = 4;
            this.lblCurrentFileName.Text = "Name: ";
            // 
            // lblTimeRemaining
            // 
            this.lblTimeRemaining.AutoSize = true;
            this.lblTimeRemaining.Location = new System.Drawing.Point(3, 146);
            this.lblTimeRemaining.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.lblTimeRemaining.Name = "lblTimeRemaining";
            this.lblTimeRemaining.Size = new System.Drawing.Size(84, 13);
            this.lblTimeRemaining.TabIndex = 5;
            this.lblTimeRemaining.Text = "Time remaining: ";
            // 
            // lblItemsRemaining
            // 
            this.lblItemsRemaining.AutoSize = true;
            this.lblItemsRemaining.Location = new System.Drawing.Point(3, 169);
            this.lblItemsRemaining.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.lblItemsRemaining.Name = "lblItemsRemaining";
            this.lblItemsRemaining.Size = new System.Drawing.Size(86, 13);
            this.lblItemsRemaining.TabIndex = 6;
            this.lblItemsRemaining.Text = "Items remaining: ";
            // 
            // tlpHorizontal
            // 
            this.tlpHorizontal.ColumnCount = 4;
            this.tlpHorizontal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpHorizontal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpHorizontal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpHorizontal.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpHorizontal.Controls.Add(this.lblPercentage, 0, 0);
            this.tlpHorizontal.Controls.Add(this.btnPause, 1, 0);
            this.tlpHorizontal.Controls.Add(this.btnCancel, 3, 0);
            this.tlpHorizontal.Location = new System.Drawing.Point(3, 26);
            this.tlpHorizontal.Name = "tlpHorizontal";
            this.tlpHorizontal.RowCount = 1;
            this.tlpHorizontal.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpHorizontal.Size = new System.Drawing.Size(394, 34);
            this.tlpHorizontal.TabIndex = 7;
            // 
            // lblPercentage
            // 
            this.lblPercentage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblPercentage.Location = new System.Drawing.Point(3, 8);
            this.lblPercentage.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.lblPercentage.Name = "lblPercentage";
            this.lblPercentage.Size = new System.Drawing.Size(200, 18);
            this.lblPercentage.TabIndex = 1;
            this.lblPercentage.Text = "0% complete";
            // 
            // btnPause
            // 
            this.btnPause.Image = global::PasteApp.Properties.Resources.PauseIcon;
            this.btnPause.Location = new System.Drawing.Point(294, 5);
            this.btnPause.Margin = new System.Windows.Forms.Padding(5, 5, 3, 3);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(25, 25);
            this.btnPause.TabIndex = 2;
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Image = global::PasteApp.Properties.Resources.CancelIcon;
            this.btnCancel.Location = new System.Drawing.Point(364, 5);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(5, 5, 3, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(25, 25);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(3, 102);
            this.lblSpeed.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(44, 13);
            this.lblSpeed.TabIndex = 8;
            this.lblSpeed.Text = "Speed: ";
            // 
            // CopyDialog
            // 
            this.ClientSize = new System.Drawing.Size(450, 210);
            this.Controls.Add(this.tlp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "CopyDialog";
            this.Text = "0% complete";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CopyDialog_FormClosing);
            this.tlp.ResumeLayout(false);
            this.tlp.PerformLayout();
            this.tlpHorizontal.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (isPaused == false)
            {
                Program.PauseCopying();
                stopwatch.Stop();
                isPaused = true;
                btnPause.Image = Properties.Resources.ResumeIcon;
            }
            else
            {
                Program.ResumeCopying();
                stopwatch.Start();
                isPaused = false;
                btnPause.Image = Properties.Resources.PauseIcon;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Program.PauseCopying();
            stopwatch.Stop();

            var result = MessageBox.Show(
                "Are you sure you wish to abort copying operation?", 
                "Confirmation", 
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Program.AbortCopying();
                Application.Exit();
            }
            else
            {
                Program.ResumeCopying();
                stopwatch.Start();
            }
        }

        private void CopyDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the app hasn't finished, prompt a confirmation dialog.
            if (filesProcessed != totalFileCount)
            {
                Program.PauseCopying();

                var result = MessageBox.Show(
                    "Are you sure you wish to abort copying operation?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    Program.AbortCopying();
                }
                else
                {
                    // Prevent from from closing.
                    e.Cancel = true;
                    Program.ResumeCopying();
                }
            }
        }

    }
}

