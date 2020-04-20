/*
 * Copyright (c) 2012 - 2017, Kurt Cancemi (kurt@x64architecture.com)
 *
 * This file is part of Wnmp.
 *
 *  Wnmp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Wnmp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Wnmp.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Wnmp.Programs;
using Wnmp.Wnmp.UI;

namespace Wnmp.UI
{
    public partial class MainFrm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style &= ~0x00040000; // Remove WS_THICKFRAME (Disables resizing)
                return cp;
            }
        }

        NginxProgram _nginx;
        MariaDBProgram _mariaDb;
        PHPProgram _php;

        ContextMenuStrip _nginxConfigContextMenuStrip, _nginxLogContextMenuStrip;
        ContextMenuStrip _mariaDbConfigContextMenuStrip, _mariaDbLogContextMenuStrip;
        ContextMenuStrip _phpConfigContextMenuStrip, _phpLogContextMenuStrip;
        private NotifyIcon ni = new NotifyIcon();
        private bool _visiblecore = true;

        private void SetupNginx()
        {
            _nginx = new NginxProgram(Program.StartupPath + "\\nginx.exe")
            {
                ProgLogSection = Log.LogSection.Nginx,
                StartArgs = "",
                StopArgs = "-s stop",
                ConfDir = Program.StartupPath + "\\conf\\",
                LogDir = Program.StartupPath + "\\logs\\"
            };
        }

        private void SetupMariaDB()
        {
            _mariaDb = new MariaDBProgram(Program.StartupPath + "\\mariadb\\bin\\mysqld.exe")
            {
                ProgLogSection = Log.LogSection.MariaDB,
                StartArgs = "--install-manual Wnmp-MariaDB",
                StopArgs = "/c sc delete Wnmp-MariaDB",
                ConfDir = Program.StartupPath + "\\mariadb\\",
                LogDir = Program.StartupPath + "\\mariadb\\data\\"
            };
        }

        private void SetCurlCAPath()
        {
            string phpini = Program.StartupPath + "/php/" + Properties.Settings.Default.PHPVersion + "php.ini";
            if (!File.Exists(phpini))
                return;

            string[] file = File.ReadAllLines(phpini);
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].Contains("curl.cainfo") == false)
                    continue;

                Regex reg = new Regex("(curl\\.cainfo).*?(=)");
                string orginal = reg.Match(file[i]).ToString();
                if (orginal == String.Empty)
                    continue;
                string replace = "curl.cainfo = " + "\"" + Program.StartupPath + @"\contrib\cacert.pem" + "\"";

                file[i] = replace;
            }

            using (var sw = new StreamWriter(phpini))
            {
                foreach (var line in file)
                    sw.WriteLine(line);
            }
        }

        /// <summary>
        /// Adds configuration files or log files to a context menu strip
        /// </summary>
        private void DirFiles(string path, string GetFiles, ContextMenuStrip cms)
        {
            var dInfo = new DirectoryInfo(path);

            if (!dInfo.Exists)
                return;

            var files = dInfo.GetFiles(GetFiles);
            foreach (var file in files)
            {
                cms.Items.Add(file.Name);
            }
        }

        private void SetupConfigAndLogMenuStrips()
        {
            _nginxConfigContextMenuStrip = new ContextMenuStrip();
            _nginxConfigContextMenuStrip.ItemClicked += (s, e) =>
            {
                Misc.OpenFileEditor(_nginx.ConfDir + e.ClickedItem.ToString());
            };
            _nginxLogContextMenuStrip = new ContextMenuStrip();
            _nginxLogContextMenuStrip.ItemClicked += (s, e) =>
            {
                Misc.OpenFileEditor(_nginx.LogDir + e.ClickedItem.ToString());
            };
            _mariaDbConfigContextMenuStrip = new ContextMenuStrip();
            _mariaDbConfigContextMenuStrip.ItemClicked += (s, e) =>
            {
                Misc.OpenFileEditor(_mariaDb.ConfDir + e.ClickedItem.ToString());
            };
            _mariaDbLogContextMenuStrip = new ContextMenuStrip();
            _mariaDbLogContextMenuStrip.ItemClicked += (s, e) =>
            {
                Misc.OpenFileEditor(_mariaDb.LogDir + e.ClickedItem.ToString());
            };
            _phpConfigContextMenuStrip = new ContextMenuStrip();
            _phpConfigContextMenuStrip.ItemClicked += (s, e) =>
            {
                Misc.OpenFileEditor(_php.ConfDir + e.ClickedItem.ToString());
            };
            _phpLogContextMenuStrip = new ContextMenuStrip();
            _phpLogContextMenuStrip.ItemClicked += (s, e) =>
            {
                Misc.OpenFileEditor(_php.LogDir + e.ClickedItem.ToString());
            };
            DirFiles(_nginx.ConfDir, "*.conf", _nginxConfigContextMenuStrip);
            DirFiles(_mariaDb.ConfDir, "my.ini", _mariaDbConfigContextMenuStrip);
            DirFiles(_php.ConfDir, "php.ini", _phpConfigContextMenuStrip);
            DirFiles(_nginx.LogDir, "*.log", _nginxLogContextMenuStrip);
            DirFiles(_mariaDb.LogDir, "*.err", _mariaDbLogContextMenuStrip);
            DirFiles(_php.LogDir, "*.log", _phpLogContextMenuStrip);
        }

        public void SetupPhp()
        {
            string phpVersion = Properties.Settings.Default.PHPVersion;
            _php = new PHPProgram(Program.StartupPath + "\\php\\" + phpVersion + "\\php-cgi.exe")
            {
                ProgLogSection = Log.LogSection.PHP,
                ConfDir = Program.StartupPath + "\\php\\" + phpVersion + "\\",
                LogDir = Program.StartupPath + "\\php\\" + phpVersion + "\\logs\\",
            };
        }

        private void CreateWnmpCertificate()
        {
            string ConfDir = Program.StartupPath + "\\conf";

            if (!Directory.Exists(ConfDir))
                Directory.CreateDirectory(ConfDir);

            string keyFile = ConfDir + "\\key.pem";
            string certFile = ConfDir + "\\cert.pem";

            if (File.Exists(keyFile) && File.Exists(certFile))
                return;

            _nginx.GenerateSslKeyPair();
        }

        private MenuItem CreateWnmpProgramMenuItem(WnmpProgram prog)
        {
            MenuItem item = new MenuItem {Text = Log.LogSectionToString(prog.ProgLogSection)};

            MenuItem start = item.MenuItems.Add("Start");
            start.Click += (s, e) => { prog.Start(); };
            MenuItem stop = item.MenuItems.Add("Stop");
            stop.Click += (s, e) => { prog.Stop(); };
            MenuItem restart = item.MenuItems.Add("Restart");
            restart.Click += (s, e) => { prog.Restart(); };

            return item;
        }

        private void SetupTrayMenu()
        {
            MenuItem controlpanel = new MenuItem("Wnmp Control Panel");
            controlpanel.Click += (s, e) =>
            {
                _visiblecore = true;
                base.SetVisibleCore(true);
                WindowState = FormWindowState.Normal;
                Show();
            };
            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add(controlpanel);
            cm.MenuItems.Add("-");
            cm.MenuItems.Add(CreateWnmpProgramMenuItem(_nginx));
            cm.MenuItems.Add(CreateWnmpProgramMenuItem(_mariaDb));
            cm.MenuItems.Add(CreateWnmpProgramMenuItem(_php));
            cm.MenuItems.Add("-");
            MenuItem exit = new MenuItem("Exit");
            exit.Click += (s, e) => { Application.Exit(); };
            cm.MenuItems.Add(exit);
            cm.MenuItems.Add("-");
            ni.ContextMenu = cm;
            ni.Icon = Properties.Resources.logo;
            ni.Click += (s, e) =>
            {
                _visiblecore = true;
                base.SetVisibleCore(true);
                WindowState = FormWindowState.Normal;
                Show();
            };
            ni.Visible = true;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_visiblecore == false)
            {
                value = false;
                if (!IsHandleCreated)
                    CreateHandle();
            }

            base.SetVisibleCore(value);
        }

        public MainFrm()
        {
            if (Properties.Settings.Default.StartMinimizedToTray)
            {
                Visible = false;
                Hide();
            }

            InitializeComponent();
            Log.SetLogComponent(logRichTextBox);
            Log.Notice("Initializing Control Panel");
            Log.Notice("Wnmp Version: " + Application.ProductVersion);
            Log.Notice("Wnmp Directory: " + Program.StartupPath);
            SetupNginx();
            SetupMariaDB();
            SetupPhp();
            SetupConfigAndLogMenuStrips();
            SetupTrayMenu();
            CreateWnmpCertificate();

            if (Properties.Settings.Default.StartMinimizedToTray)
            {
                _visiblecore = false;
                base.SetVisibleCore(false);
            }

            if (Properties.Settings.Default.StartNginxOnLaunch)
            {
                _nginx.Start();
            }

            if (Properties.Settings.Default.StartMariaDBOnLaunch)
            {
                _mariaDb.Start();
            }

            if (Properties.Settings.Default.StartPHPOnLaunch)
            {
                _php.Start();
            }
        }

        /* Menu */

        /* File */

        private void WnmpOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var optionForm = new OptionsFrm(this);
            optionForm.ShowDialog(this);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /* Applications Group Box */

        private void CtxButton(object sender, ContextMenuStrip contextMenuStrip)
        {
            var btnSender = (Button) sender;
            var ptLowerLeft = new Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            contextMenuStrip.Show(ptLowerLeft);
        }

        private void NginxStartButton_Click(object sender, EventArgs e)
        {
            _nginx.Start();
        }

        private void MariadbStartButton_Click(object sender, EventArgs e)
        {
            _mariaDb.Start();
        }

        private void PhpStartButton_Click(object sender, EventArgs e)
        {
            _php.Start();
        }

        private void NginxStopButton_Click(object sender, EventArgs e)
        {
            _nginx.Stop();
        }

        private void MariadbStopButton_Click(object sender, EventArgs e)
        {
            _mariaDb.Stop();
        }

        private void PhpStopButton_Click(object sender, EventArgs e)
        {
            _php.Stop();
        }

        private void NginxRestartButton_Click(object sender, EventArgs e)
        {
            _nginx.Restart();
        }

        private void MariadbRestartButton_Click(object sender, EventArgs e)
        {
            _mariaDb.Restart();
        }

        private void PhpRestartButton_Click(object sender, EventArgs e)
        {
            _php.Restart();
        }

        private void NginxConfigButton_Click(object sender, EventArgs e)
        {
            CtxButton(sender, _nginxConfigContextMenuStrip);
        }

        private void MariadbConfigButton_Click(object sender, EventArgs e)
        {
            CtxButton(sender, _mariaDbConfigContextMenuStrip);
        }

        private void PhpConfigButton_Click(object sender, EventArgs e)
        {
            CtxButton(sender, _phpConfigContextMenuStrip);
        }

        private void NginxLogButton_Click(object sender, EventArgs e)
        {
            CtxButton(sender, _nginxLogContextMenuStrip);
        }

        private void MariadbLogButton_Click(object sender, EventArgs e)
        {
            CtxButton(sender, _mariaDbLogContextMenuStrip);
        }

        private void PhpLogButton_Click(object sender, EventArgs e)
        {
            CtxButton(sender, _phpLogContextMenuStrip);
        }

        /* */

        public void StopAll()
        {
            _nginx.Stop();
            _mariaDb.Stop();
            _php.Stop();
        }

        private void StartAllButton_Click(object sender, EventArgs e)
        {
            _nginx.Start();
            _mariaDb.Start();
            _php.Start();
        }

        private void StopAllButton_Click(object sender, EventArgs e)
        {
            StopAll();
        }

        private void GetHTTPHeadersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HTTPHeadersFrm httpHeadersFrm = new HTTPHeadersFrm()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            httpHeadersFrm.Show(this);
        }

        private void HostToIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HostToIPFrm hostToIPFrm = new HostToIPFrm()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            hostToIPFrm.Show(this);
        }

        private void WebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Misc.StartProcessAsync("https://ogsteam.fr");
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var aboutFrm = new AboutFrm()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            aboutFrm.ShowDialog(this);
        }

        private void ReportBugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Misc.StartProcessAsync("https://github.com/darknoon29/onmp/issues/new");
        }

        private void SetRunningStatusLabel(Label label, bool running)
        {
            if (running)
            {
                label.Text = @"✓";
                label.ForeColor = Color.Green;
            }
            else
            {
                label.Text = @"X";
                label.ForeColor = Color.DarkRed;
            }
        }

        private void AppsRunningTimer_Tick(object sender, EventArgs e)
        {
            SetRunningStatusLabel(nginxrunning, _nginx.IsRunning());
            SetRunningStatusLabel(phprunning, _php.IsRunning());
            SetRunningStatusLabel(mariadbrunning, _mariaDb.IsRunning());
        }

        private void LocalhostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Misc.StartProcessAsync("http://localhost");
        }

        private void OpenMariaDBShellButton_Click(object sender, EventArgs e)
        {
            _mariaDb.OpenShell();
        }

        private void WnmpDirButton_Click(object sender, EventArgs e)
        {
            Misc.StartProcessAsync("explorer.exe", Program.StartupPath);
        }

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && Properties.Settings.Default.MinimizeInsteadOfClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                Properties.Settings.Default.Save();
            }
        }

        private void MainFrm_Resize(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.MinimizeToTray == false)
                return;

            if (WindowState == FormWindowState.Minimized)
                Hide();
        }
    }
}