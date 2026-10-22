using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;

namespace SolidWorksPrimitiveBuilderV2
{
    internal sealed class PolyhedronOptions
    {
        public int Faces=4;
        public void Validate()
        {
            if(Faces!=4&&Faces!=6&&Faces!=8)throw new ArgumentException("目前支持正四面体、正六面体（立方体）和正八面体。");
        }
        public string DisplayName { get { return Faces==4?"正四面体":Faces==6?"正六面体（立方体）":"正八面体"; } }
    }

    internal sealed class HelicalGearOptions
    {
        public double NormalModule=2, NormalPressure=20, Width=16, Bore=10, HelixAngle=20, Shift=0, Addendum=1, Clearance=.25;
        public int Teeth=24; public bool LeftHand;
        public double TransverseModule { get { return NormalModule/Math.Cos(HelixAngle*Math.PI/180); } }
        public double TransversePressure { get { return Math.Atan(Math.Tan(NormalPressure*Math.PI/180)/Math.Cos(HelixAngle*Math.PI/180))*180/Math.PI; } }
        public double PitchDiameter { get { return TransverseModule*Teeth; } }
        public double Twist { get { return (LeftHand?-1:1)*2*Width*Math.Tan(HelixAngle*Math.PI/180)/PitchDiameter; } }
        public void Validate()
        {
            foreach(double v in new[]{NormalModule,NormalPressure,Width,Bore,HelixAngle,Shift,Addendum,Clearance}) if(double.IsNaN(v)||double.IsInfinity(v))throw new ArgumentException("参数必须为有限数字。");
            if(NormalModule<.2||NormalModule>20||Teeth<10||Teeth>100||NormalPressure<14.5||NormalPressure>25)throw new ArgumentException("范围：法向模数 0.2–20 mm；齿数 10–100；法向压力角 14.5–25°。");
            if(Width<1||Width>160||HelixAngle<5||HelixAngle>45||Shift<-.5||Shift>1)throw new ArgumentException("范围：齿宽 1–160 mm；螺旋角 5–45°；变位系数 -0.5–1。");
            Gear().Validate();
            if(Math.Abs(Twist)>Math.PI*.72)throw new ArgumentException("当前齿宽与螺旋角产生的总扭转过大。请减小齿宽或螺旋角，使跨齿宽扭转小于约 130°。");
        }
        public GearOptions Gear()
        {
            return new GearOptions{Module=TransverseModule,Teeth=Teeth,Pressure=TransversePressure,Width=Width,Bore=Bore,Shift=Shift,Addendum=Addendum,Clearance=Clearance,RootFillet=0,Chamfer=0};
        }
        public string Description()
        {
            return string.Format(CultureInfo.InvariantCulture,"法向模数={0} mm; 齿数={1}; 法向压力角={2} deg; 齿宽={3} mm; 轴孔={4} mm\r\n螺旋角={5} deg; 旋向={6}; 变位系数={7}; 齿顶高系数={8}; 顶隙系数={9}\r\n端面模数={10:F5} mm; 端面压力角={11:F5} deg; 分度圆直径={12:F5} mm; 跨齿宽扭转={13:F5} deg",NormalModule,Teeth,NormalPressure,Width,Bore,HelixAngle,LeftHand?"左旋":"右旋",Shift,Addendum,Clearance,TransverseModule,TransversePressure,PitchDiameter,Twist*180/Math.PI);
        }
    }

