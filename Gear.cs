// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;

namespace SolidWorksPrimitiveBuilderV2
{
    // Millimetres and degrees at the UI boundary; SOLIDWORKS coordinates are metres.
    internal sealed class GearOptions
    {
        public double Module = 2, Pressure = 20, Width = 10, Bore = 8, Shift = 0;
        public double Addendum = 1, Clearance = 0.25, RootFillet = 0.3, Chamfer = 0.15;
        public int Teeth = 20;
        public double Pitch { get { return Module * Teeth; } }
        public double BaseRadius { get { return Pitch / 2 * Math.Cos(Pressure * Math.PI / 180); } }
        public double TipRadius { get { return Pitch / 2 + Module * (Addendum + Shift); } }
        public double RootRadius { get { return Pitch / 2 - Module * (Addendum + Clearance - Shift); } }
        public double StartRadius { get { return Math.Max(RootRadius, BaseRadius); } }
        public double HalfAngle(double r)
        {
            double alpha = Pressure * Math.PI / 180;
            double t = Math.Sqrt(Math.Max(0, r * r / (BaseRadius * BaseRadius) - 1));
            return (Math.PI / 2 + 2 * Shift * Math.Tan(alpha)) / Teeth + Math.Tan(alpha) - alpha - (t - Math.Atan(t));
        }
        public void Validate()
        {
            foreach (double v in new double[] { Module, Pressure, Width, Bore, Shift, Addendum, Clearance, RootFillet, Chamfer })
                if (double.IsNaN(v) || double.IsInfinity(v)) throw new ArgumentException("参数必须为有限数字。");
            if (Module < 0.1 || Module > 30 || Teeth < 8 || Teeth > 120 || Pressure < 14.5 || Pressure > 30 || Width < 0.2 || Width > 200)
                throw new ArgumentException("范围：模数 0.1–30 mm；齿数 8–120；压力角 14.5–30°；齿宽 0.2–200 mm。");
            if (Shift < -0.5 || Shift > 1 || Addendum < 0.5 || Addendum > 1.25 || Clearance < 0.1 || Clearance > 0.5)
                throw new ArgumentException("范围：变位系数 -0.5–1；齿顶高系数 0.5–1.25；顶隙系数 0.1–0.5。");
            double sin = Math.Sin(Pressure * Math.PI / 180);
            if (Shift < Addendum - Teeth * sin * sin / 2 - 1e-9)
                throw new ArgumentException("该组合存在根切风险，本版不模拟滚刀根切。请增加齿数/正变位，或降低齿顶高系数。");
            if (RootRadius <= 0 || TipRadius <= StartRadius || HalfAngle(TipRadius) <= 0.002 || HalfAngle(StartRadius) >= Math.PI / Teeth)
                throw new ArgumentException("齿顶过尖或齿根相交，请调整模数、齿数和系数。");
            if (Bore < 0 || Bore / 2 >= RootRadius - Math.Max(Module * 0.25, 2 * Chamfer))
                throw new ArgumentException("轴孔过大：轴孔与齿根之间需保留足够壁厚；0 表示无孔。");
            if (RootFillet < 0 || Chamfer < 0 || Chamfer >= Math.Min(Width / 2, Module / 4))
                throw new ArgumentException("圆角/倒角不能为负；端面倒角必须小于齿宽的一半及模数的 1/4。0 表示不加工。");
            if (RootFillet > 0)
            {
                double contact = Math.Sqrt(RootRadius * RootRadius + 2 * RootRadius * RootFillet);
                double delta = Math.Asin(RootFillet / (RootRadius + RootFillet));
                if (contact >= BaseRadius - 0.0001 || HalfAngle(StartRadius) + delta >= Math.PI / Teeth - 0.0001)
                    throw new ArgumentException("齿根圆角放不下：减小圆角半径，或设为 0。本版圆角位于基圆以下的径向过渡段。");
            }
        }
        public string Description()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "模数={0} mm; 齿数={1}; 压力角={2} deg; 齿宽={3} mm; 孔径={4} mm\r\n变位系数={5}; 齿顶高系数={6}; 顶隙系数={7}; 齿根圆角={8} mm; 端面45度倒角={9} mm\r\n分度圆直径={10:F4} mm; 齿顶圆直径={11:F4} mm; 齿根圆直径={12:F4} mm; 基圆直径={13:F4} mm",
                Module, Teeth, Pressure, Width, Bore, Shift, Addendum, Clearance, RootFillet, Chamfer, Pitch, 2*TipRadius, 2*RootRadius, 2*BaseRadius);
        }
    }

    internal sealed class GearDialog : Form
    {
        private readonly NumericUpDown[] fields = new NumericUpDown[10];
        private readonly Label summary = new Label();
        public GearOptions Options;
        public GearDialog()
        {
            Text = "渐开线直齿轮 · 参数（mm / °）";
            UiRounder.PreferRoundedWindow(this);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Font = new Font("Microsoft YaHei UI", 10);
            ClientSize = new Size(660, 540); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; MaximizeBox = false;
            string[] names = {"模数 m (mm)", "齿数 z", "压力角 α (°)", "齿宽 b (mm)", "轴孔直径 (mm)", "变位系数 x", "齿顶高系数 ha*", "顶隙系数 c*", "齿根圆角 R (mm)", "端面倒角 C (mm)"};
            decimal[] initial = {2,20,20,10,8,0,1,0.25m,0.3m,0.15m};
            for(int i=0;i<10;i++)
            {
                int x=24+(i%2)*320, y=28+(i/2)*65;
                Controls.Add(new Label {Text=names[i],Location=new Point(x,y),AutoSize=true});
                var n = new NumericUpDown {Location=new Point(x+165,y-3),Width=118,DecimalPlaces=i==1?0:3,Minimum=i==5?-0.5m:0,Maximum=i==1?120:10000,Increment=i==1?1:0.1m,Value=initial[i]};
                UiRounder.Apply(n,7);
                fields[i]=n; Controls.Add(n); n.ValueChanged += delegate { UpdateSummary(); };
            }
            summary.Location=new Point(24,357); summary.Size=new Size(612,70); Controls.Add(summary);
            Controls.Add(new Label {Text="渐开线用样条拟合；齿根为圆弧过渡，未模拟滚刀包络。\n适合模型/打印验证；不包含配对啮合与承载校核。",Location=new Point(24,433),Size=new Size(612,45),ForeColor=Color.DimGray});
            var ok=new Button {Text="创建齿轮",Location=new Point(450,485),Size=new Size(180,36)};
            ok.FlatStyle=FlatStyle.Flat;ok.FlatAppearance.BorderColor=Color.FromArgb(128,117,255);UiRounder.Apply(ok,10);
            ok.Click += delegate { try { var o=Read(); o.Validate(); Options=o; DialogResult=DialogResult.OK; } catch(Exception ex) { MessageBox.Show(this,ex.Message,"参数需要调整"); } };
            Controls.Add(ok); AcceptButton=ok;
            UpdateSummary();
        }
        private GearOptions Read()
        {
            return new GearOptions {Module=(double)fields[0].Value,Teeth=(int)fields[1].Value,Pressure=(double)fields[2].Value,Width=(double)fields[3].Value,Bore=(double)fields[4].Value,Shift=(double)fields[5].Value,Addendum=(double)fields[6].Value,Clearance=(double)fields[7].Value,RootFillet=(double)fields[8].Value,Chamfer=(double)fields[9].Value};
        }
        private void UpdateSummary()
        {
            foreach(var f in fields) if(f==null)return;
            var o=Read();
            summary.Text=string.Format("分度圆 Ø {0:F3} mm    齿顶圆 Ø {1:F3} mm\n齿根圆 Ø {2:F3} mm    基圆 Ø {3:F3} mm",o.Pitch,o.TipRadius*2,o.RootRadius*2,o.BaseRadius*2);
        }
    }

    internal static partial class SolidWorksBuilder
    {
        private static bool SetAnyEquation(EquationMgr equations,string[] names,string value)
        {
            bool changed=false;foreach(string name in names)changed|=SetGlobalEquation(equations,name,value);return changed;
        }

        private static BuildResult CreateGearFromEquationMaster(SldWorks app,GearOptions o,string folder,bool step,bool mf)
        {
            string master=Path.Combine(Path.GetDirectoryName(typeof(SolidWorksBuilder).Assembly.Location),"圆柱直齿轮方程母版.SLDPRT");
            if(!File.Exists(master))throw new FileNotFoundException("安装包中的齿轮方程母版缺失。请重新运行安装程序。",master);
            int errors=0,warnings=0;ModelDoc2 model=app.OpenDoc6(master,1,0,"",ref errors,ref warnings);
            if(model==null)throw new Exception("无法打开齿轮方程母版（SOLIDWORKS 错误代码："+errors+"）。");
            EquationMgr equations=model.GetEquationMgr();
            int updated=0;
            if(SetAnyEquation(equations,new[]{"m","mn","模数"},o.Module.ToString(CultureInfo.InvariantCulture)))updated++;
            if(SetAnyEquation(equations,new[]{"z","Z","齿数"},o.Teeth.ToString(CultureInfo.InvariantCulture)))updated++;
            if(SetAnyEquation(equations,new[]{"a","α","压力角"},o.Pressure.ToString(CultureInfo.InvariantCulture)))updated++;
            if(SetAnyEquation(equations,new[]{"B","b","齿宽"},o.Width.ToString(CultureInfo.InvariantCulture)))updated++;
            SetAnyEquation(equations,new[]{"x","变位系数"},o.Shift.ToString(CultureInfo.InvariantCulture));
            SetAnyEquation(equations,new[]{"ha","齿顶高系数"},o.Addendum.ToString(CultureInfo.InvariantCulture));
            SetAnyEquation(equations,new[]{"c","顶隙系数"},o.Clearance.ToString(CultureInfo.InvariantCulture));
            SetAnyEquation(equations,new[]{"R","齿根圆角"},o.RootFillet.ToString(CultureInfo.InvariantCulture));
            SetAnyEquation(equations,new[]{"C","C1","倒角"},o.Chamfer.ToString(CultureInfo.InvariantCulture));
            if(updated<2)throw new InvalidOperationException("齿轮方程母版未识别到模数/齿数变量，已停止保存以保护母版。请检查“零件1”的全局变量命名。");
            model.EditRebuild3();
            BuildResult result=SaveModel(model,"齿轮_m"+o.Module.ToString(CultureInfo.InvariantCulture)+"_z"+o.Teeth,folder,step,mf);
            File.WriteAllText(Path.ChangeExtension(result.PartPath,".parameters.txt"),o.Description()+"\r\n由安装包内置的 圆柱直齿轮方程母版.SLDPRT 生成；本次输入已写入母版全局方程后另存，原母版不覆盖。",new UTF8Encoding(true));
            result.Message+="\r\n\r\n"+o.Description();return result;
        }

        internal struct P
        {
            public double X,Y;
            public P(double x,double y) {X=x;Y=y;}
            public static P Polar(double r,double a) {return new P(r*Math.Cos(a),r*Math.Sin(a));}
        }
        internal static void GearLine(SketchManager sm,P a,P b)
        {
            if(Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)<1e-8)return;
            if(sm.CreateLine(a.X/1000,a.Y/1000,0,b.X/1000,b.Y/1000,0)==null)throw new Exception("齿轮直线创建失败。");
        }
        internal static void GearArc(SketchManager sm,P center,P a,P b,bool ccw)
        {
            if(Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)<1e-8)return;
            if(sm.CreateArc(center.X/1000,center.Y/1000,0,a.X/1000,a.Y/1000,0,b.X/1000,b.Y/1000,0,(short)(ccw?1:-1))==null)throw new Exception("齿轮圆弧创建失败。");
        }
        internal static void GearFlank(SketchManager sm,GearOptions o,double theta,int side,bool outward)
        {
            const int count=25;
            var points=new double[count*3];
            double t0=Math.Sqrt(Math.Max(0,o.StartRadius*o.StartRadius/(o.BaseRadius*o.BaseRadius)-1));
            double t1=Math.Sqrt(o.TipRadius*o.TipRadius/(o.BaseRadius*o.BaseRadius)-1);
            for(int i=0;i<count;i++)
            {
                double f=(double)i/(count-1);if(!outward)f=1-f;
                double t=t0+(t1-t0)*f;
                double r=o.BaseRadius*Math.Sqrt(1+t*t);
                P p=P.Polar(r,theta+side*o.HalfAngle(r));
                points[3*i]=p.X/1000;points[3*i+1]=p.Y/1000;
            }
            if(sm.CreateSpline(points)==null)throw new Exception("渐开线样条创建失败。");
        }

        internal static void DrawGearProfile(SketchManager sm, GearOptions o, double rotation, bool includeBore)
        {
            double pitch=2*Math.PI/o.Teeth,h=o.HalfAngle(o.StartRadius),ht=o.HalfAngle(o.TipRadius);
            double delta=o.RootFillet>0?Math.Asin(o.RootFillet/(o.RootRadius+o.RootFillet)):0;
            double contact=o.RootFillet>0?Math.Sqrt(o.RootRadius*o.RootRadius+2*o.RootRadius*o.RootFillet):o.RootRadius;
            for(int i=0;i<o.Teeth;i++)
            {
                double theta=i*pitch+rotation;
                P lowerRoot=P.Polar(o.RootRadius,theta-h-delta),lowerContact=P.Polar(contact,theta-h);
                if(o.RootFillet>0)GearArc(sm,P.Polar(o.RootRadius+o.RootFillet,theta-h-delta),lowerRoot,lowerContact,false);
                GearLine(sm,lowerContact,P.Polar(o.StartRadius,theta-h));
                GearFlank(sm,o,theta,-1,true);
                GearArc(sm,new P(0,0),P.Polar(o.TipRadius,theta-ht),P.Polar(o.TipRadius,theta+ht),true);
                GearFlank(sm,o,theta,1,false);
                P upperContact=P.Polar(contact,theta+h),upperRoot=P.Polar(o.RootRadius,theta+h+delta);
                GearLine(sm,P.Polar(o.StartRadius,theta+h),upperContact);
                if(o.RootFillet>0)GearArc(sm,P.Polar(o.RootRadius+o.RootFillet,theta+h+delta),upperContact,upperRoot,false);
                GearArc(sm,new P(0,0),upperRoot,P.Polar(o.RootRadius,theta+pitch-h-delta),true);
            }
            if(includeBore && o.Bore>0 && sm.CreateCircleByRadius(0,0,0,o.Bore/2000)==null)throw new Exception("轴孔草图创建失败。");
        }

        internal static BuildResult CreateGear(GearOptions o,string folder,bool step,bool mf)
        {
            o.Validate();
            SldWorks app=Connect();
            return CreateGearFromEquationMaster(app,o,folder,step,mf);
#pragma warning disable 0162
            ModelDoc2 model=app.NewDocument(FindTemplate(app),0,0,0);
            if(model==null)throw new Exception("无法新建齿轮零件。");
            model.ClearSelection2(true);SelectPlane(model);
            SketchManager sm=model.SketchManager;sm.InsertSketch(true);
            Sketch sketch=sm.ActiveSketch;
            sm.AddToDB=true;
            try
            {
                DrawGearProfile(sm,o,0,true);
            }
            finally {sm.AddToDB=false;}
            sm.InsertSketch(true);
            model.ClearSelection2(true);
            ((Feature)sketch).Select2(false,0);
            Feature feature=model.FeatureManager.FeatureExtrusion2(true,false,false,0,0,o.Width/1000,0,false,false,false,false,0,0,false,false,false,false,true,true,true,0,0,false);
            if(feature==null)throw new Exception("齿轮轮廓拉伸失败，草图保留在当前零件中供检查。");
            feature.Name="渐开线齿轮_m"+o.Module+"_z"+o.Teeth;
            model.EditRebuild3();
            Array bodies=((PartDoc)model).GetBodies2(0,false) as Array;
            if(bodies==null || bodies.Length!=1)throw new Exception("齿轮必须形成一个完整实体。");
            Body2 body=(Body2)bodies.GetValue(0);
            double[] mass=body.GetMassProperties(1) as double[];
            if(mass==null || mass[3]<=0)throw new Exception("齿轮实体体积无效。");
            if(o.Chamfer>0)
            {
                model.ClearSelection2(true);
                int selected=0;
                foreach(Face2 face in (object[])body.GetFaces())
                {
                    double[] normal=face.Normal as double[];
                    if(normal!=null && Math.Abs(normal[2])>0.999 && ((Entity)face).Select4(true,null))selected++;
                }
                if(selected!=2)throw new Exception("无法识别齿轮的两个端面，未执行倒角。");
                Feature chamfer=model.FeatureManager.InsertFeatureChamfer(4,1,o.Chamfer/1000,Math.PI/4,0,0,0,0);
                if(chamfer==null)throw new Exception("端面倒角失败。请减小倒角尺寸或设为 0；未保存不完整结果。");
                chamfer.Name="双端面45度倒角";
            }
            BuildResult result=SaveModel(model,"齿轮_m"+o.Module.ToString(CultureInfo.InvariantCulture)+"_z"+o.Teeth,folder,step,mf);
            File.WriteAllText(Path.ChangeExtension(result.PartPath,".parameters.txt"),o.Description()+"\r\n渐开线采用25点样条拟合，齿根采用圆弧+径向过渡；不模拟滚刀根切或配对啮合。",new UTF8Encoding(true));
            result.Message+="\r\n\r\n"+o.Description();
            return result;
#pragma warning restore 0162
        }
    }
}
