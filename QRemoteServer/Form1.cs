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
        ConfigData cfg;
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
            cfg = ConfigManager.GetConfigData();
            AsynchronousSocketListener.StartListening(logTextBox, ipTextBox, cfg.IP, cfg.Port);
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
            // Сворачивание в трей при старте
            WindowState = FormWindowState.Minimized;
            Form1_Resize(sender, e);
            // Добавление приложения в автозагузку
            if (cfg.AutoRun)
            {
                if (!AutorunManager.isAppOnAutorun() && !AutorunManager.SetAutorunValue(true))
                    MessageBox.Show("Не удалось добавить приложение в автозагрузку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                if(AutorunManager.isAppOnAutorun())
                    AutorunManager.SetAutorunValue(false);

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
                contextMenuStrip1.Items[0].Text = ipTextBox.Text;
        }

        private void ToolStripRestoreButton_Click(object sender, EventArgs e)
        {
            NotifyIcon1_MouseClick(sender, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
        }

        private void ToolStripExitButton1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