    internal sealed class WormSetOptions
    {
        public double Module=2, Pressure=20, WormLength=40, DiameterFactor=10, WheelWidth=14, WheelBore=12;
        public int Starts=2, WheelTeeth=40; public bool LeftHand;
        public double WormPitchDiameter { get { return Module*DiameterFactor; } }
        public double WheelPitchDiameter { get { return Module*WheelTeeth; } }
        public double LeadAngle { get { return Math.Atan((double)Starts/DiameterFactor)*180/Math.PI; } }
        public double CenterDistance { get { return (WormPitchDiameter+WheelPitchDiameter)/2; } }
        public double Ratio { get { return (double)WheelTeeth/Starts; } }
        public void Validate()
        {
            ValidateWorm(); ValidateWheel();
        }
        public void ValidateWorm()
        {
            foreach(double v in new[]{Module,Pressure,WormLength,DiameterFactor,WheelWidth,WheelBore})if(double.IsNaN(v)||double.IsInfinity(v))throw new ArgumentException("参数必须为有限数字。");
            if(Module<.5||Module>10||Starts<1||Starts>4)throw new ArgumentException("范围：模数 0.5–10 mm；蜗杆头数 1–4。");
            if(Pressure<14.5||Pressure>25||DiameterFactor<7||DiameterFactor>18||WormLength<8||WormLength>100)throw new ArgumentException("请检查压力角、直径系数和蜗杆有效长度是否在允许范围内。");
            if(WormLength/(Math.PI*Module*Starts)>4.2)throw new ArgumentException("蜗杆有效长度超过 4.2 圈的当前建模上限，请增大模数/头数或缩短长度。");
        }
        public void ValidateWheel()
        {
            foreach(double v in new[]{Module,Pressure,DiameterFactor,WheelWidth,WheelBore})if(double.IsNaN(v)||double.IsInfinity(v))throw new ArgumentException("参数必须为有限数字。");
            if(Module<.5||Module>10||Starts<1||Starts>4||WheelTeeth<18||WheelTeeth>80)throw new ArgumentException("范围：模数 0.5–10 mm；配对蜗杆头数 1–4；蜗轮齿数 18–80。");
            if(Pressure<14.5||Pressure>25||DiameterFactor<7||DiameterFactor>18||WheelWidth<3||WheelWidth>80)throw new ArgumentException("请检查压力角、直径系数和蜗轮齿宽是否在允许范围内。");
            GearOptions wheel=WheelGear(); wheel.Validate();
        }
        public GearOptions WheelGear()
        {
            double beta=LeadAngle*Math.PI/180;
            return new GearOptions{Module=Module/Math.Cos(beta),Teeth=WheelTeeth,Pressure=Math.Atan(Math.Tan(Pressure*Math.PI/180)/Math.Cos(beta))*180/Math.PI,Width=WheelWidth,Bore=WheelBore,Shift=0,Addendum=1,Clearance=.25,RootFillet=0,Chamfer=0};
        }
        public string Description()
        {
            return string.Format(CultureInfo.InvariantCulture,"模数={0} mm; 压力角={1} deg; 蜗杆头数={2}; 蜗轮齿数={3}; 传动比={4:F4}\r\n蜗杆分度圆={5:F4} mm; 蜗杆长度={6} mm; 蜗轮分度圆={7:F4} mm; 蜗轮齿宽={8} mm; 蜗轮孔径={9} mm\r\n直径系数 q={10}; 导程角={11:F4} deg; 理论中心距={12:F4} mm; 旋向={13}",Module,Pressure,Starts,WheelTeeth,Ratio,WormPitchDiameter,WormLength,WheelPitchDiameter,WheelWidth,WheelBore,DiameterFactor,LeadAngle,CenterDistance,LeftHand?"左旋":"右旋");
        }
    }

    internal abstract class DarkParameterDialog : Form
    {
        protected readonly List<NumericUpDown> N = new List<NumericUpDown>();
        protected readonly Panel body = new Panel();
        protected DarkParameterDialog(string title, string sub, int height)
        {
            Text=title; ClientSize=new Size(710,height); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
            Font=new Font("Microsoft YaHei UI",10); BackColor=Color.FromArgb(7,11,29); ForeColor=Color.FromArgb(232,239,255);
            UiRounder.PreferRoundedWindow(this);
            try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
            Controls.Add(new Label{Text=title,Font=new Font(Font.FontFamily,18,FontStyle.Bold),ForeColor=Color.White,AutoSize=true,Location=new Point(28,22)});
            Controls.Add(new Label{Text=sub,ForeColor=Color.FromArgb(139,159,202),AutoSize=true,Location=new Point(31,63)});
            body.Location=new Point(25,96);body.Size=new Size(660,height-180);body.BackColor=Color.FromArgb(17,25,51);UiRounder.Apply(body,16);Controls.Add(body);
        }
        protected NumericUpDown Field(string name, decimal value, decimal min, decimal max, decimal inc, int index, int decimals=3)
        {
            int col=index%2,row=index/2,x=22+col*320,y=18+row*58;
            body.Controls.Add(new Label{Text=name,ForeColor=Color.FromArgb(190,205,235),AutoSize=true,Location=new Point(x,y+5)});
            var n=new NumericUpDown{Value=value,Minimum=min,Maximum=max,Increment=inc,DecimalPlaces=decimals,Location=new Point(x+170,y),Width=120,BackColor=Color.FromArgb(28,38,69),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};
            UiRounder.Apply(n,7);
            body.Controls.Add(n);N.Add(n);return n;
        }
        protected Button Action(string text, EventHandler click)
        {
            var b=new Button{Text=text,Location=new Point(480,ClientSize.Height-65),Size=new Size(200,40),BackColor=Color.FromArgb(92,83,238),ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font(Font.FontFamily,10,FontStyle.Bold)};
            b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Color.FromArgb(128,117,255);UiRounder.Apply(b,10);
            b.MouseEnter+=delegate{b.FlatAppearance.BorderSize=2;b.FlatAppearance.BorderColor=Color.FromArgb(91,226,255);b.BackColor=Color.FromArgb(117,91,255);};
            b.MouseLeave+=delegate{b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Color.FromArgb(128,117,255);b.BackColor=Color.FromArgb(92,83,238);};
            b.Click+=click;Controls.Add(b);AcceptButton=b;return b;
        }
        protected void Note(string text,int y){body.Controls.Add(new Label{Text=text,ForeColor=Color.FromArgb(123,207,255),Location=new Point(22,y),Size=new Size(610,48)});}
    }

