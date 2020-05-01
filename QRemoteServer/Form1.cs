using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QRemoteServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            headerPB.Width = this.Width;
            headerPB.Left = 0;
            headerPB.Top = 0;
            headerPB.BackColor = Color.FromArgb(39, 150, 214);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AsynchronousSocketListener.StartListening(logTextBox, ipTextBox, ConfigManager.IP, ConfigManager.Port);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Form1_Resize(sender, e);
        }

        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                notifyIcon1.Visible = false;
            }
            else
                notifyIcon1.ShowBalloonTip(2000, "ip:порт", ipTextBox.Text, ToolTipIcon.Info);
        }
    }
}
