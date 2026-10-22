// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SolidWorksPrimitiveBuilderV2
{
    internal static class UiRounder
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
        internal static void PreferRoundedWindow(Form form)
        {
            form.HandleCreated+=delegate{try{int preference=2;DwmSetWindowAttribute(form.Handle,33,ref preference,4);}catch{}};
        }
        internal static void Apply(Control control,int radius)
        {
            EventHandler update=null;
            update=delegate
            {
                if(control.Width<2||control.Height<2)return;
                int d=Math.Min(radius*2,Math.Min(control.Width,control.Height));
                using(var path=new GraphicsPath())
                {
                    path.AddArc(0,0,d,d,180,90);path.AddArc(control.Width-d-1,0,d,d,270,90);
                    path.AddArc(control.Width-d-1,control.Height-d-1,d,d,0,90);path.AddArc(0,control.Height-d-1,d,d,90,90);path.CloseFigure();
                    Region old=control.Region;control.Region=new Region(path);if(old!=null)old.Dispose();
                }
            };
            control.HandleCreated+=update;control.SizeChanged+=update;if(control.IsHandleCreated)update(control,EventArgs.Empty);
        }
    }

    internal sealed class FloatingToast : Form
    {
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private int frame;

        internal FloatingToast(Form owner, string message)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(310, 62);
            BackColor = Color.FromArgb(23, 31, 59);
            Opacity = 0;
            var text = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(241, 236, 255),
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold)
            };
            Controls.Add(text);
            UiRounder.Apply(this, 16);
            Rectangle bounds = owner.Bounds;
            Location = new Point(bounds.Left + (bounds.Width - Width) / 2, bounds.Top + (bounds.Height - Height) / 2);
            timer.Interval = 40;
            timer.Tick += delegate
            {
                frame++;
                if (frame <= 4) Opacity = Math.Min(1.0, frame * .25);
                else if (frame <= 28) Opacity = 1.0;
                else
                {
                    Opacity = Math.Max(0.0, Opacity - .05);
                    if (Opacity <= 0.0) Close();
                }
            };
            FormClosed += delegate { timer.Stop(); timer.Dispose(); };
            Shown += delegate { timer.Start(); };
            Show(owner);
        }
    }

    internal sealed class HoverGlowLabel : Control
    {
        private string zhText;
        private string enText;
        private Font zhFont;
        private Font enFont;
        private Color zhColor = Color.FromArgb(238, 218, 170);
        private Color enColor = Color.FromArgb(153, 186, 218);
        private readonly Color darkColor = Color.FromArgb(10, 12, 26);
        private readonly Color backColor = Color.FromArgb(11, 17, 37);
        private Point cursor = Point.Empty;
        private bool hovered = false;
        private float radius = 44F;
        private float contentTopInset = 0F;

        internal HoverGlowLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            // 由父卡片统一提供底色，避免文字控件自己形成黑色矩形层。
            BackColor = Color.Transparent;
            Cursor = Cursors.Default;
        }

        internal string ZhText { get { return zhText; } set { zhText = value; Invalidate(); } }
        internal string EnText { get { return enText; } set { enText = value; Invalidate(); } }
        internal Font ZhFont { get { return zhFont; } set { zhFont = value; Invalidate(); } }
        internal Font EnFont { get { return enFont; } set { enFont = value; Invalidate(); } }
        internal Color ZhColor { get { return zhColor; } set { zhColor = value; Invalidate(); } }
        internal Color EnColor { get { return enColor; } set { enColor = value; Invalidate(); } }
        internal float GlowRadius { get { return radius; } set { radius = value; Invalidate(); } }
        // 光圈可在整个控件中移动，文字则可单独向下安放；这样图片与文字之间的
        // 留白仍属于同一个连续交互区域，而不会把文字也拉回图片边缘。
        internal float ContentTopInset { get { return contentTopInset; } set { contentTopInset = Math.Max(0F, value); Invalidate(); } }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            cursor = e.Location; hovered = true; Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false; Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // 行书笔画会超出普通字体度量；明确留出顶部净空并整体下移。
            float contentHeight = Math.Max(1F, Height - contentTopInset);
            float topInset = contentTopInset + Math.Max(12F, contentHeight * .085F);
            float zhH = contentHeight * .57F;
            float enTop = topInset + zhH;
            float enH = Math.Max(1F, Height - enTop - 5F);
            var zhRect = new RectangleF(0, topInset, Width, zhH);
            var enRect = new RectangleF(0, enTop, Width, enH);
            var format = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoClip };

            if (!string.IsNullOrEmpty(zhText) && zhFont != null)
                using (var b = new SolidBrush(zhColor)) e.Graphics.DrawString(zhText, zhFont, b, zhRect, format);
            if (!string.IsNullOrEmpty(enText) && enFont != null)
                using (var b = new SolidBrush(enColor)) e.Graphics.DrawString(enText, enFont, b, enRect, format);

            if (hovered)
            {
                var circle = new RectangleF(cursor.X - radius, cursor.Y - radius, radius * 2F, radius * 2F);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(circle);
                    var state = e.Graphics.Save();
                    e.Graphics.SetClip(path);
                    using (var fill = new SolidBrush(Color.FromArgb(190, 242, 244, 250)))
                        e.Graphics.FillEllipse(fill, circle);
                    if (!string.IsNullOrEmpty(zhText) && zhFont != null)
                        using (var b = new SolidBrush(darkColor)) e.Graphics.DrawString(zhText, zhFont, b, zhRect, format);
                    if (!string.IsNullOrEmpty(enText) && enFont != null)
                        using (var b = new SolidBrush(darkColor)) e.Graphics.DrawString(enText, enFont, b, enRect, format);
                    e.Graphics.Restore(state);
                }
                using (var ring = new Pen(Color.FromArgb(240, 250, 252, 255), 2F))
                    e.Graphics.DrawEllipse(ring, circle);
            }
        }
    }

    internal sealed class RoundedImageBox : Control
    {
        private Image image;
        private readonly System.Windows.Forms.Timer motionTimer;
        private float currentX, currentY, targetX, targetY, maximumShift = 4F;
        internal Image DisplayImage
        {
            get { return image; }
            set { if (!ReferenceEquals(image, value)) { if (image != null) image.Dispose(); image = value; Invalidate(); } }
        }
        internal int CornerRadius { get; set; }

        internal RoundedImageBox()
        {
            CornerRadius = 18;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            motionTimer = new System.Windows.Forms.Timer { Interval = 16 };
            motionTimer.Tick += AnimateMotion;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { using (Graphics g = CreateGraphics()) maximumShift = Math.Max(3F, g.DpiX / 25.4F); }
            catch { maximumShift = 4F; }
        }

        private Rectangle GetImageViewport()
        {
            if (image == null || Width < 2 || Height < 2) return Rectangle.Empty;
            float scale = Math.Min((float)Width / image.Width, (float)Height / image.Height);
            int width = Math.Max(1, (int)Math.Round(image.Width * scale));
            int height = Math.Max(1, (int)Math.Round(image.Height * scale));
            return new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Rectangle viewport = GetImageViewport();
            if (!viewport.Contains(e.Location)) { SetMotionTarget(0F, 0F); return; }
            float horizontal = (e.X - (viewport.Left + viewport.Width / 2F)) / Math.Max(1F, viewport.Width / 2F);
            float vertical = (e.Y - (viewport.Top + viewport.Height / 2F)) / Math.Max(1F, viewport.Height / 2F);
            SetMotionTarget(horizontal * maximumShift, vertical * maximumShift);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            SetMotionTarget(0F, 0F);
        }

        private void SetMotionTarget(float x, float y)
        {
            targetX = x; targetY = y;
            if (!motionTimer.Enabled) motionTimer.Start();
        }

        private void AnimateMotion(object sender, EventArgs e)
        {
            currentX += (targetX - currentX) * .24F;
            currentY += (targetY - currentY) * .24F;
            if (Math.Abs(targetX - currentX) < .04F && Math.Abs(targetY - currentY) < .04F)
            {
                currentX = targetX; currentY = targetY; motionTimer.Stop();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (image == null || Width < 2 || Height < 2) return;

            Rectangle viewport = GetImageViewport();
            int overscan = (int)Math.Ceiling(maximumShift) + 2;
            float drawScale = Math.Max((float)(viewport.Width + overscan * 2) / image.Width, (float)(viewport.Height + overscan * 2) / image.Height);
            int drawWidth = Math.Max(1, (int)Math.Ceiling(image.Width * drawScale));
            int drawHeight = Math.Max(1, (int)Math.Ceiling(image.Height * drawScale));
            var bounds = new Rectangle(
                (int)Math.Round(viewport.Left + viewport.Width / 2F - drawWidth / 2F + currentX),
                (int)Math.Round(viewport.Top + viewport.Height / 2F - drawHeight / 2F + currentY),
                drawWidth, drawHeight);
            int diameter = Math.Min(CornerRadius * 2, Math.Min(viewport.Width, viewport.Height));

            using (var clip = new GraphicsPath())
            {
                clip.AddArc(viewport.Left, viewport.Top, diameter, diameter, 180, 90);
                clip.AddArc(viewport.Right - diameter, viewport.Top, diameter, diameter, 270, 90);
                clip.AddArc(viewport.Right - diameter, viewport.Bottom - diameter, diameter, diameter, 0, 90);
                clip.AddArc(viewport.Left, viewport.Bottom - diameter, diameter, diameter, 90, 90);
                clip.CloseFigure();
                var state = e.Graphics.Save();
                e.Graphics.SetClip(clip);
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.DrawImage(image, bounds);
                e.Graphics.Restore(state);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                motionTimer.Stop(); motionTimer.Dispose();
                if (image != null) { image.Dispose(); image = null; }
            }
            base.Dispose(disposing);
        }
    }

    internal abstract class NebulaFieldBase : Control
    {
        private bool hovered;
        private Rectangle restingBounds;
        protected readonly Color FieldColor = Color.FromArgb(27, 37, 68);

        protected NebulaFieldBase()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            // 输入值、单位、路径和英文缩写统一采用 Comic Sans MS；中文由系统回退字体绘制。
            Font = new Font("Comic Sans MS", 10F);
            Height = 32;
            TabStop = true;
            MouseEnter += delegate { SetHovered(true); };
            MouseLeave += delegate { RefreshHover(); };
        }

        protected bool IsHighlighted { get { return hovered; } }

        protected void SetHovered(bool value)
        {
            if (hovered == value) return;
            if (value)
            {
                restingBounds = Bounds;
                Bounds = new Rectangle(restingBounds.X - 2, restingBounds.Y - 2, restingBounds.Width + 4, restingBounds.Height + 4);
            }
            else if (restingBounds.Width > 0) Bounds = restingBounds;
            hovered = value;
            Invalidate();
        }

        protected void RefreshHover()
        {
            Point cursor = PointToClient(Cursor.Position);
            SetHovered(ClientRectangle.Contains(cursor));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(1, 1, Width - 3, Height - 3);
            int radius = 8, diameter = radius * 2;
            using (var path = new GraphicsPath())
            {
                path.AddArc(box.Left, box.Top, diameter, diameter, 180, 90);
                path.AddArc(box.Right - diameter, box.Top, diameter, diameter, 270, 90);
                path.AddArc(box.Right - diameter, box.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(box.Left, box.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                using (var fill = new SolidBrush(FieldColor)) e.Graphics.FillPath(fill, path);
                Color border = IsHighlighted ? Color.FromArgb(174, 116, 255) : Color.FromArgb(58, 73, 119);
                using (var pen = new Pen(border, IsHighlighted ? 2.2F : 1F)) e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    }

    internal sealed class NebulaTextField : NebulaFieldBase
    {
        private readonly TextBox editor = new TextBox();

        internal NebulaTextField()
        {
            editor.BorderStyle = BorderStyle.None;
            editor.BackColor = FieldColor;
            editor.ForeColor = Color.White;
            editor.Font = Font;
            editor.Location = new Point(13, 7);
            editor.MouseEnter += delegate { SetHovered(true); };
            editor.MouseLeave += delegate { RefreshHover(); };
            editor.GotFocus += delegate { Invalidate(); };
            editor.LostFocus += delegate { Invalidate(); };
            editor.TextChanged += delegate { base.Text = editor.Text; };
            Controls.Add(editor);
            Cursor = Cursors.IBeam;
            Click += delegate { editor.Focus(); };
        }

        public override string Text
        {
            get { return editor == null ? base.Text : editor.Text; }
            set { base.Text = value; if (editor != null && editor.Text != value) editor.Text = value ?? String.Empty; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (editor == null) return;
            editor.Location = new Point(13, Math.Max(5, (Height - editor.PreferredHeight) / 2));
            editor.Width = Math.Max(10, Width - 26);
        }
    }

    internal sealed class NebulaSelector : NebulaFieldBase
    {
        private readonly List<object> items = new List<object>();
        private int selectedIndex = -1;
        internal event EventHandler SelectedIndexChanged;

        internal void AddItems(object[] values) { items.AddRange(values); Invalidate(); }
        internal void SetItems(object[] values)
        {
            items.Clear();
            if (values != null) items.AddRange(values);
            selectedIndex = -1;
            SelectedIndex = items.Count == 0 ? -1 : 0;
            Invalidate();
        }
        internal int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (value < -1 || value >= items.Count || selectedIndex == value) return;
                selectedIndex = value; Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        internal NebulaSelector()
        {
            Cursor = Cursors.Hand;
            Click += delegate { ShowChoices(); };
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { ShowChoices(); e.Handled = true; }
                else if (e.KeyCode == Keys.Down && selectedIndex < items.Count - 1) SelectedIndex++;
                else if (e.KeyCode == Keys.Up && selectedIndex > 0) SelectedIndex--;
            };
        }

        private void ShowChoices()
        {
            if (!Enabled || items.Count == 0) return;
            Focus();
            var menu = new ContextMenuStrip { ShowImageMargin = false, BackColor = Color.FromArgb(20, 28, 54), ForeColor = Color.White, Font = Font };
            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                var option = new ToolStripMenuItem(items[i].ToString()) { Checked = i == selectedIndex, AutoSize = false, Width = Math.Max(Width - 4, 150), Height = 30 };
                // ToolStripMenuItem 会在点击后自行关闭所属菜单；此处不再在关闭
                // 回调里同步 Dispose。WinForms 仍在派发 Click / Closed 消息时销毁
                // ContextMenuStrip，会导致“无法访问已释放的对象”的未处理异常。
                option.Click += delegate { SelectedIndex = index; };
                menu.Items.Add(option);
            }
            // 交由控件容器在窗体释放时统一清理。菜单对象很小，而这避免了关闭消息
            // 尚未分发完成时访问已释放对象的问题。
            menu.Closed += delegate { Invalidate(); };
            menu.Show(this, new Point(0, Height + 2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            string value = selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex].ToString() : String.Empty;
            Color textColor = Enabled ? ForeColor : Color.FromArgb(115, 127, 157);
            TextRenderer.DrawText(e.Graphics, value, Font, new Rectangle(13, 0, Width - 47, Height), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            Point center = new Point(Width - 19, Height / 2 + 1);
            using (var brush = new SolidBrush(IsHighlighted ? Color.FromArgb(201, 166, 255) : Color.FromArgb(175, 190, 224)))
            {
                e.Graphics.FillPolygon(brush, new[] { new Point(center.X - 5, center.Y - 3), new Point(center.X + 5, center.Y - 3), new Point(center.X, center.Y + 3) });
            }
        }
    }

    internal sealed class CosmicMainForm : Form
    {
        private static readonly Color Ink = Color.FromArgb(232, 239, 255);
        private static readonly Color Muted = Color.FromArgb(154, 169, 203);
        private static readonly Color Card = Color.FromArgb(225, 13, 20, 43);
        private readonly NebulaSelector shape = new NebulaSelector(), unit = new NebulaSelector();
        private readonly Label label1 = new Label(), label2 = new Label(), label3 = new Label();
        private readonly NebulaTextField value1 = new NebulaTextField(), value2 = new NebulaTextField(), value3 = new NebulaTextField(), output = new NebulaTextField(), apiKey = new NebulaTextField();
        private readonly CheckBox exportStep = new CheckBox(), export3mf = new CheckBox();
        private readonly Button create = new Button();
        private readonly Label status = new Label(), modeHint = new Label();
        private readonly ShapeKind[] solidWorksKinds = {
            ShapeKind.Box, ShapeKind.Sphere, ShapeKind.Cylinder, ShapeKind.Cone, ShapeKind.Frustum,
            ShapeKind.Washer, ShapeKind.Bushing, ShapeKind.RegularPolyhedron, ShapeKind.SpurGear,
            ShapeKind.HelicalGearRight, ShapeKind.HelicalGearLeft, ShapeKind.SpurRing,
            ShapeKind.HelicalRingRight, ShapeKind.HelicalRingLeft, ShapeKind.BevelGear, ShapeKind.Worm,
            ShapeKind.WormWheelRight, ShapeKind.WormWheelLeft, ShapeKind.CompressionSpring,
            ShapeKind.ExtensionSpring, ShapeKind.TorsionSpring, ShapeKind.ConicalSpringPlain,
            ShapeKind.ConicalSpringClosed
        };
        private readonly ShapeKind[] creoKinds = { ShapeKind.Box, ShapeKind.Sphere, ShapeKind.Frustum, ShapeKind.RegularPolyhedron };
        private ShapeKind[] activeKinds;
        private bool creoMode;
        private Label kernelBadge, subtitle;
        private Button switchKernel;

        internal CosmicMainForm()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Nebula Gear Studio · SolidWorks 建模器 V5.0";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            ClientSize = new Size(1180, 762); MinimumSize = new Size(1050, 714);
            StartPosition = FormStartPosition.CenterScreen; Font = new Font("Microsoft YaHei UI", 10F);
            BackColor = Color.FromArgb(5, 8, 24); DoubleBuffered = true;
            UiRounder.PreferRoundedWindow(this);

            var sky = new SpaceCanvas { Dock = DockStyle.Fill };
            Controls.Add(sky);

            var badge = new Label { Text="✦  NEBULA GEAR STUDIO  ·  V5.0", Font=new Font("Comic Sans MS",10F,FontStyle.Bold), ForeColor=Color.FromArgb(112,222,255), BackColor=Color.Transparent, AutoSize=true };
            badge.Location = new Point(42, 28); sky.Controls.Add(badge);
            var title = MakeLabel("于寂静星海，铸造精密回声", 25F, Color.White, FontStyle.Bold);
            title.Location = new Point(38, 58); sky.Controls.Add(title);
            subtitle = new Label { Text="SOLIDWORKS API 驱动 · 基础实体 / 渐开线齿轮 / 斜齿轮 / 蜗轮蜗杆", Font=new Font("Comic Sans MS",10F), ForeColor=Muted, BackColor=Color.Transparent, AutoSize=true };
            subtitle.Location = new Point(42, 120); sky.Controls.Add(subtitle);
            kernelBadge = new Label { Text="SOLIDWORKS", Font=new Font("Comic Sans MS",20F,FontStyle.Bold), ForeColor=Color.FromArgb(167,132,255), BackColor=Color.Transparent, AutoSize=true, Visible=false, Anchor=AnchorStyles.Top|AnchorStyles.Right };
            sky.Controls.Add(kernelBadge); kernelBadge.BringToFront();

            Panel card = new Panel { Location = new Point(38, 162), Size = new Size(535, 548), BackColor = Card, Anchor = AnchorStyles.Left | AnchorStyles.Top };
            UiRounder.Apply(card,18);
            Controls.Add(card); card.BringToFront();
            var consoleTitle = MakeLabel("建模控制台", 16F, Ink, FontStyle.Bold, new Point(28, 18));
            consoleTitle.AutoSize = false; consoleTitle.Size = new Size(476, 39); card.Controls.Add(consoleTitle);
            var consoleHint = MakeLabel("选择一个几何任务，参数会在创建前进行校验", 9F, Muted, FontStyle.Regular, new Point(30, 64));
            consoleHint.AutoSize = false; consoleHint.Size = new Size(476, 25); card.Controls.Add(consoleHint);

            var modeCaption = AddCaption(card, "建模模式", 28, 96);
            activeKinds = solidWorksKinds;
            ConfigureCombo(shape, MasterPartCatalog.ShapeNames, new Point(28, 121), 302);
            shape.SelectedIndexChanged += delegate { UpdateFields(); };
            card.Controls.Add(shape);
            ConfigureCombo(unit, new object[] { "毫米 mm", "厘米 cm", "米 m" }, new Point(344, 121), 160);
            card.Controls.Add(unit);

            ConfigureField(card, label1, value1, 28, 174);
            ConfigureField(card, label2, value2, 28, 222);
            ConfigureField(card, label3, value3, 28, 270);
            value1.Text = value2.Text = value3.Text = "100";

            modeHint.Location = new Point(28, 315); modeHint.Size = new Size(476, 42); modeHint.ForeColor = Color.FromArgb(126, 206, 255); modeHint.BackColor = Color.Transparent;
            // 提示中包含 mm / cm / m，统一让拉丁字符使用 Comic Sans MS；中文由系统字体回退。
            modeHint.Font = new Font("Comic Sans MS", 10F);
            card.Controls.Add(modeHint);

            var outputCaption = AddCaption(card, "输出目录", 28, 363);
            StyleTextBox(output, new Point(28, 388), 390);
            output.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SolidWorks零件");
            card.Controls.Add(output);
            var browse = MakeButton("浏览", new Point(428, 386), new Size(76, 31), Color.FromArgb(38, 50, 84));
            browse.Click += Browse; card.Controls.Add(browse);

            StyleCheck(exportStep, "STEP", new Point(31, 435), true);
            StyleCheck(export3mf, "3MF", new Point(142, 435), true);
            card.Controls.Add(exportStep); card.Controls.Add(export3mf);
            var exportHint = MakeLabel("同时输出", 9F, Muted, FontStyle.Regular, new Point(253, 438)); card.Controls.Add(exportHint);

            create.Text = "启 动 建 模"; create.Location = new Point(28, 474); create.Size = new Size(476, 43);
            create.FlatStyle = FlatStyle.Flat; create.FlatAppearance.BorderSize = 1; create.FlatAppearance.BorderColor = Color.FromArgb(128, 117, 255); create.BackColor = Color.FromArgb(92, 83, 238); create.ForeColor = Color.White;
            create.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold); create.Cursor = Cursors.Hand; create.Click += CreateClicked;
            UiRounder.Apply(create,11);
            WireGlow(create,Color.FromArgb(92,83,238),Color.FromArgb(117,91,255),Color.FromArgb(128,117,255),false);
            card.Controls.Add(create);

            status.Text = "● 系统就绪"; status.Location = new Point(28, 516); status.Size = new Size(476, 28); status.ForeColor = Color.FromArgb(103, 229, 190); status.BackColor = Color.Transparent; status.AutoEllipsis = true;
            card.Controls.Add(status);

            // 先只预留大模型密钥入口：不落盘、不校验，也不发起任何网络请求。
            // 仅在展开布局中显示，视觉与其他输入框保持一致。
            var apiKeyCaption = AddCaption(card, "密钥接入  Key", 28, 558);
            apiKeyCaption.Font = new Font("Comic Sans MS", 9F);
            StyleTextBox(apiKey, new Point(28, 583), 476);
            apiKey.Text = String.Empty;
            apiKeyCaption.Visible = apiKey.Visible = false;
            card.Controls.Add(apiKey);
            switchKernel = MakeButton("转入 CREO", new Point(446, 581), new Size(130, 36), Color.FromArgb(38, 50, 84));
            switchKernel.Font = new Font("Comic Sans MS", 9.5F, FontStyle.Bold);
            switchKernel.Visible = false;
            switchKernel.Click += delegate
            {
                if (creoMode) { SetCreoMode(false); return; }
                if (String.IsNullOrWhiteSpace(apiKey.Text))
                {
                    ShowFloatingToast("请接入正确的key");
                    return;
                }
                SetCreoMode(true);
                ShowFloatingToast("请先补装API Toolkits");
            };
            card.Controls.Add(switchKernel);

            var photoCard = new Panel { Location = new Point(605, 162), Size = new Size(535, 548), BackColor = Color.FromArgb(220, 11, 17, 37), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiRounder.Apply(photoCard,22);
            Controls.Add(photoCard); photoCard.BringToFront();
            var picture = new RoundedImageBox { Location = new Point(16, 16), Size = new Size(503, 472), CornerRadius = 38, BackColor = Color.FromArgb(8, 12, 28) };
            picture.DisplayImage = LoadEmbeddedImage("Payload.Hero");
            photoCard.Controls.Add(picture);
            var lakeside = new RoundedImageBox { BackColor = Color.FromArgb(8, 12, 28), CornerRadius = 32, Visible = false, DisplayImage = LoadEmbeddedImage("Payload.Lakeside") };
            var rainy = new RoundedImageBox { BackColor = Color.FromArgb(8, 12, 28), CornerRadius = 32, Visible = false, DisplayImage = LoadEmbeddedImage("Payload.Rainy") };
            var stairwell = new RoundedImageBox { BackColor = Color.FromArgb(8, 12, 28), CornerRadius = 32, Visible = false, DisplayImage = LoadEmbeddedImage("Payload.Stairwell") };
            var gallery = new[] { picture, lakeside, rainy, stairwell };
            photoCard.Controls.Add(lakeside); photoCard.Controls.Add(rainy); photoCard.Controls.Add(stairwell);
            var photoTitle = new Label { Text = "GREENHOUSE / ORBIT 07", Font = new Font("Comic Sans MS", 11F, FontStyle.Bold), ForeColor = Ink, BackColor = Color.Transparent, AutoSize = true, Location = new Point(18, 501) };
            photoTitle.Anchor = AnchorStyles.Left | AnchorStyles.Bottom; photoCard.Controls.Add(photoTitle);
            var galleryNote = new HoverGlowLabel { ZhText = "建模靠参数，作者靠艺术。", ZhFont = new Font("STXingkai", 42F, FontStyle.Bold), ZhColor = Color.FromArgb(238, 218, 170), EnText = "Modeling runs on parameters. The author runs on art.", EnFont = new Font("Comic Sans MS", 16F, FontStyle.Regular), EnColor = Color.FromArgb(153, 186, 218), GlowRadius = 54F, Visible = false };
            photoCard.Controls.Add(galleryNote);

            EventHandler responsiveLayout = delegate
            {
                // 最大化/还原会连续触发大量 Resize；将一批 Bounds 更新合并，避免中间帧
                // 反复布局、透明重绘而造成卡顿或短暂的字符残影。
                SuspendLayout(); card.SuspendLayout(); photoCard.SuspendLayout();
                try
                {
                bool expanded = WindowState == FormWindowState.Maximized || ClientSize.Width >= 1500;
                if (expanded)
                {
                    int consoleHeight = Math.Min(1060, Math.Max(720, ClientSize.Height - 260));
                    card.Size = new Size(640, consoleHeight);
                    card.Location = new Point(42, Math.Max(175, Math.Min(230, ClientSize.Height - consoleHeight - 80)));
                    Func<float, int> y = delegate(float ratio) { return (int)Math.Round(consoleHeight * ratio); };
                    consoleTitle.Location = new Point(38, y(.030F)); consoleTitle.Size = new Size(564, 42);
                    consoleHint.Location = new Point(40, y(.088F)); consoleHint.Size = new Size(562, 26);
                    modeCaption.Location = new Point(38, y(.135F));
                    shape.Location = new Point(38, y(.170F)); shape.Width = 362;
                    unit.Location = new Point(420, y(.170F)); unit.Width = 182;
                    label1.Location = new Point(38, y(.260F) + 6); value1.Location = new Point(226, y(.260F)); value1.Width = 376;
                    label2.Location = new Point(38, y(.365F) + 6); value2.Location = new Point(226, y(.365F)); value2.Width = 376;
                    label3.Location = new Point(38, y(.470F) + 6); value3.Location = new Point(226, y(.470F)); value3.Width = 376;
                    modeHint.Location = new Point(38, y(.555F)); modeHint.Size = new Size(564, 42);
                    outputCaption.Location = new Point(38, y(.635F));
                    output.Location = new Point(38, y(.675F)); output.Width = 420;
                    browse.Location = new Point(476, y(.675F) - 2); browse.Size = new Size(126, 38);
                    exportStep.Location = new Point(41, y(.745F)); export3mf.Location = new Point(166, y(.745F)); exportHint.Location = new Point(290, y(.745F) + 3);
                    create.Location = new Point(38, y(.800F)); create.Size = new Size(564, 48);
                    status.Location = new Point(38, y(.865F)); status.Size = new Size(564, 28);
                    apiKeyCaption.Location = new Point(38, y(.905F));
                    apiKey.Location = new Point(38, y(.940F)); apiKey.Width = 392;
                    switchKernel.Location = new Point(448, y(.938F) - 2); switchKernel.Size = new Size(154, 38);
                    apiKeyCaption.Visible = apiKey.Visible = switchKernel.Visible = true;
                    kernelBadge.Visible = true;
                    kernelBadge.Location = new Point(ClientSize.Width - kernelBadge.PreferredWidth - 46, 38);
                    const int gap = 18;
                    int availableWidth = Math.Max(0, ClientSize.Width - card.Right - 84);
                    int tile = Math.Max(150, Math.Min(390, (availableWidth - gap * 5) / 4));
                    int galleryWidth = tile * 4 + gap * 5;
                    // 图片和双语标题严格分区：标题与图片底边留出额外净空，避免高 DPI 下
                    // 行书字形的上沿被图片控件的黑色背景覆盖。
                    // 在 96 DPI 下再下移约 2 mm（8 px）；高 DPI 时控件会随窗体缩放。
                    int noteTop = gap + tile + 104;
                    int noteHeight = 168;
                    int galleryHeight = noteTop + noteHeight + gap;
                    photoCard.Size = new Size(galleryWidth, galleryHeight);
                    photoCard.Left = card.Right + 42 + Math.Max(0, (availableWidth - galleryWidth) / 2);
                    photoCard.Top = Math.Max(190, (ClientSize.Height - galleryHeight) / 2);
                    for (int i = 0; i < gallery.Length; i++)
                    {
                        gallery[i].Visible = true;
                        gallery[i].Bounds = new Rectangle(gap + i * (tile + gap), gap, tile, tile);
                        gallery[i].CornerRadius = Math.Max(24, tile / 12);
                    }
                    photoTitle.Visible = false;
                    galleryNote.Visible = true;
                    // 中英文合并为一个控件；圆圈可在中英文之间自由游走。
                    // 显式 BringToFront，避免不同 DPI/窗口切换时图片重排改变 Z 顺序。
                    // 光圈区域从图片底边下方约 1 mm 开始，但文字仍保持现有位置。
                    // 因此整段黑色留白、中文和英文是一块连续的交互区域，图片本身不响应。
                    int glowTop = gap + tile + 4;
                    galleryNote.ContentTopInset = noteTop - glowTop;
                    galleryNote.Bounds = new Rectangle(gap, glowTop, galleryWidth - gap * 2, galleryHeight - glowTop - gap);
                    galleryNote.BringToFront();
                }
                else
                {
                    card.Size = new Size(535, 548); card.Location = new Point(38, 162);
                    consoleTitle.Location = new Point(28, 18); consoleTitle.Size = new Size(476, 39);
                    consoleHint.Location = new Point(30, 64); consoleHint.Size = new Size(476, 25);
                    modeCaption.Location = new Point(28, 96);
                    shape.Location = new Point(28, 121); shape.Width = 302;
                    unit.Location = new Point(344, 121); unit.Width = 160;
                    label1.Location = new Point(28, 178); value1.Location = new Point(178, 174); value1.Width = 326;
                    label2.Location = new Point(28, 226); value2.Location = new Point(178, 222); value2.Width = 326;
                    label3.Location = new Point(28, 274); value3.Location = new Point(178, 270); value3.Width = 326;
                    modeHint.Location = new Point(28, 315); modeHint.Size = new Size(476, 42);
                    outputCaption.Location = new Point(28, 363);
                    output.Location = new Point(28, 388); output.Width = 390;
                    browse.Location = new Point(428, 386); browse.Size = new Size(76, 31);
                    exportStep.Location = new Point(31, 435); export3mf.Location = new Point(142, 435); exportHint.Location = new Point(253, 438);
                    create.Location = new Point(28, 474); create.Size = new Size(476, 43);
                    status.Location = new Point(28, 516); status.Size = new Size(476, 28);
                    apiKeyCaption.Visible = apiKey.Visible = switchKernel.Visible = kernelBadge.Visible = false;
                    photoCard.Size = new Size(535, 548);
                    photoCard.Left = ClientSize.Width - photoCard.Width - 40;
                    photoCard.Top = 162;
                    picture.Visible = true;
                    picture.Bounds = new Rectangle(16, 16, 503, 472);
                    picture.CornerRadius = 38;
                    lakeside.Visible = rainy.Visible = stairwell.Visible = false;
                    photoTitle.Visible = true;
                    photoTitle.Location = new Point(18, 501);
                    galleryNote.Visible = false;
                }
                }
                finally { photoCard.ResumeLayout(false); card.ResumeLayout(false); ResumeLayout(false); Invalidate(true); }
            };
            var layoutDebounce = new System.Windows.Forms.Timer { Interval = 90 };
            layoutDebounce.Tick += delegate { layoutDebounce.Stop(); responsiveLayout(this, EventArgs.Empty); };
            Resize += delegate { layoutDebounce.Stop(); layoutDebounce.Start(); };
            ResizeEnd += delegate { layoutDebounce.Stop(); responsiveLayout(this, EventArgs.Empty); };
            SizeChanged += delegate { layoutDebounce.Stop(); layoutDebounce.Start(); };
            responsiveLayout(this, EventArgs.Empty);
            UpdateFields();
        }

        private static Label MakeLabel(string text, float size, Color color, FontStyle style = FontStyle.Regular, Point? location = null)
        {
            return new Label { Text = text, Font = new Font("Microsoft YaHei UI", size, style), ForeColor = color, BackColor = Color.Transparent, AutoSize = true, Location = location ?? Point.Empty };
        }
        private static Image LoadEmbeddedImage(string resourceName)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    if (stream != null) using (Image source = Image.FromStream(stream)) return new Bitmap(source);
            }
            catch { }
            return null;
        }
        private static Label AddCaption(Control parent, string text, int x, int y) { var label = MakeLabel(text, 9F, Muted, FontStyle.Regular, new Point(x, y)); parent.Controls.Add(label); return label; }
        private static void ConfigureCombo(NebulaSelector combo, object[] items, Point p, int width)
        {
            combo.AddItems(items); combo.Location = p; combo.Width = width; combo.SelectedIndex = 0;
        }
        private static void StyleTextBox(NebulaTextField box, Point p, int width)
        {
            box.Location = p; box.Width = width;
        }
        private static void ConfigureField(Control parent, Label label, NebulaTextField box, int x, int y)
        {
            label.Location = new Point(x, y + 4); label.Size = new Size(142, 25); label.ForeColor = Ink; label.BackColor = Color.Transparent;
            label.Font = new Font("Comic Sans MS", 9.5F);
            StyleTextBox(box, new Point(x + 150, y), 326); parent.Controls.Add(label); parent.Controls.Add(box);
        }
        private static Button MakeButton(string text, Point p, Size size, Color color)
        {
            var button=new Button { Text = text, Location = p, Size = size, BackColor = color, ForeColor = Ink, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=Color.FromArgb(76,98,145);UiRounder.Apply(button,9);WireGlow(button,color,Color.FromArgb(104,78,224),Color.FromArgb(76,98,145),true);return button;
        }
        private static void WireGlow(Button button,Color normal,Color hover,Color normalBorder,bool enlarge)
        {
            Rectangle resting=Rectangle.Empty;
            button.MouseEnter+=delegate
            {
                resting=button.Bounds;
                if(enlarge) button.Bounds=new Rectangle(resting.X-2,resting.Y-2,resting.Width+4,resting.Height+4);
                button.BackColor=hover;button.FlatAppearance.BorderSize=2;button.FlatAppearance.BorderColor=Color.FromArgb(195,137,255);
            };
            button.MouseLeave+=delegate
            {
                if(enlarge&&resting.Width>0) button.Bounds=resting;
                button.BackColor=normal;button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=normalBorder;
            };
        }
        private static void StyleCheck(CheckBox box, string text, Point p, bool check)
        {
            box.Text = text; box.Checked = check; box.Location = p; box.AutoSize = true; box.ForeColor = Ink; box.BackColor = Color.Transparent;
            box.Font = new Font("Comic Sans MS", 10F);
        }

        private ShapeKind SelectedShapeKind()
        {
            int index = shape.SelectedIndex;
            if (activeKinds == null || index < 0 || index >= activeKinds.Length) return ShapeKind.Box;
            return activeKinds[index];
        }

        private void ShowFloatingToast(string message)
        {
            new FloatingToast(this, message);
        }

        private void SetCreoMode(bool enabled)
        {
            creoMode = enabled;
            activeKinds = enabled ? creoKinds : solidWorksKinds;
            if (enabled)
            {
                shape.SetItems(new object[] { "长方体", "球体", "圆台", "正 n 面体" });
                kernelBadge.Text = "CREO";
                switchKernel.Text = "转回 SW";
                subtitle.Text = "CREO API 驱动 · 长方体 / 球体 / 圆台 / 正 n 面体";
                export3mf.Text = "CREO PRT"; export3mf.Checked = true; export3mf.Enabled = false;
                create.Enabled = false;
                create.Text = "CREO 建模接口待接入";
                status.ForeColor = Color.FromArgb(255, 203, 107);
                status.Text = "● CREO 前端预览 · 后端待接入";
            }
            else
            {
                shape.SetItems(MasterPartCatalog.ShapeNames);
                kernelBadge.Text = "SOLIDWORKS";
                switchKernel.Text = "转入 CREO";
                subtitle.Text = "SOLIDWORKS API 驱动 · 基础实体 / 渐开线齿轮 / 斜齿轮 / 蜗轮蜗杆";
                export3mf.Text = "3MF"; export3mf.Enabled = true;
                create.Enabled = true;
                status.ForeColor = Color.FromArgb(103, 229, 190); status.Text = "● 系统就绪";
            }
            if (kernelBadge.Visible) kernelBadge.Location = new Point(ClientSize.Width - kernelBadge.PreferredWidth - 46, 38);
            UpdateFields();
        }

        private void UpdateFields()
        {
            ShapeKind k = SelectedShapeKind();
            bool advanced = k >= ShapeKind.RegularPolyhedron;
            value1.Visible = value2.Visible = value3.Visible = !advanced;
            label1.Visible = label2.Visible = label3.Visible = !advanced;
            unit.Enabled = !advanced;
            create.Text = creoMode ? "CREO 建模接口待接入" : (advanced ? "设 置 参 数 并 建 模" : "启 动 建 模");
            modeHint.Text = creoMode ? (k == ShapeKind.RegularPolyhedron ? "CREO 模式 · 设置面数后生成原生 PRT 零件" : "CREO 模式 · 输入基础尺寸并生成原生 PRT 零件") :
                k == ShapeKind.RegularPolyhedron ? "使用正 n 面体原生模板 · 参数可在 SOLIDWORKS 特征树中继续编辑" :
                k == ShapeKind.Washer || k == ShapeKind.Bushing ? "由外径 / 内径 / 高度公式链直接创建，不调用母版" :
                advanced ? "点击下方按钮，在软件内逐项设置此零件的参数" : "输入基础尺寸；可切换 mm / cm / m";
            if (advanced) return;
            label1.Visible = value1.Visible = true;
            label2.Visible = value2.Visible = k != ShapeKind.Sphere;
            label3.Visible = value3.Visible = k == ShapeKind.Box || k == ShapeKind.Frustum || k == ShapeKind.Washer || k == ShapeKind.Bushing;
            if (k == ShapeKind.Box) { label1.Text = "长度  Length"; label2.Text = "宽度  Width"; label3.Text = "高度  Height"; }
            else if (k == ShapeKind.Sphere) label1.Text = "直径  Diameter";
            else if (k == ShapeKind.Cylinder) { label1.Text = "直径  Diameter"; label2.Text = "高度  Height"; }
            else if (k == ShapeKind.Cone) { label1.Text = "底径  Base Ø"; label2.Text = "高度  Height"; }
            else if (k == ShapeKind.Washer) { label1.Text = "外径  Outer Ø"; label2.Text = "内径  Inner Ø"; label3.Text = "厚度  Thickness"; }
            else if (k == ShapeKind.Bushing) { label1.Text = "外径  Outer Ø"; label2.Text = "内径  Inner Ø"; label3.Text = "高度  Height"; }
            else { label1.Text = "底径  Base Ø"; label2.Text = "顶径  Top Ø"; label3.Text = "高度  Height"; }
        }

        private void Browse(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = output.Text, Description = "选择模型保存文件夹" })
                if (dialog.ShowDialog(this) == DialogResult.OK) output.Text = dialog.SelectedPath;
        }
        private double UnitFactor() { return unit.SelectedIndex == 0 ? .001 : unit.SelectedIndex == 1 ? .01 : 1; }
        private double ParsePositive(NebulaTextField box, string name)
        {
            double v;
            if (!double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out v) && !double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) throw new ArgumentException(name + "必须是数字。");
            if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0) throw new ArgumentException(name + "必须大于 0。");
            return v * UnitFactor();
        }

        private async void CreateClicked(object sender, EventArgs e)
        {
            try
            {
                ShapeKind k = SelectedShapeKind();
                PolyhedronOptions polyhedron = null; MasterPartOptions master = null;
                if (k == ShapeKind.RegularPolyhedron) using (var d = new PolyhedronDialog()) { if (d.ShowDialog(this) != DialogResult.OK) return; polyhedron = d.Options; }
                if (k > ShapeKind.RegularPolyhedron) using (var d = new MasterPartDialog(k)) { if (d.ShowDialog(this) != DialogResult.OK) return; master = d.Options; }
                double a = k <= ShapeKind.Bushing ? ParsePositive(value1, label1.Text) : 0;
                double b = k <= ShapeKind.Bushing && k != ShapeKind.Sphere ? ParsePositive(value2, label2.Text) : 0;
                double c = k == ShapeKind.Box || k == ShapeKind.Frustum || k == ShapeKind.Washer || k == ShapeKind.Bushing ? ParsePositive(value3, label3.Text) : 0;
                string folder = output.Text.Trim(); if (folder.Length == 0) throw new ArgumentException("请选择保存目录。"); Directory.CreateDirectory(folder);
                bool step = exportStep.Checked, mf = export3mf.Checked;
                create.Enabled = shape.Enabled = false; status.ForeColor = Color.FromArgb(255, 203, 107); status.Text = "● 正在连接 SOLIDWORKS 并构建几何…";
                var done = new TaskCompletionSource<BuildResult>();
                var thread = new Thread(delegate()
                {
                    try
                    {
                        BuildResult r = polyhedron != null ? SolidWorksBuilder.CreateRegularPolyhedron(polyhedron, folder, step, mf) : master != null ? SolidWorksBuilder.CreateFromMaster(master, folder, step, mf) : SolidWorksBuilder.Create(k, a, b, c, folder, step, mf);
                        done.SetResult(r);
                    }
                    catch (Exception error) { done.SetException(error); }
                });
                thread.SetApartmentState(ApartmentState.STA); thread.Start();
                BuildResult result = await done.Task;
                status.ForeColor = Color.FromArgb(103, 229, 190); status.Text = "● 完成 · " + result.PartPath;
                MessageBox.Show(this, result.Message, "建模完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Exception rootError = ex; while (rootError.InnerException != null) rootError = rootError.InnerException;
                status.ForeColor = Color.FromArgb(255, 119, 151); status.Text = "● 建模失败";
                MessageBox.Show(this, rootError.Message + "\r\n\r\n请确认 SOLIDWORKS 已正常启动、没有模态窗口遮挡，并且当前版本的零件模板可用。", "建模失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { create.Enabled = shape.Enabled = true; }
        }
    }

    internal sealed class SpaceCanvas : Panel
    {
        private struct Star { public float X, Y, R, Brightness; }
        private struct Meteor { public float StartY, EndY, Delay, Speed, Sway; }
        private readonly List<Star> stars = new List<Star>();
        private readonly List<Meteor> meteors = new List<Meteor>();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private float time;
        internal SpaceCanvas()
        {
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            DoubleBuffered = true; ResizeRedraw = true; BackColor = Color.FromArgb(5, 8, 24);
            var random = new Random(7307);
            for (int i = 0; i < 150; i++) stars.Add(new Star { X = (float)random.NextDouble(), Y = (float)random.NextDouble(), R = .45f + (float)random.NextDouble() * 1.65f, Brightness = 75+(float)random.NextDouble()*125 });
            for (int i = 0; i < 11; i++) meteors.Add(new Meteor { StartY = -.20f+(float)random.NextDouble()*.34f, EndY=.80f+(float)random.NextDouble()*.38f, Delay=i*.31f, Speed=.16f+(float)random.NextDouble()*.055f, Sway=6+(float)random.NextDouble()*13 });
            timer.Interval = 40; timer.Tick += delegate { time += .04f; Invalidate(); }; timer.Start();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(4, 7, 25), Color.FromArgb(22, 11, 54), 18F)) g.FillRectangle(bg, ClientRectangle);
            using (var nebula = new SolidBrush(Color.FromArgb(28, 61, 92, 220))) g.FillEllipse(nebula, -150, -280, Width * .72f, Height * .78f);
            foreach (Star s in stars)
            {
                using (var b = new SolidBrush(Color.FromArgb((int)s.Brightness, 196, 219, 255))) g.FillEllipse(b, s.X * Width, s.Y * Height, s.R, s.R);
            }
            for (int i = 0; i < meteors.Count; i++)
            {
                Meteor m=meteors[i];float cycle=(time*m.Speed+m.Delay)%1.45f;if(cycle>1.05f)continue;float p=cycle/1.05f;
                PointF head=MeteorPoint(m,p,i);
                PointF previous=head;
                // 拖尾使用流星先前经过的位置，而不是固定直线，形成轻微摇曳的牵引感。
                for(int segment=1;segment<=15;segment++)
                {
                    float past=Math.Max(0F,p-segment*.018F);
                    PointF point=MeteorPoint(m,past,i);
                    int alpha=Math.Max(12,165-segment*10);
                    float width=Math.Max(.55F,2.6F-segment*.13F);
                    using(var pen=new Pen(Color.FromArgb(alpha,112+Math.Min(80,segment*8),199+Math.Min(45,segment*4),255),width)) g.DrawLine(pen,previous,point);
                    previous=point;
                }
                using(var halo=new SolidBrush(Color.FromArgb(70,99,226,255)))g.FillEllipse(halo,head.X-6,head.Y-6,12,12);
                using(var glow=new SolidBrush(Color.FromArgb(238,230,248,255)))g.FillEllipse(glow,head.X-2.3f,head.Y-2.3f,4.6f,4.6f);
            }
        }

        private PointF MeteorPoint(Meteor meteor,float progress,int index)
        {
            float x=(1.12f-progress*1.34f)*Width;
            float y=(meteor.StartY+(meteor.EndY-meteor.StartY)*progress)*Height;
            float sway=(float)Math.Sin(progress*5.2f+index*1.73f)*meteor.Sway*1.45f;
            return new PointF(x+sway*.58f,y+sway);
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
