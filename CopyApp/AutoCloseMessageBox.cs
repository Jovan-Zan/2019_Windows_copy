using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyApp
{
    public partial class AutoCloseMessageBox : Form
    {
        private Timer timer;

        public AutoCloseMessageBox(string caption, string message, int interval)
        {
            InitializeComponent();

            this.Text = caption;
            this.label1.Text = message;

            this.timer = new Timer();
            this.timer.Interval = interval;
            this.timer.Tick += (s, e) => this.Close();
            this.timer.Start();
        }
    }
}
