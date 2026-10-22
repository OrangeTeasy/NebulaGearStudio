// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;

namespace SolidWorksPrimitiveBuilderV2
{
    internal sealed class MasterPartOptions
    {
        internal string FileName;
        internal string DisplayName;
        internal string Equations;
    }

    internal static class MasterPartCatalog
    {
        internal sealed class Entry { internal readonly string Name, File; internal Entry(string name,string file) { Name=name; File=file; } }
        private static readonly Entry[] entries = {
            new Entry("圆柱直齿轮","圆柱直齿轮.SLDPRT"), new Entry("圆柱斜齿轮（右旋）","圆柱斜齿轮右旋.SLDPRT"), new Entry("圆柱斜齿轮（左旋）","圆柱斜齿轮左旋.SLDPRT"),
            new Entry("直齿圈","直齿圈.SLDPRT"), new Entry("斜齿圈（右旋）","斜齿圈右旋.SLDPRT"), new Entry("斜齿圈（左旋）","斜齿圈左旋.SLDPRT"),
            new Entry("锥齿轮","锥齿轮.SLDPRT"), new Entry("蜗杆","涡杆.SLDPRT"), new Entry("蜗轮（右旋）","涡轮右旋.SLDPRT"), new Entry("蜗轮（左旋）","涡轮左旋.SLDPRT"),
            new Entry("压缩弹簧","压缩弹簧.SLDPRT"), new Entry("拉伸弹簧","拉伸弹簧.SLDPRT"), new Entry("扭转弹簧","扭转弹簧.SLDPRT"),
            new Entry("截锥弹簧（无紧固圈）","截锥弹簧无紧固圈.SLDPRT"), new Entry("截锥弹簧（有紧固圈）","截锥弹簧有紧固圈.SLDPRT")
        };
        internal static object[] ShapeNames
        {
            get
            {
                var names=new object[8+entries.Length];
                string[] basic={"长方体","球体","圆柱体","圆锥体","圆台","垫片（公式直建）","轴套（公式直建）","正 n 面体（原生模板）"};
                for(int i=0;i<basic.Length;i++)names[i]=basic[i];
                for(int i=0;i<entries.Length;i++)names[8+i]=entries[i].Name;
                return names;
            }
        }
        internal static Entry For(ShapeKind kind)
        {
            int index=(int)kind-(int)ShapeKind.SpurGear;
            if(index<0||index>=entries.Length)throw new ArgumentOutOfRangeException("kind");
            return entries[index];
        }
    }

    internal sealed class MasterPartDialog : DarkParameterDialog
    {
        private sealed class ParameterRow { internal string Name; internal TextBox Value; }
        private readonly MasterPartCatalog.Entry part;
        private readonly Panel parameterArea = new Panel();
        private readonly Label info = new Label();
        private readonly List<ParameterRow> rows = new List<ParameterRow>();
        private readonly Button action;
        internal MasterPartOptions Options;

        internal MasterPartDialog(ShapeKind kind) : base(MasterPartCatalog.For(kind).Name+" · 参数", "在软件中逐项修改母版参数，随后生成新的 SOLIDWORKS 零件", 690)
        {
            part=MasterPartCatalog.For(kind);
            ClientSize=new Size(850,690);
            body.Size=new Size(800,500);
            info.Location=new Point(20,15);info.Size=new Size(755,32);info.ForeColor=Color.FromArgb(123,207,255);
            info.Text="正在读取 “"+part.Name+"” 的参数…";body.Controls.Add(info);
            parameterArea.Location=new Point(18,53);parameterArea.Size=new Size(764,370);
            parameterArea.AutoScroll=true;parameterArea.BackColor=Color.FromArgb(13,21,45);body.Controls.Add(parameterArea);
            body.Controls.Add(new Label { Text="左侧为参数名，右侧输入数值或公式；未改动的参数保持母版值。", ForeColor=Color.FromArgb(145,171,217), Location=new Point(22,438), Size=new Size(745,43) });
            action=Action("生成新零件",delegate { try { Finish(); } catch(Exception ex) { MessageBox.Show(this,ex.Message,"参数需要调整"); } });
            action.Location=new Point(620,ClientSize.Height-65);action.Enabled=false;
            Shown+=delegate { LoadParameters(); };
        }

