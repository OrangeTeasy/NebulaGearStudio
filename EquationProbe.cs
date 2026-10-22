using System;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

internal static class EquationProbe
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string source=args[0],target=args[1];
            File.Copy(source,target,false);
            SldWorks app;
            try { app=(SldWorks)Marshal.GetActiveObject("SldWorks.Application"); Console.WriteLine("attached existing process"); }
            catch(Exception attachError) { Console.WriteLine("starting new process: "+attachError.Message); app=(SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")); }
            Console.WriteLine("revision="+app.RevisionNumber()+" visible="+app.Visible);
            app.Visible=true;
            System.Threading.Thread.Sleep(5000);
            app.CommandInProgress=true;
            int errors=0,warnings=0;
            ModelDoc2 model=app.OpenDoc6(target,1,1,"",ref errors,ref warnings);
            if(model==null)throw new Exception("OpenDoc6 failed: "+errors);
            int activeError=0;
            ModelDoc2 active=app.ActivateDoc3(model.GetTitle(),true,0,ref activeError);
            Console.WriteLine("active="+(active==null?"null":active.GetPathName())+" activationError="+activeError);
            Console.WriteLine("readOnly="+model.IsOpenedReadOnly()+" viewOnly="+model.IsOpenedViewOnly()+" future="+model.Extension.IsFutureVersion());
            EquationMgr manager=model.GetEquationMgr();
            Console.WriteLine("configs="+String.Join(",",(string[])model.GetConfigurationNames()));
            Console.WriteLine("linked="+manager.LinkToFile+" count="+manager.GetCount());
            Console.WriteLine("addBefore="+manager.Add2(-1,"\"PROBE\" = 123",true)+" count="+manager.GetCount()+" status="+manager.Status);
            for(int i=0;i<manager.GetCount();i++)
            {
                string before=manager.get_Equation(i);
                if(before==null||!before.TrimStart().StartsWith("\"z\""))continue;
                Console.WriteLine("index="+i+" before="+before+" global="+manager.get_GlobalVariable(i)+" configOption="+manager.GetConfigurationOption(i));
                manager.set_Equation(i,"\"z\" = 51");
                Console.WriteLine("setStatus="+manager.Status);
                Console.WriteLine("afterSet="+manager.get_Equation(i)+" status="+manager.Status);
                Console.WriteLine("setAll="+manager.SetEquationAndConfigurationOption(i,"\"z\" = 51",2,null)+" status="+manager.Status);
                Console.WriteLine("afterSetAll="+manager.get_Equation(i));
                Console.WriteLine("delete="+manager.Delete(i)+" count="+manager.GetCount());
                Console.WriteLine("add2="+manager.Add2(i,"\"z\" = 51",false)+" count="+manager.GetCount());
                Console.WriteLine("add3="+manager.Add3(i,"\"z\" = 51",true,2,null)+" count="+manager.GetCount());
                Console.WriteLine("addTest="+manager.Add2(-1,"\"TESTVAR\" = 123",true)+" count="+manager.GetCount());
                Console.WriteLine("afterAdd="+manager.get_Equation(i));
                Console.WriteLine("evaluate="+manager.EvaluateAll());
                Console.WriteLine("afterEvaluate="+manager.get_Equation(i));
                Console.WriteLine("rebuild="+model.EditRebuild3());
                Console.WriteLine("afterRebuild="+manager.get_Equation(i));
                int saveError=0,saveWarning=0;
                Console.WriteLine("save="+model.Save3(1,ref saveError,ref saveWarning)+" error="+saveError+" warning="+saveWarning);
                Console.WriteLine("afterSave="+manager.get_Equation(i));
                return 0;
            }
            Console.WriteLine("no z equation; final count="+manager.GetCount());
            return 0;
        }
        catch(Exception ex) { Console.WriteLine(ex);return 1; }
    }
}
