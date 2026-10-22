// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;

[assembly: System.Reflection.AssemblyTitle("Nebula Gear Studio V5.0")]
[assembly: System.Reflection.AssemblyDescription("宇宙主题参数化建模器：基础实体、直齿轮、斜齿轮与蜗轮蜗杆")]
[assembly: System.Reflection.AssemblyCompany("本地工具")]
[assembly: System.Reflection.AssemblyProduct("Nebula Gear Studio")]
[assembly: System.Reflection.AssemblyFileVersion("5.0.0.0")]

namespace SolidWorksPrimitiveBuilderV2
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CosmicMainForm());
        }
    }

    internal enum ShapeKind
    {
        Box, Sphere, Cylinder, Cone, Frustum, Washer, Bushing, RegularPolyhedron,
        SpurGear, HelicalGearRight, HelicalGearLeft, SpurRing, HelicalRingRight,
        HelicalRingLeft, BevelGear, Worm, WormWheelRight, WormWheelLeft,
        CompressionSpring, ExtensionSpring, TorsionSpring, ConicalSpringPlain,
        ConicalSpringClosed,
        // 旧版未使用的 MainForm 仍引用这个名称；当前界面按 SpurGear 枚举值工作。
        Gear = SpurGear
    }

    internal sealed class MainForm : Form
    {
        private readonly ComboBox shape = new ComboBox();
        private readonly ComboBox unit = new ComboBox();
        private readonly Label label1 = new Label(), label2 = new Label(), label3 = new Label();
        private readonly TextBox value1 = new TextBox(), value2 = new TextBox(), value3 = new TextBox();
        private readonly TextBox output = new TextBox();
        private readonly CheckBox exportStep = new CheckBox(), export3mf = new CheckBox();
        private readonly Button create = new Button();
        private readonly Label status = new Label();

        internal MainForm()
        {
            Text = "SolidWorks 多形体建模器 V2.1 · 齿轮版";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            ClientSize = new Size(590, 455);
            MinimumSize = new Size(606, 494);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 10F);

            var title = new Label { Text = "SolidWorks 多形体建模器", Font = new Font(Font.FontFamily, 17F, FontStyle.Bold), AutoSize = true, Location = new Point(24, 20) };
            var subtitle = new Label { Text = "参数化创建基础实体，并导出 SLDPRT / STEP / 3MF", ForeColor = Color.DimGray, AutoSize = true, Location = new Point(27, 58) };
            Controls.Add(title); Controls.Add(subtitle);

            AddLabel("形体：", 28, 98);
            shape.DropDownStyle = ComboBoxStyle.DropDownList;
            shape.Items.AddRange(new object[] { "长方体", "球体", "圆柱体", "圆锥体", "圆台", "渐开线直齿轮" });
            shape.SelectedIndex = 0;
            shape.Location = new Point(135, 94); shape.Width = 220;
            shape.SelectedIndexChanged += delegate { UpdateFields(); };
            Controls.Add(shape);

            AddLabel("单位：", 378, 98);
            unit.DropDownStyle = ComboBoxStyle.DropDownList;
            unit.Items.AddRange(new object[] { "毫米 (mm)", "厘米 (cm)", "米 (m)" });
            unit.SelectedIndex = 0; unit.Location = new Point(445, 94); unit.Width = 115;
            Controls.Add(unit);

            ConfigureField(label1, value1, 142);
            ConfigureField(label2, value2, 184);
            ConfigureField(label3, value3, 226);
            value1.Text = "100"; value2.Text = "100"; value3.Text = "100";

            AddLabel("保存到：", 28, 274);
            output.Location = new Point(135, 270); output.Width = 348;
            output.Text = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "SolidWorks零件");
            Controls.Add(output);
            var browse = new Button { Text = "浏览…", Location = new Point(491, 268), Size = new Size(69, 31) };
            browse.Click += Browse;
            Controls.Add(browse);

            exportStep.Text = "同时导出 STEP"; exportStep.Checked = true; exportStep.AutoSize = true; exportStep.Location = new Point(135, 315);
            export3mf.Text = "同时导出 3MF"; export3mf.Checked = true; export3mf.AutoSize = true; export3mf.Location = new Point(315, 315);
            Controls.Add(exportStep); Controls.Add(export3mf);

            create.Text = "创建形体零件"; create.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold); create.Location = new Point(135, 354); create.Size = new Size(425, 43);
            create.Click += CreateClicked;
            Controls.Add(create);

            status.Text = "就绪"; status.AutoEllipsis = true; status.ForeColor = Color.DimGray; status.Location = new Point(28, 416); status.Size = new Size(532, 25);
            Controls.Add(status);
            UpdateFields();
        }

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x, y + 4) });
        }

        private void ConfigureField(Label label, TextBox box, int y)
        {
            label.AutoSize = true; label.Location = new Point(28, y + 4); Controls.Add(label);
            box.Location = new Point(135, y); box.Width = 220; Controls.Add(box);
        }

        private void UpdateFields()
        {
            ShapeKind k = (ShapeKind)shape.SelectedIndex;
            label1.Visible = true;
            value1.Visible = k != ShapeKind.Gear;
            unit.Enabled = k != ShapeKind.Gear;
            create.Text = k == ShapeKind.Gear ? "设置齿轮参数并创建…" : "创建形体零件";
            label2.Visible = value2.Visible = k != ShapeKind.Sphere;
            label3.Visible = value3.Visible = k == ShapeKind.Box || k == ShapeKind.Frustum;
            if (k == ShapeKind.Box) { label1.Text = "长度："; label2.Text = "宽度："; label3.Text = "高度："; }
            if (k == ShapeKind.Sphere) { label1.Text = "直径："; }
            if (k == ShapeKind.Cylinder) { label1.Text = "直径："; label2.Text = "高度："; }
            if (k == ShapeKind.Cone) { label1.Text = "底面直径："; label2.Text = "高度："; }
            if (k == ShapeKind.Frustum) { label1.Text = "底面直径："; label2.Text = "顶面直径："; label3.Text = "高度："; }
            if (k == ShapeKind.Gear)
            {
                label1.Text = "点击下方按钮设置模数、齿数、压力角、孔径等。\n齿轮尺寸统一使用毫米，角度使用度。";
                label2.Visible = value2.Visible = label3.Visible = value3.Visible = false;
            }
        }

        private void Browse(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = output.Text, Description = "选择模型保存文件夹" })
                if (dialog.ShowDialog(this) == DialogResult.OK) output.Text = dialog.SelectedPath;
        }

        private double ParsePositive(TextBox box, string name)
        {
            double v;
            if (!double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out v) &&
                !double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                throw new ArgumentException(name + "必须是数字。");
            if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0) throw new ArgumentException(name + "必须是大于 0 的有限数字。");
            return v * UnitFactor();
        }

        private double UnitFactor() { return unit.SelectedIndex == 0 ? 0.001 : unit.SelectedIndex == 1 ? 0.01 : 1.0; }

        private async void CreateClicked(object sender, EventArgs e)
        {
            try
            {
                var k = (ShapeKind)shape.SelectedIndex;
                GearOptions gear = null;
                if (k == ShapeKind.Gear)
                {
                    using (var dialog = new GearDialog())
                    {
                        if (dialog.ShowDialog(this) != DialogResult.OK) return;
                        gear = dialog.Options;
                    }
                }
                double a = k == ShapeKind.Gear ? 0 : ParsePositive(value1, label1.Text.TrimEnd('：'));
                double b = k == ShapeKind.Sphere || k == ShapeKind.Gear ? 0 : ParsePositive(value2, label2.Text.TrimEnd('：'));
                double c = (k == ShapeKind.Box || k == ShapeKind.Frustum) ? ParsePositive(value3, label3.Text.TrimEnd('：')) : 0;
                string folder = output.Text.Trim();
                if (folder.Length == 0) throw new ArgumentException("请选择保存目录。");
                Directory.CreateDirectory(folder);

                create.Enabled = false; shape.Enabled = false; status.Text = "正在连接 SOLIDWORKS 并创建模型…";
                bool step = exportStep.Checked, mf = export3mf.Checked;
                var completion = new TaskCompletionSource<BuildResult>();
                var thread = new Thread(() => {
                    try { completion.SetResult(gear == null ? SolidWorksBuilder.Create(k, a, b, c, folder, step, mf) : SolidWorksBuilder.CreateGear(gear, folder, step, mf)); }
                    catch (Exception error) { completion.SetException(error); }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                var result = await completion.Task;
                status.Text = "完成：" + result.PartPath;
                MessageBox.Show(this, result.Message, "创建完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                status.Text = "创建失败";
                MessageBox.Show(this, FriendlyError(ex), "创建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { create.Enabled = true; shape.Enabled = true; }
        }

        private static string FriendlyError(Exception ex)
        {
            Exception e = ex;
            while (e.InnerException != null) e = e.InnerException;
            return e.Message + "\r\n\r\n请确认：\r\n1. 已安装并启动受支持的 SOLIDWORKS；\r\n2. 没有模态对话框挡住 SOLIDWORKS；\r\n3. 当前版本的零件模板和保存目录可用。";
        }
    }

    internal sealed class BuildResult { internal string PartPath; internal string Message; }

    internal static partial class SolidWorksBuilder
    {
        internal static BuildResult CreateRegularPolyhedron(PolyhedronOptions options, string folder, bool step, bool threeMf)
        {
            options.Validate();
            SldWorks app = Connect();
            int year = SolidWorksCompatibility.InstalledYear(app);
            string library = Path.Combine(Path.GetDirectoryName(typeof(SolidWorksBuilder).Assembly.Location), "母版库");
            string template = SolidWorksCompatibility.ResolveMother(library, year, "正n面体.PRTDOT");
            // 使用用户提供的原生 PRTDOT：保留其草图、方程、特征树和参数化关系，
            // 而不是把多面体离散化为三角片网格。
            ModelDoc2 model = app.NewDocument(template, 0, 0.0, 0.0);
            if (model == null) throw new InvalidOperationException("SOLIDWORKS " + year + " 无法从正 n 面体模板新建零件；请确认文件已真正另存为当前版本：\r\n" + template);
            EquationMgr equations = model.GetEquationMgr();
            bool changed = false;
            int nIndex = -1;
            string target = options.Faces.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < equations.GetCount(); i++)
            {
                string equation = equations.get_Equation(i);
                int equals = equation == null ? -1 : equation.IndexOf('=');
                string left = equals < 0 ? String.Empty : equation.Substring(0, equals).Trim();
                if (left == "\"n\"" || left == "\"N\"" || left == "\"面数\"")
                {
                    nIndex = i;
                    string current = equation.Substring(equals + 1).Split('\'')[0].Trim();
                    if (current != target) equations.set_Equation(i, left + " = " + target);
                    changed = true;
                    break;
                }
            }
            if (!changed) throw new InvalidOperationException("正 n 面体模板中没有找到全局变量 n。为避免错误地继续生成默认正四面体，已停止保存；请检查模板方程式中的 n 变量名称。");
            if (!model.EditRebuild3()) throw new InvalidOperationException("SOLIDWORKS 无法重建正 n 面体，请检查 n 的值和模板特征。");
            string updated = equations.get_Equation(nIndex);
            int updatedEquals = updated == null ? -1 : updated.IndexOf('=');
            if (updatedEquals < 0 || updated.Substring(updatedEquals + 1).Split('\'')[0].Trim() != target)
                throw new InvalidOperationException("SOLIDWORKS 未写入正 n 面体的面数参数；已停止保存，避免生成错误的默认模型。请检查方程编辑功能是否可用。");
            return SaveModel(model, options.DisplayName, folder, step, threeMf);
        }

        internal static BuildResult Create(ShapeKind kind, double a, double b, double c, string folder, bool step, bool threeMf)
        {
            SldWorks app = Connect();
            string template = FindTemplate(app);
            ModelDoc2 model = app.NewDocument(template, 0, 0.0, 0.0);
            if (model == null) throw new InvalidOperationException("SOLIDWORKS " + SolidWorksCompatibility.InstalledYear(app) + " 无法从此模板新建零件；请确认它不是更高版本的文件：\r\n" + template);

            string shapeName;
            switch (kind)
            {
                case ShapeKind.Box: CreateBox(model, a, b, c); shapeName = "长方体"; break;
                case ShapeKind.Sphere: CreateSphere(model, a / 2.0); shapeName = "球体"; break;
                case ShapeKind.Cylinder: CreateModelerBody(app, model, new double[] { 0, 0, 0, 0, 0, 1, a / 2.0, b }, false, "圆柱体"); shapeName = "圆柱体"; break;
                case ShapeKind.Cone: CreateModelerBody(app, model, new double[] { 0, 0, 0, 0, 0, 1, a / 2.0, 0, b }, true, "圆锥体"); shapeName = "圆锥体"; break;
                case ShapeKind.Washer: CreateHollowCylinder(model, a, b, c, "垫片"); shapeName = "垫片"; break;
                case ShapeKind.Bushing: CreateHollowCylinder(model, a, b, c, "轴套"); shapeName = "轴套"; break;
                default: CreateModelerBody(app, model, new double[] { 0, 0, 0, 0, 0, 1, a / 2.0, b / 2.0, c }, true, "圆台"); shapeName = "圆台"; break;
            }

            return SaveModel(model, shapeName, folder, step, threeMf);
        }

        private static BuildResult SaveModel(ModelDoc2 model, string shapeName, string folder, bool step, bool threeMf)
        {
            Directory.CreateDirectory(folder);
            model.ClearSelection2(true);
            model.EditRebuild3(); model.ShowNamedView2("*Isometric", 7); model.ViewZoomtofit2(); model.GraphicsRedraw2();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string basePath = Path.Combine(folder, shapeName + "_" + stamp);
            int err = 0, warn = 0;
            ModelDocExtension ext = model.Extension;
            bool partOk = ext.SaveAs(basePath + ".SLDPRT", 0, 1, null, ref err, ref warn);
            if (!partOk || err != 0 || !File.Exists(basePath + ".SLDPRT")) throw new IOException("SLDPRT 保存失败，SOLIDWORKS 错误代码：" + err);

            string message = "已保存：\r\n" + basePath + ".SLDPRT";
            if (step) message += Export(ext, basePath + ".STEP", "STEP");
            if (threeMf) message += Export(ext, basePath + ".3MF", "3MF");
            return new BuildResult { PartPath = basePath + ".SLDPRT", Message = message };
        }

        private static SldWorks Connect()
        {
            object app = null;
            try { app = Marshal.GetActiveObject("SldWorks.Application"); } catch { }
            if (app == null)
            {
                Type t = Type.GetTypeFromProgID("SldWorks.Application");
                if (t == null) throw new InvalidOperationException("没有检测到 SOLIDWORKS COM 自动化接口。");
                app = Activator.CreateInstance(t);
            }
            SldWorks sw = (SldWorks)app; sw.Visible = true; return sw;
        }

        private static string FindTemplate(SldWorks app)
        {
            int year = SolidWorksCompatibility.InstalledYear(app);
            try
            {
                string preferred = app.GetUserPreferenceStringValue(8);
                if (!String.IsNullOrWhiteSpace(preferred) && File.Exists(preferred) &&
                    !SolidWorksCompatibility.IsFutureTemplatePath(preferred, year)) return preferred;
            }
            catch { }
            try
            {
                string selected = app.GetDocumentTemplate(1, String.Empty, 0, 0.0, 0.0);
                if (!String.IsNullOrWhiteSpace(selected) && File.Exists(selected) &&
                    !SolidWorksCompatibility.IsFutureTemplatePath(selected, year)) return selected;
            }
            catch { }
            string folder = Path.Combine(@"C:\ProgramData\SOLIDWORKS", "SOLIDWORKS " + year, "templates");
            string[] names = { "gb_part.prtdot", "零件模板GB.prtdot", "Part.prtdot" };
            foreach (string name in names)
            {
                string candidate = Path.Combine(folder, name);
                if (File.Exists(candidate)) return candidate;
            }
            if (Directory.Exists(folder))
                foreach (string candidate in Directory.GetFiles(folder, "*.prtdot", SearchOption.TopDirectoryOnly)) return candidate;
            throw new FileNotFoundException("没有找到 SOLIDWORKS " + year + " 的零件模板（PRTDOT）。请在该版本的系统选项中配置默认零件模板。");
        }

        private static Feature SelectPlane(ModelDoc2 model)
        {
            Feature f = model.FirstFeature();
            while (f != null) { if ((string)f.GetTypeName2() == "RefPlane") { if (!(bool)f.Select2(false, 0)) break; return f; } f = f.GetNextFeature(); }
            throw new InvalidOperationException("无法选择 SOLIDWORKS 基准面。");
        }

        private static void CreateBox(ModelDoc2 model, double length, double width, double height)
        {
            model.ClearSelection2(true); SelectPlane(model);
            SketchManager sm = model.SketchManager; sm.InsertSketch(true);
            sm.CreateCornerRectangle(-length / 2, -width / 2, 0, length / 2, width / 2, 0); sm.InsertSketch(true);
            Feature feature = model.FeatureManager.FeatureExtrusion2(true, false, false, 0, 0, height, 0.01, false, false, false, false, 0.0, 0.0, false, false, false, false, true, true, true, 0, 0.0, false);
            if (feature == null) throw new InvalidOperationException("长方体拉伸失败。");
            feature.Name = "长方体拉伸";
        }

        private static void CreateSphere(ModelDoc2 model, double radius)
        {
            model.ClearSelection2(true); SelectPlane(model);
            SketchManager sm = model.SketchManager; sm.InsertSketch(true);
            sm.CreateArc(0.0, 0.0, 0.0, 0.0, -radius, 0.0, 0.0, radius, 0.0, (short)-1);
            sm.CreateCenterLine(0.0, -radius, 0.0, 0.0, radius, 0.0);
            sm.InsertSketch(true);
            Feature feature = model.FeatureManager.FeatureRevolve2(true, true, false, false, false, false, 0, 0, Math.PI * 2.0, 0.0, false, false, 0.0, 0.0, 0, 0.0, 0.0, true, true, true);
            if (feature == null) throw new InvalidOperationException("球体旋转特征创建失败。");
            feature.Name = "球体旋转";
        }

        // 不依赖 PRTDOT 母版：由外径、内径和高度的约束链直接建立同心草图并拉伸成实体。
        private static void CreateHollowCylinder(ModelDoc2 model, double outside, double inside, double height, string name)
        {
            if (outside <= inside) throw new ArgumentException(name + "的外径必须大于内径。");
            model.ClearSelection2(true); SelectPlane(model);
            SketchManager manager = model.SketchManager; manager.InsertSketch(true);
            manager.CreateCircleByRadius(0, 0, 0, outside / 2.0);
            manager.CreateCircleByRadius(0, 0, 0, inside / 2.0);
            manager.InsertSketch(true);
            Feature feature = model.FeatureManager.FeatureExtrusion2(true, false, false, 0, 0, height, 0.01, false, false, false, false, 0.0, 0.0, false, false, false, false, true, true, true, 0, 0.0, false);
            if (feature == null) throw new InvalidOperationException(name + "拉伸失败。");
            feature.Name = name + "拉伸（外径/内径/高度）";
        }

        private static void CreateModelerBody(SldWorks app, ModelDoc2 model, double[] dimensions, bool cone, string name)
        {
            Modeler modeler = app.GetModeler();
            Body2 body = (Body2)(cone ? modeler.CreateBodyFromCone(dimensions) : modeler.CreateBodyFromCyl(dimensions));
            if (body == null) throw new InvalidOperationException(name + "几何体创建失败。");
            Feature feature = ((PartDoc)model).CreateFeatureFromBody3(body, false, 0);
            if (feature == null) throw new InvalidOperationException(name + "实体特征创建失败。");
            feature.Name = name;
        }

        private static string Export(ModelDocExtension ext, string path, string label)
        {
            int err = 0, warn = 0; bool ok = ext.SaveAs(path, 0, 1, null, ref err, ref warn);
            if (ok && err == 0 && File.Exists(path)) return "\r\n" + path;
            throw new IOException(label + " 导出失败（SOLIDWORKS 错误代码 " + err + "）。原生 SLDPRT 零件已保留：\r\n" + Path.ChangeExtension(path, ".SLDPRT"));
        }
    }
}
