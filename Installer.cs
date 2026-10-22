using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: AssemblyTitle("Nebula Gear Studio V5.0 安装程序")]
[assembly: AssemblyDescription("安装宇宙主题 SolidWorks 参数化齿轮建模器")]
[assembly: AssemblyCompany("本地工具")]
[assembly: AssemblyProduct("Nebula Gear Studio V5.0 安装程序")]
[assembly: AssemblyFileVersion("5.0.0.0")]

namespace SolidWorksPrimitiveBuilderInstaller
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }

    internal sealed class InstallerForm : Form
    {
        private readonly TextBox path = new TextBox();
        private readonly CheckBox desktopShortcut = new CheckBox();
        private readonly CheckBox startMenuShortcut = new CheckBox();
        private readonly CheckBox launchAfter = new CheckBox();
        private readonly Button install = new Button();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Label status = new Label();

        internal InstallerForm()
        {
            Text = "安装 Nebula Gear Studio V5.0 · 跨版本适配版";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            ClientSize = new Size(720, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Microsoft YaHei UI", 10F);

            BackColor = Color.FromArgb(7, 11, 29); ForeColor = Color.FromArgb(232, 239, 255);
            PictureBox picture = new PictureBox { Location = new Point(28, 25), Size = new Size(170, 170), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(12,18,40) };
            try { using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.Hero")) using(Image source=Image.FromStream(s)) picture.Image = new Bitmap(source); } catch { }
            Controls.Add(picture);
            Controls.Add(new Label { Text = "Nebula Gear Studio V5.0", ForeColor=Color.White, BackColor=Color.Transparent, Font = new Font("Comic Sans MS", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(220, 32) });
            var zh = new Font("Microsoft YaHei UI", 10F);
            var en = new Font("Comic Sans MS", 10F);
            Controls.Add(new Label { Text = "基础实体 · 直齿轮 · 斜齿轮 · 独立蜗杆 · 独立蜗轮", ForeColor = Color.FromArgb(125,207,255), BackColor=Color.Transparent, Font = zh, AutoSize = true, Location = new Point(223, 82) });
            var line2 = SegmentedLine(Color.FromArgb(160,175,210), new[] { "宇宙动态界面 / ", "SLDPRT / STEP / 3MF" }, new[] { zh, en });
            line2.Location = new Point(223, 111); Controls.Add(line2);
            var line3 = SegmentedLine(Color.FromArgb(190,202,230), new[] { "直接调用 ", "SOLIDWORKS API", "，不依赖 ", "SWP", "，不模拟鼠标。" }, new[] { zh, en, zh, en, zh });
            line3.Location = new Point(223, 148); Controls.Add(line3);
            Controls.Add(new Label { Text = "内置母版为 2024 格式；2022/2023 需另存对应版本母版。", ForeColor=Color.FromArgb(255,203,107), BackColor=Color.Transparent, Font = zh, AutoSize = true, Location=new Point(223,175) });

            Controls.Add(new Label { Text = "安装位置", ForeColor=Color.FromArgb(170,185,218), BackColor=Color.Transparent, AutoSize = true, Location = new Point(29, 219) });
            path.Location = new Point(29, 244); path.Size = new Size(560, 29); path.BackColor=Color.FromArgb(27,37,67);path.ForeColor=Color.White;
            path.Text = Directory.Exists(@"D:\") ? @"D:\SolidWorks多形体建模器" : Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "SolidWorks多形体建模器");
            Controls.Add(path);
            Button browse = new Button { Text = "浏览…", Location = new Point(600, 242), Size = new Size(90, 31), BackColor=Color.FromArgb(38,50,84),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            browse.Click += Browse;
            Controls.Add(browse);

            desktopShortcut.Text = "创建桌面快捷方式"; desktopShortcut.Checked = true; desktopShortcut.AutoSize = true; desktopShortcut.Location = new Point(30, 294);
            startMenuShortcut.Text = "创建开始菜单快捷方式"; startMenuShortcut.Checked = true; startMenuShortcut.AutoSize = true; startMenuShortcut.Location = new Point(245, 294);
            launchAfter.Text = "安装完成后启动"; launchAfter.Checked = true; launchAfter.AutoSize = true; launchAfter.Location = new Point(489, 294);
            foreach(CheckBox c in new[]{desktopShortcut,startMenuShortcut,launchAfter}){c.ForeColor=Color.FromArgb(220,230,250);c.BackColor=Color.Transparent;}
            Controls.Add(desktopShortcut); Controls.Add(startMenuShortcut); Controls.Add(launchAfter);

            progress.Location = new Point(29, 340); progress.Size = new Size(661, 17); progress.Minimum = 0; progress.Maximum = 4;
            Controls.Add(progress);
            status.Text = "准备安装"; status.ForeColor = Color.FromArgb(145,165,205); status.AutoEllipsis = true; status.Location = new Point(29, 371); status.Size = new Size(661, 24);
            Controls.Add(status);

            install.Text = "安装到本机"; install.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold); install.Location = new Point(480, 422); install.Size = new Size(210, 45); install.BackColor=Color.FromArgb(92,83,238);install.ForeColor=Color.White;install.FlatStyle=FlatStyle.Flat;
            install.Click += InstallClicked;
            Controls.Add(install);
            Button cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(359, 422), Size = new Size(110, 45), BackColor=Color.FromArgb(38,50,84),ForeColor=Color.White,FlatStyle=FlatStyle.Flat };
            Controls.Add(cancel); CancelButton = cancel;
        }

        private static FlowLayoutPanel SegmentedLine(Color color, string[] texts, Font[] fonts)
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            for (int i = 0; i < texts.Length; i++)
            {
                panel.Controls.Add(new Label
                {
                    Text = texts[i],
                    Font = fonts[i],
                    ForeColor = color,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Margin = new Padding(0)
                });
            }
            return panel;
        }

        private void Browse(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "选择安装目录", SelectedPath = path.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = dialog.SelectedPath;
        }

        private void InstallClicked(object sender, EventArgs e)
        {
            string dir = path.Text.Trim();
            if (dir.Length == 0) { MessageBox.Show(this, "请选择安装位置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                install.Enabled = false;
                Directory.CreateDirectory(dir);
                string appPath = Path.Combine(dir, "NebulaGearStudioV5.exe");
                status.Text = "正在安装主程序…"; progress.Value = 1; Application.DoEvents();
                Extract("Payload.AppExe", appPath);
                status.Text = "正在安装图标和使用说明…"; progress.Value = 2; Application.DoEvents();
                Extract("Payload.Hero", Path.Combine(dir, "girl-greenhouse.png"));
                Extract("Payload.IconIco", Path.Combine(dir, "girl-greenhouse-rounded.ico"));
                Extract("Payload.SpurGearMaster", Path.Combine(dir, "圆柱直齿轮方程母版.SLDPRT"));
                Extract("Payload.HelicalGearMaster", Path.Combine(dir, "圆柱斜齿轮左旋方程母版.SLDPRT"));
                string library = Path.Combine(dir, "母版库"); Directory.CreateDirectory(library);
                Directory.CreateDirectory(Path.Combine(library,"SW2022"));
                Directory.CreateDirectory(Path.Combine(library,"SW2023"));
                Extract("Payload.Polyhedron", Path.Combine(library, "正n面体.PRTDOT"));
                string[] libraryNames={"圆柱直齿轮.SLDPRT","圆柱斜齿轮右旋.SLDPRT","圆柱斜齿轮左旋.SLDPRT","直齿圈.SLDPRT","斜齿圈右旋.SLDPRT","斜齿圈左旋.SLDPRT","锥齿轮.SLDPRT","涡杆.SLDPRT","涡轮右旋.SLDPRT","涡轮左旋.SLDPRT","压缩弹簧.SLDPRT","拉伸弹簧.SLDPRT","扭转弹簧.SLDPRT","截锥弹簧无紧固圈.SLDPRT","截锥弹簧有紧固圈.SLDPRT"};
                for(int i=0;i<libraryNames.Length;i++) Extract("Payload.Library"+i,Path.Combine(library,libraryNames[i]));
                Extract("Payload.Readme", Path.Combine(dir, "使用说明.txt"));

                status.Text = "正在创建快捷方式…"; progress.Value = 3; Application.DoEvents();
                if (desktopShortcut.Checked)
                    CreateShortcut(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "SolidWorks多形体建模器（D盘安装版）.lnk"), appPath);
                if (startMenuShortcut.Checked)
                {
                    string menu = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Programs), "SolidWorks多形体建模器");
                    Directory.CreateDirectory(menu);
                    CreateShortcut(Path.Combine(menu, "Nebula Gear Studio V3.lnk"), appPath);
                }

                progress.Value = 4; status.Text = "安装完成：" + appPath;
                MessageBox.Show(this, "安装完成。\r\n\r\n主程序：\r\n" + appPath + "\r\n\r\n温室少女原图已封装进主界面。", "安装完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (launchAfter.Checked) System.Diagnostics.Process.Start(appPath);
                Close();
            }
            catch (Exception ex)
            {
                install.Enabled = true; status.Text = "安装失败";
                MessageBox.Show(this, ex.Message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Extract(string resource, string target)
        {
            using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (input == null) throw new InvalidOperationException("安装资源缺失：" + resource);
                using (FileStream output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None)) input.CopyTo(output);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath)
        {
            Type type = Type.GetTypeFromProgID("WScript.Shell");
            if (type == null) throw new InvalidOperationException("无法创建 Windows 快捷方式。");
            object shellObject = Activator.CreateInstance(type);
            try
            {
                dynamic shell = shellObject;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.IconLocation = Path.Combine(Path.GetDirectoryName(targetPath),"girl-greenhouse-rounded.ico") + ",0";
                shortcut.Description = "Nebula Gear Studio V5.0 · 跨版本适配";
                shortcut.Save();
                Marshal.FinalReleaseComObject(shortcut);
            }
            finally { Marshal.FinalReleaseComObject(shellObject); }
        }
    }
}

