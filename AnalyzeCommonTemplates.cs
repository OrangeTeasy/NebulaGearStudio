using System;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

internal static class AnalyzeCommonTemplates
{
    [STAThread]
    private static int Main(string[] args)
    {
        string directory = args.Length == 0 ? @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件" : args[0];
        try
        {
            SldWorks app;
            try { app = (SldWorks)Marshal.GetActiveObject("SldWorks.Application"); }
            catch { app = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")); }
            app.Visible = true;
            Console.WriteLine("SOLIDWORKS=" + app.RevisionNumber());
            foreach (string file in Directory.GetFiles(directory, "*.prtdot")) Inspect(app, file);
            foreach (string file in Directory.GetFiles(directory, "*.PRTDOT"))
                if (!File.Exists(Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + ".prtdot"))) Inspect(app, file);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Inspect(SldWorks app, string file)
    {
        int errors = 0, warnings = 0;
        ModelDoc2 model = app.OpenDoc6(file, 1, 1, "", ref errors, ref warnings);
        Console.WriteLine("FILE=" + Path.GetFileName(file));
        Console.WriteLine("OPEN=" + errors + ";" + warnings);
        if (model == null) return;
        try
        {
            int featureCount = 0;
            for (Feature feature = (Feature)model.FirstFeature(); feature != null; feature = (Feature)feature.GetNextFeature())
            {
                featureCount++;
                Console.WriteLine("FEATURE=" + feature.Name + "|" + feature.GetTypeName2());
            }
            EquationMgr equations = model.GetEquationMgr();
            int equationCount = equations.GetCount();
            Console.WriteLine("COUNTS=features:" + featureCount + ";equations:" + equationCount);
            for (int i = 0; i < equationCount; i++) Console.WriteLine("EQ=" + equations.get_Equation(i));
        }
        finally { app.CloseDoc(model.GetTitle()); }
    }
}