    internal sealed class PolyhedronDialog : DarkParameterDialog
    {
        private readonly ComboBox faces=new ComboBox(); public PolyhedronOptions Options;
        internal PolyhedronDialog():base("正多面体","选择原生模板的面数 · 严格正多面体",310)
        {
            body.Controls.Add(new Label{Text="目标形体",ForeColor=Color.FromArgb(190,205,235),AutoSize=true,Location=new Point(22,25)});
            faces.DropDownStyle=ComboBoxStyle.DropDownList;faces.Items.AddRange(new object[]{"4 面 · 正四面体","6 面 · 正六面体（立方体）","8 面 · 正八面体"});faces.SelectedIndex=0;
            faces.Location=new Point(175,19);faces.Size=new Size(260,30);faces.BackColor=Color.FromArgb(28,38,69);faces.ForeColor=Color.White;body.Controls.Add(faces);
            Note("提示：严格的凸正多面体只有 4 / 6 / 8 / 12 / 20 面；不存在“正五面体”。本版按你的范围提供 4、6、8 面。",82);
            Action("创建正多面体",delegate{Options=new PolyhedronOptions{Faces=faces.SelectedIndex==0?4:faces.SelectedIndex==1?6:8};Options.Validate();DialogResult=DialogResult.OK;});
        }
    }