        private void LoadParameters()
        {
            var worker=new Thread(delegate()
            {
                string equations=null;Exception failure=null;
                try { equations=SolidWorksBuilder.ReadMasterEquations(part.File); }
                catch(Exception ex) { failure=ex; }
                if(IsDisposed||!IsHandleCreated)return;
                try { BeginInvoke((MethodInvoker)delegate
                {
                    if(IsDisposed)return;
                    if(failure!=null)
                    {
                        info.Text="读取失败：请检查母版版本和安装目录。";
                        MessageBox.Show(this,failure.Message,"母版不可用",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                        DialogResult=DialogResult.Cancel;
                        Close();
                        return;
                    }
                    ShowParameters(equations);
                }); } catch(InvalidOperationException) { }
            });
            worker.SetApartmentState(ApartmentState.STA);worker.IsBackground=true;worker.Start();
        }

        private void ShowParameters(string equations)
        {
            parameterArea.SuspendLayout();
            try
            {
                int y=0;
                using(var reader=new StringReader(equations))
                {
                    string equation;
                    while((equation=reader.ReadLine())!=null)
                    {
                        int equal=equation.IndexOf('=');if(equal<1)continue;
                        string name=equation.Substring(0,equal).Trim();string value=equation.Substring(equal+1).Trim();
                        int commentAt=value.IndexOf('\'');
                        string comment=commentAt<0?String.Empty:value.Substring(commentAt+1).Trim();
                        if(commentAt>=0)value=value.Substring(0,commentAt).Trim();
                        var label=new Label { Text=name.Trim('"')+(comment.Length==0?String.Empty:" · "+comment), Location=new Point(8,y+8), Size=new Size(245,30), AutoEllipsis=true, ForeColor=Color.FromArgb(207,221,250), Font=new Font("Comic Sans MS",10F) };
                        var field=new TextBox { Text=value, Location=new Point(260,y+5), Size=new Size(465,29), BackColor=Color.FromArgb(29,40,73), ForeColor=Color.White, BorderStyle=BorderStyle.FixedSingle, Font=new Font("Comic Sans MS",10F) };
                        parameterArea.Controls.Add(label);parameterArea.Controls.Add(field);
                        rows.Add(new ParameterRow{Name=name,Value=field});y+=42;
                    }
                }
                parameterArea.AutoScrollMinSize=new Size(0,y+8);
                info.Text="已读取 “"+part.Name+"” 的 "+rows.Count+" 项参数；修改右侧输入框后生成零件。";
                action.Enabled=rows.Count>0;
            }
            finally { parameterArea.ResumeLayout(false); }
        }

        private void Finish()
        {
            if(rows.Count==0)throw new ArgumentException("尚未读取到母版参数。");
            var text=new StringBuilder();
            foreach(ParameterRow row in rows)
            {
                string value=row.Value.Text.Trim();
                if(value.Length==0){row.Value.Focus();throw new ArgumentException(row.Name+" 的值不能为空。");}
                text.Append(row.Name).Append(" = ").Append(value).AppendLine();
            }
            Options=new MasterPartOptions{FileName=part.File,DisplayName=part.Name,Equations=text.ToString()};
            DialogResult=DialogResult.OK;
        }
    }

    internal static partial class SolidWorksBuilder
    {
        private static string MasterPath(SldWorks app, string fileName)
        {
            string root = Path.Combine(Path.GetDirectoryName(typeof(SolidWorksBuilder).Assembly.Location), "母版库");
            int year = SolidWorksCompatibility.InstalledYear(app);
            return SolidWorksCompatibility.ResolveMother(root, year, fileName);
        }

        internal static string ReadMasterEquations(string fileName)
        {
            SldWorks app=Connect(); string path=MasterPath(app,fileName); int errors=0,warnings=0;
            ModelDoc2 model=app.GetOpenDocumentByName(path) as ModelDoc2;
            bool openedHere=model==null;
            if(openedHere) model=app.OpenDoc6(path,1,1,"",ref errors,ref warnings);
            if(model==null) throw new Exception("无法读取母版（SOLIDWORKS 错误代码："+errors+"）。");
            try
            {
                SolidWorksCompatibility.RequireEditableMother(model,path);
                EquationMgr manager=model.GetEquationMgr(); var result=new List<string>();
                for(int i=0;i<manager.GetCount();i++) { string row=manager.get_Equation(i); if(!String.IsNullOrWhiteSpace(row) && row.TrimStart().StartsWith("\"")) result.Add(row); }
                if(result.Count==0) throw new Exception("该母版没有可编辑的全局方程。");
                return String.Join(System.Environment.NewLine,result.ToArray());
            }
            finally { if(openedHere) app.CloseDoc(model.GetTitle()); }
        }