    internal sealed class HelicalGearDialog : DarkParameterDialog
    {
        private readonly CheckBox left=new CheckBox(); public HelicalGearOptions Options;
        internal HelicalGearDialog():base("渐开线斜齿轮","法向制参数 · 扭转放样实体 · mm / °",550)
        {
            Field("法向模数 mn",2,.2m,20,.1m,0);Field("齿数 z",24,10,100,1,1,0);Field("法向压力角",20,14.5m,25,.5m,2);Field("齿宽 b",16,1,160,1,3);
            Field("轴孔直径",10,0,500,1,4);Field("螺旋角 β",20,5,45,1,5);Field("变位系数 x",0,-.5m,1,.05m,6);Field("齿顶高系数",1,.5m,1.25m,.05m,7);Field("顶隙系数",.25m,.1m,.5m,.05m,8);
            left.Text="左旋（不勾选为右旋）";left.Location=new Point(360,257);left.AutoSize=true;left.ForeColor=Color.White;body.Controls.Add(left);
            Note("采用起止渐开线齿形的连续扭转放样；齿面为单一原生实体，不再使用分层拉伸。",320);
            Action("创建斜齿轮",delegate{try{var o=Read();o.Validate();Options=o;DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,ex.Message,"参数需要调整");}});
        }
        private HelicalGearOptions Read(){return new HelicalGearOptions{NormalModule=(double)N[0].Value,Teeth=(int)N[1].Value,NormalPressure=(double)N[2].Value,Width=(double)N[3].Value,Bore=(double)N[4].Value,HelixAngle=(double)N[5].Value,Shift=(double)N[6].Value,Addendum=(double)N[7].Value,Clearance=(double)N[8].Value,LeftHand=left.Checked};}
    }

    internal sealed class WormSetDialog : DarkParameterDialog
    {
        private readonly CheckBox left=new CheckBox(); private readonly Label summary=new Label(); public WormSetOptions Options;
        internal WormSetDialog():base("蜗轮蜗杆套件","一次生成蜗杆与蜗轮两个零件 · mm / °",575)
        {
            Field("轴向模数 m",2,.5m,10,.1m,0);Field("压力角",20,14.5m,25,.5m,1);Field("蜗杆头数",2,1,4,1,2,0);Field("蜗轮齿数",40,18,80,1,3,0);
            Field("蜗杆有效长度",40,8,100,1,4);Field("直径系数 q",10,7,18,.5m,5);Field("蜗轮齿宽",14,3,80,1,6);Field("蜗轮轴孔",12,0,500,1,7);
            left.Text="左旋（不勾选为右旋）";left.Location=new Point(32,252);left.AutoSize=true;left.ForeColor=Color.White;body.Controls.Add(left);
            summary.Location=new Point(22,295);summary.Size=new Size(610,48);summary.ForeColor=Color.FromArgb(123,207,255);body.Controls.Add(summary);
            foreach(var n in N)n.ValueChanged+=delegate{UpdateSummary();};
            Note("蜗杆采用连续旋转截面的原生放样；蜗轮采用与导程角匹配的连续扭转齿面。输出两个 SLDPRT 及配对参数记录。",350);UpdateSummary();
            Action("创建蜗轮蜗杆",delegate{try{var o=Read();o.Validate();Options=o;DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,ex.Message,"参数需要调整");}});
        }
        private WormSetOptions Read(){return new WormSetOptions{Module=(double)N[0].Value,Pressure=(double)N[1].Value,Starts=(int)N[2].Value,WheelTeeth=(int)N[3].Value,WormLength=(double)N[4].Value,DiameterFactor=(double)N[5].Value,WheelWidth=(double)N[6].Value,WheelBore=(double)N[7].Value,LeftHand=left.Checked};}
        private void UpdateSummary(){var o=Read();summary.Text=string.Format("传动比  {0:F2}:1     导程角  {1:F2}°\n理论中心距  {2:F3} mm",o.Ratio,o.LeadAngle,o.CenterDistance);}
    }

    internal sealed class WormDialog : DarkParameterDialog
    {
        private readonly CheckBox left=new CheckBox(); private readonly Label summary=new Label(); public WormSetOptions Options;
        internal WormDialog():base("蜗杆零件","独立生成一个蜗杆零件 · mm / °",505)
        {
            Field("轴向模数 m",2,.5m,10,.1m,0);Field("压力角",20,14.5m,25,.5m,1);Field("蜗杆头数",2,1,4,1,2,0);Field("有效长度",40,8,100,1,3);Field("直径系数 q",10,7,18,.5m,4);
            left.Text="左旋（不勾选为右旋）";left.Location=new Point(360,135);left.AutoSize=true;left.ForeColor=Color.White;body.Controls.Add(left);
            summary.Location=new Point(22,198);summary.Size=new Size(610,42);summary.ForeColor=Color.FromArgb(123,207,255);body.Controls.Add(summary);
            foreach(var n in N)n.ValueChanged+=delegate{UpdateSummary();};
            Note("仅生成蜗杆，不再同时生成蜗轮。连续旋转截面放样形成单一平滑实体。",255);UpdateSummary();
            Action("创建蜗杆零件",delegate{try{var o=Read();o.ValidateWorm();Options=o;DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,ex.Message,"参数需要调整");}});
        }
        private WormSetOptions Read(){return new WormSetOptions{Module=(double)N[0].Value,Pressure=(double)N[1].Value,Starts=(int)N[2].Value,WormLength=(double)N[3].Value,DiameterFactor=(double)N[4].Value,LeftHand=left.Checked};}
        private void UpdateSummary(){var o=Read();summary.Text=string.Format("分度圆 Ø {0:F3} mm     导程角 {1:F3}°     旋向 {2}",o.WormPitchDiameter,o.LeadAngle,o.LeftHand?"左旋":"右旋");}
    }

    internal sealed class WormWheelDialog : DarkParameterDialog
    {
        private readonly CheckBox left=new CheckBox(); private readonly Label summary=new Label(); public WormSetOptions Options;
        internal WormWheelDialog():base("蜗轮零件","独立生成一个蜗轮零件 · 输入配对蜗杆参数",535)
        {
            Field("模数 m",2,.5m,10,.1m,0);Field("压力角",20,14.5m,25,.5m,1);Field("蜗轮齿数",40,18,80,1,2,0);Field("蜗轮齿宽",14,3,80,1,3);Field("轴孔直径",12,0,500,1,4);Field("配对蜗杆头数",2,1,4,1,5,0);Field("配对直径系数 q",10,7,18,.5m,6);
            left.Text="配对蜗杆为左旋";left.Location=new Point(360,194);left.AutoSize=true;left.ForeColor=Color.White;body.Controls.Add(left);
            summary.Location=new Point(22,252);summary.Size=new Size(610,48);summary.ForeColor=Color.FromArgb(123,207,255);body.Controls.Add(summary);
            foreach(var n in N)n.ValueChanged+=delegate{UpdateSummary();};
            Note("仅生成蜗轮，不再同时生成蜗杆。配对头数和直径系数用于计算导程角、旋向与理论中心距。",315);UpdateSummary();
            Action("创建蜗轮零件",delegate{try{var o=Read();o.ValidateWheel();Options=o;DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,ex.Message,"参数需要调整");}});
        }
        private WormSetOptions Read(){return new WormSetOptions{Module=(double)N[0].Value,Pressure=(double)N[1].Value,WheelTeeth=(int)N[2].Value,WheelWidth=(double)N[3].Value,WheelBore=(double)N[4].Value,Starts=(int)N[5].Value,DiameterFactor=(double)N[6].Value,LeftHand=left.Checked};}
        private void UpdateSummary(){var o=Read();summary.Text=string.Format("蜗轮分度圆 Ø {0:F3} mm     导程角 {1:F3}°\n理论中心距 {2:F3} mm     传动比 {3:F2}:1",o.WheelPitchDiameter,o.LeadAngle,o.CenterDistance,o.Ratio);}
    }

    internal static partial class SolidWorksBuilder
    {
        internal static string InspectTemplateEquations(string path)
        {
            SldWorks app=Connect();int errors=0,warnings=0;ModelDoc2 model=app.OpenDoc6(path,1,0,"",ref errors,ref warnings);
            if(model==null)throw new Exception("无法打开模板（错误代码："+errors+"）。");
            EquationMgr equations=model.GetEquationMgr();var rows=new List<string>();
            for(int i=0;i<equations.GetCount();i++)rows.Add(equations.get_Equation(i));
            return String.Join("\r\n",rows.ToArray());
        }

        internal static bool SetGlobalEquation(EquationMgr equations,string name,string value)
        {
            string target="\""+name+"\"";
            for(int i=0;i<equations.GetCount();i++)
            {
                string equation=equations.get_Equation(i);int equals=equation==null?-1:equation.IndexOf('=');
                if(equals<0||!String.Equals(equation.Substring(0,equals).Trim(),target,StringComparison.OrdinalIgnoreCase))continue;
                equations.set_Equation(i,target+" = "+value);return true;
            }
            return false;
        }

        private static ModelDoc2 BuildEquationDrivenHelicalGear(SldWorks app,HelicalGearOptions o)
        {
            string master=Path.Combine(Path.GetDirectoryName(typeof(SolidWorksBuilder).Assembly.Location),"圆柱斜齿轮左旋方程母版.SLDPRT");
            if(!File.Exists(master))throw new FileNotFoundException("安装包中的圆柱斜齿轮方程母版缺失。请重新运行安装程序。",master);
            int errors=0,warnings=0;ModelDoc2 model=app.OpenDoc6(master,1,0,"",ref errors,ref warnings);
            if(model==null)throw new Exception("无法打开斜齿轮方程母版（SOLIDWORKS 错误代码："+errors+"）。");
            EquationMgr equations=model.GetEquationMgr();
            // 变量及公式来源于用户提供的圆柱斜齿轮左旋母版：分度圆、基圆、齿顶圆和齿根圆
            // 均由法向模数、齿数、压力角、螺旋角和变位参数联动计算。
            var values=new Dictionary<string,string>{
                {"a",o.NormalPressure.ToString(CultureInfo.InvariantCulture)},
                {"x",o.Shift.ToString(CultureInfo.InvariantCulture)},
                {"mn",o.NormalModule.ToString(CultureInfo.InvariantCulture)},
                {"β",(o.LeftHand?-o.HelixAngle:o.HelixAngle).ToString(CultureInfo.InvariantCulture)},
                {"C1","1"},
                {"c",o.Clearance.ToString(CultureInfo.InvariantCulture)},
                {"B",o.Width.ToString(CultureInfo.InvariantCulture)},
                {"z",o.Teeth.ToString(CultureInfo.InvariantCulture)},
                {"R",Math.Max(.001,o.Gear().RootFillet).ToString(CultureInfo.InvariantCulture)},
                {"ha",o.Addendum.ToString(CultureInfo.InvariantCulture)}
            };
            var missing=new List<string>();foreach(var pair in values)if(!SetGlobalEquation(equations,pair.Key,pair.Value))missing.Add(pair.Key);
            if(missing.Count>0)throw new Exception("斜齿轮母版缺少全局变量："+String.Join("、",missing.ToArray())+"。已停止保存，避免生成错误模型。");
            model.EditRebuild3();return model;
        }

        private static Feature CreateProfileSketch(ModelDoc2 model, Feature basePlane, GearOptions gear, double z, double rotation, string name, bool includeBore)
        {
            model.ClearSelection2(true); basePlane.Select2(false,0);
            if(z>1e-10)
            {
                object plane=model.FeatureManager.InsertRefPlane(8,z/1000,0,0,0,0);
                if(plane==null)throw new Exception("斜齿轮截面基准面创建失败。");
                Feature planeFeature=LastReferencePlane(model);
                model.ClearSelection2(true);
                if(planeFeature==null||!planeFeature.Select2(false,0))throw new Exception("斜齿轮截面基准面选择失败。");
            }
            SketchManager sm=model.SketchManager;sm.InsertSketch(true);Sketch sketch=sm.ActiveSketch;if(sketch==null)throw new Exception("无法在截面基准面上创建草图（Z="+z.ToString("F3",CultureInfo.InvariantCulture)+" mm）。");
            sm.AddToDB=true;try{DrawGearProfile(sm,gear,rotation,includeBore);}finally{sm.AddToDB=false;}sm.InsertSketch(true);
            Feature f=(Feature)sketch;f.Name=name;return f;
        }

        private static Feature ExtrudeProfile(ModelDoc2 model,Feature sketch,double depth,string name)
        {
            model.ClearSelection2(true);if(!sketch.Select2(false,0))throw new Exception("截面草图选择失败。");
            Feature feature=model.FeatureManager.FeatureExtrusion2(true,false,false,0,0,depth/1000,0,false,false,false,false,0,0,false,false,false,false,true,true,true,0,0,false);
            if(feature==null)throw new Exception("分层螺旋实体拉伸失败（"+name+"）。请减小螺旋角或增大模数。");feature.Name=name;return feature;
        }

        private static Feature LastReferencePlane(ModelDoc2 model)
        {
            Feature found=null,f=model.FirstFeature();
            while(f!=null){if((string)f.GetTypeName2()=="RefPlane")found=f;f=f.GetNextFeature();}
            return found;
        }

        private static Feature LoftProfiles(ModelDoc2 model,List<Feature> profiles,string name)
        {
            model.ClearSelection2(true);for(int i=0;i<profiles.Count;i++)if(!profiles[i].Select2(i>0,1))throw new Exception("放样截面选择失败："+(i+1));
            Feature loft=model.FeatureManager.InsertProtrusionBlend2(false,true,false,1,0,0,1,1,true,true,false,0,0,0,true,true,true,0);
            if(loft==null)throw new Exception("扭转齿面放样失败。请减小齿数、齿宽或螺旋角后重试。");loft.Name=name;return loft;
        }

        private static void CutBore(ModelDoc2 model,Feature basePlane,double diameter,double depth)
        {
            if(diameter<=0)return;model.ClearSelection2(true);basePlane.Select2(false,0);SketchManager sm=model.SketchManager;sm.InsertSketch(true);Sketch sketch=sm.ActiveSketch;
            if(sm.CreateCircleByRadius(0,0,0,diameter/2000)==null)throw new Exception("轴孔草图创建失败。");sm.InsertSketch(true);model.ClearSelection2(true);((Feature)sketch).Select2(false,0);
            Feature cut=model.FeatureManager.FeatureCut4(true,false,true,1,0,depth/1000*1.2,0,false,false,false,false,0,0,false,false,false,false,false,true,true,false,false,false,0,0,false,true);
            if(cut==null)throw new Exception("轴孔切除失败。请减小孔径或设为 0。");cut.Name="贯穿轴孔";
        }

        private static ModelDoc2 BuildTwistedGear(SldWorks app,GearOptions gear,double width,double twist,string featureName)
        {
            ModelDoc2 model=app.NewDocument(FindTemplate(app),0,0,0);if(model==null)throw new Exception("无法新建斜齿轮零件。");Feature basePlane=SelectPlane(model);
            // 两端同拓扑渐开线轮廓经原生放样连接，SOLIDWORKS 生成连续扭转齿面，
            // 而非把多个短拉伸体堆叠成“切片”。轴孔在实体完成后统一贯穿切除。
            var profiles=new List<Feature>();
            profiles.Add(CreateProfileSketch(model,basePlane,gear,0,0,"齿形起始截面",false));
            profiles.Add(CreateProfileSketch(model,basePlane,gear,width,twist,"齿形终止截面",false));
            LoftProfiles(model,profiles,featureName+"连续扭转放样");
            CutBore(model,basePlane,gear.Bore,width);
            model.EditRebuild3();EnsureSingleBody(model,featureName);return model;
        }

        private static void EnsureSingleBody(ModelDoc2 model,string label)
        {
            Array bodies=((PartDoc)model).GetBodies2(0,false) as Array;if(bodies==null||bodies.Length!=1)throw new Exception(label+"没有形成单一实体。");
            double[] mass=((Body2)bodies.GetValue(0)).GetMassProperties(1) as double[];if(mass==null||mass[3]<=0)throw new Exception(label+"实体体积无效。");
        }

        internal static BuildResult CreateHelicalGear(HelicalGearOptions o,string folder,bool step,bool mf)
        {
            o.Validate();SldWorks app=Connect();ModelDoc2 model=BuildEquationDrivenHelicalGear(app,o);
            string hand=o.LeftHand?"左旋":"右旋";BuildResult result=SaveModel(model,"斜齿轮_"+hand+"_mn"+o.NormalModule.ToString(CultureInfo.InvariantCulture)+"_z"+o.Teeth,folder,step,mf);
            File.WriteAllText(Path.ChangeExtension(result.PartPath,".parameters.txt"),o.Description()+"\r\n基于安装包内置的圆柱斜齿轮左旋方程母版：分度圆、基圆、齿根圆与齿顶圆由全局变量联动；不含承载校核。",new UTF8Encoding(true));result.Message+="\r\n\r\n"+o.Description();return result;
        }

        private static Feature CreateWormProfile(ModelDoc2 model,Feature basePlane,double z,double rotation,WormSetOptions o,int index)
        {
            model.ClearSelection2(true);basePlane.Select2(false,0);if(z>1e-10){if(model.FeatureManager.InsertRefPlane(8,z/1000,0,0,0,0)==null)throw new Exception("蜗杆截面基准面创建失败。");Feature planeFeature=LastReferencePlane(model);model.ClearSelection2(true);if(planeFeature==null||!planeFeature.Select2(false,0))throw new Exception("蜗杆截面基准面选择失败。");}
            SketchManager sm=model.SketchManager;sm.InsertSketch(true);Sketch sketch=sm.ActiveSketch;int count=72;P[] pts=new P[count];
            double pitchR=o.WormPitchDiameter/2,root=Math.Max(o.Module*1.2,pitchR-o.Module*1.1),tip=pitchR+o.Module;
            sm.AddToDB=true;
            try
            {
                for(int i=0;i<count;i++){double local=2*Math.PI*i/count,a=local+rotation;double wave=.5+.5*Math.Cos(o.Starts*local);double r=root+(tip-root)*Math.Pow(wave,4);pts[i]=P.Polar(r,a);}
                for(int i=0;i<count;i++)GearLine(sm,pts[i],pts[(i+1)%count]);
            }
            finally{sm.AddToDB=false;}
            sm.InsertSketch(true);Feature f=(Feature)sketch;f.Name="蜗杆截面_"+index;return f;
        }

        private static ModelDoc2 BuildWorm(SldWorks app,WormSetOptions o)
        {
            ModelDoc2 model=app.NewDocument(FindTemplate(app),0,0,0);if(model==null)throw new Exception("无法新建蜗杆零件。");Feature basePlane=SelectPlane(model);
            double revolutions=o.WormLength/(Math.PI*o.Module*o.Starts),total=(o.LeftHand?-1:1)*revolutions*2*Math.PI;
            // 以等距旋转截面做一次连续实体放样。中间截面只用于约束螺旋路径，
            // 最终特征只有一个连续实体，不产生层间接缝。
            int sections=Math.Max(5,(int)Math.Ceiling(Math.Abs(total)*180/Math.PI/45)+1);sections=Math.Min(sections,17);
            var profiles=new List<Feature>();
            for(int i=0;i<sections;i++)
            {
                double f=(double)i/(sections-1);
                profiles.Add(CreateWormProfile(model,basePlane,o.WormLength*f,total*f,o,i+1));
            }
            LoftProfiles(model,profiles,"蜗杆连续螺旋放样");
            model.EditRebuild3();EnsureSingleBody(model,"蜗杆");return model;
        }

        internal static BuildResult CreateWormSet(WormSetOptions o,string folder,bool step,bool mf)
        {
            o.Validate();SldWorks app=Connect();ModelDoc2 worm=BuildWorm(app,o);BuildResult wr=SaveModel(worm,"蜗杆_m"+o.Module.ToString(CultureInfo.InvariantCulture)+"_头数"+o.Starts,folder,step,mf);
            double beta=o.LeadAngle*Math.PI/180*(o.LeftHand?1:-1);ModelDoc2 wheel=BuildTwistedGear(app,o.WheelGear(),o.WheelWidth,2*o.WheelWidth*Math.Tan(beta)/(o.WheelPitchDiameter),"蜗轮近似齿面");
            BuildResult wh=SaveModel(wheel,"蜗轮_m"+o.Module.ToString(CultureInfo.InvariantCulture)+"_z"+o.WheelTeeth,folder,step,mf);
            string parameter=Path.Combine(folder,"蜗轮蜗杆配对参数_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".txt");File.WriteAllText(parameter,o.Description()+"\r\n\r\n蜗杆与蜗轮均为结构/外观级工程近似；未生成滚刀包络、齿面修形、侧隙、接触斑点或强度校核。",new UTF8Encoding(true));
            return new BuildResult{PartPath=wr.PartPath,Message="蜗杆：\r\n"+wr.PartPath+"\r\n\r\n蜗轮：\r\n"+wh.PartPath+"\r\n\r\n配对参数：\r\n"+parameter+"\r\n\r\n"+o.Description()};
        }

        internal static BuildResult CreateWormPart(WormSetOptions o,string folder,bool step,bool mf)
        {
            o.ValidateWorm();SldWorks app=Connect();ModelDoc2 worm=BuildWorm(app,o);BuildResult result=SaveModel(worm,"蜗杆_m"+o.Module.ToString(CultureInfo.InvariantCulture)+"_头数"+o.Starts,folder,step,mf);
            string note=string.Format(CultureInfo.InvariantCulture,"模数={0} mm; 压力角={1} deg; 头数={2}; 有效长度={3} mm; 直径系数 q={4}; 分度圆直径={5:F4} mm; 导程角={6:F4} deg; 旋向={7}",o.Module,o.Pressure,o.Starts,o.WormLength,o.DiameterFactor,o.WormPitchDiameter,o.LeadAngle,o.LeftHand?"左旋":"右旋");
            File.WriteAllText(Path.ChangeExtension(result.PartPath,".parameters.txt"),note+"\r\n连续螺旋截面放样实体，不含齿面修形、侧隙与强度校核。",new UTF8Encoding(true));result.Message+="\r\n\r\n"+note;return result;
        }

        internal static BuildResult CreateWormWheelPart(WormSetOptions o,string folder,bool step,bool mf)
        {
            o.ValidateWheel();SldWorks app=Connect();double beta=o.LeadAngle*Math.PI/180*(o.LeftHand?1:-1);ModelDoc2 wheel=BuildTwistedGear(app,o.WheelGear(),o.WheelWidth,2*o.WheelWidth*Math.Tan(beta)/(o.WheelPitchDiameter),"蜗轮近似齿面");
            BuildResult result=SaveModel(wheel,"蜗轮_m"+o.Module.ToString(CultureInfo.InvariantCulture)+"_z"+o.WheelTeeth,folder,step,mf);
            string note=string.Format(CultureInfo.InvariantCulture,"模数={0} mm; 压力角={1} deg; 蜗轮齿数={2}; 齿宽={3} mm; 孔径={4} mm; 配对蜗杆头数={5}; q={6}; 导程角={7:F4} deg; 理论中心距={8:F4} mm; 传动比={9:F4}",o.Module,o.Pressure,o.WheelTeeth,o.WheelWidth,o.WheelBore,o.Starts,o.DiameterFactor,o.LeadAngle,o.CenterDistance,o.Ratio);
            File.WriteAllText(Path.ChangeExtension(result.PartPath,".parameters.txt"),note+"\r\n与导程角匹配的连续扭转放样齿面，不含滚刀包络、齿面修形、侧隙与强度校核。",new UTF8Encoding(true));result.Message+="\r\n\r\n"+note;return result;
        }
    }
}