        internal static BuildResult CreateFromMaster(MasterPartOptions options,string folder,bool step,bool mf)
        {
            if(options==null || String.IsNullOrWhiteSpace(options.FileName)) throw new ArgumentException("未选择母版。");
            SldWorks app=Connect(); string path=MasterPath(app,options.FileName);
            var requested=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            using(StringReader reader=new StringReader(options.Equations??String.Empty))
            {
                string line; while((line=reader.ReadLine())!=null)
                {
                    int at=line.IndexOf('=');
                    if(at<1||!line.TrimStart().StartsWith("\""))continue;
                    string name=line.Substring(0,at).Trim();string value=EquationExpression(line.Substring(at+1));
                    if(value.Length==0)throw new ArgumentException(name+" 的参数值不能为空。");
                    if(requested.ContainsKey(name))throw new ArgumentException("参数名重复："+name);
                    requested.Add(name,value);
                }
            }
            if(requested.Count==0)throw new ArgumentException("没有可写入的参数。");
            Directory.CreateDirectory(folder);
            string basePath=Path.Combine(folder,options.DisplayName+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+"_"+Guid.NewGuid().ToString("N").Substring(0,6));
            string partPath=basePath+".SLDPRT";
            // 先复制母版再打开复制件：用户在 SOLIDWORKS 中打开的原始母版不会被本次参数修改污染。
            File.Copy(path,partPath,false);
            int errors=0,warnings=0; ModelDoc2 model=app.OpenDoc6(partPath,1,1,"",ref errors,ref warnings);
            if(model==null) throw new Exception("无法打开母版副本（SOLIDWORKS 错误代码："+errors+"）。");
            SolidWorksCompatibility.RequireEditableMother(model,path);
            EquationMgr manager=model.GetEquationMgr();var found=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changed=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(KeyValuePair<string,string> input in requested)
            {
                for(int i=0;i<manager.GetCount();i++)
                {
                    string current=manager.get_Equation(i);int at=current==null?-1:current.IndexOf('=');if(at<0)continue;
                    string left=current.Substring(0,at).Trim();
                    if(!String.Equals(left,input.Key,StringComparison.OrdinalIgnoreCase))continue;
                    found.Add(left);
                    if(!String.Equals(EquationExpression(current.Substring(at+1)),input.Value,StringComparison.Ordinal))
                    {
                        manager.set_Equation(i,left+" = "+input.Value);
                        changed.Add(left,input.Value);
                    }
                    break;
                }
            }
            foreach(string name in requested.Keys)if(!found.Contains(name))throw new ArgumentException("母版中找不到参数："+name);
            if(!model.EditRebuild3())throw new InvalidOperationException("SOLIDWORKS 无法按这些参数重建零件。请检查输入值与公式。");
            foreach(KeyValuePair<string,string> input in changed)
            {
                bool persisted=false;
                for(int i=0;i<manager.GetCount();i++)
                {
                    string equation=manager.get_Equation(i);int at=equation==null?-1:equation.IndexOf('=');
                    if(at>=0 && String.Equals(equation.Substring(0,at).Trim(),input.Key,StringComparison.OrdinalIgnoreCase) &&
                       String.Equals(EquationExpression(equation.Substring(at+1)),input.Value,StringComparison.Ordinal)){persisted=true;break;}
                }
                if(!persisted)throw new InvalidOperationException("SOLIDWORKS 未写入参数 "+input.Key+"；已停止保存，请检查方程格式和母版状态。");
            }
            Array bodies=((PartDoc)model).GetBodies2(0,false) as Array;
            if(bodies==null||bodies.Length==0)throw new InvalidOperationException("参数更新后没有形成实体。请检查输入值与公式。");
            int saveError=0,saveWarning=0;
            if(!model.Save3(1,ref saveError,ref saveWarning)||saveError!=0||!File.Exists(partPath))throw new IOException("零件保存失败（SOLIDWORKS 错误代码："+saveError+"）。");
            string message="已保存：\r\n"+partPath;
            // STEP/3MF 必须从当前活动文档导出；读取母版时 SOLIDWORKS 可能仍停留在另一零件。
            model.ClearSelection2(true);
            int activateError=0;
            ModelDoc2 active=app.ActivateDoc3(model.GetTitle(),true,0,ref activateError);
            if(active==null||activateError!=0)throw new InvalidOperationException("无法激活待导出的零件（SOLIDWORKS 错误代码："+activateError+"）。");
            ModelDocExtension ext=model.Extension;
            if(step)message+=Export(ext,basePath+".STEP","STEP");
            if(mf)message+=Export(ext,basePath+".3MF","3MF");
            BuildResult result=new BuildResult{PartPath=partPath,Message=message};
            File.WriteAllText(Path.ChangeExtension(result.PartPath,".parameters.txt"),(options.Equations??String.Empty)+"\r\n\r\n来源：内置方程母版 “"+options.FileName+"”。",new UTF8Encoding(true));
            result.Message+="\r\n\r\n已由内置方程母版生成；源母版未被覆盖。"; return result;
        }

        private static string EquationExpression(string value)
        {
            if(value==null)return String.Empty;
            int comment=value.IndexOf('\'');
            return (comment<0?value:value.Substring(0,comment)).Trim();
        }
    }
}
