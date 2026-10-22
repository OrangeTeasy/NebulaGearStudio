// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

internal static class TemplateInspector
{
    [STAThread]
    private static void Main()
    {
        string[] paths = {
            @"C:\Users\Lenovo\Desktop\零件7.SLDPRT"
        };
        object app = null;
        try { app = Marshal.GetActiveObject("SldWorks.Application"); } catch { }
        if (app == null) app = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        var sw = (SldWorks)app;
        sw.Visible = true;
        foreach (string path in paths)
        {
            int errors = 0, warnings = 0;
            ModelDoc2 doc = sw.OpenDoc6(path, 1, 1, "", ref errors, ref warnings);
            Console.WriteLine("TEMPLATE=" + System.IO.Path.GetFileName(path));
            Console.WriteLine("OPEN=" + errors + ";" + warnings);
            if (doc == null) continue;
            for (Feature f = (Feature)doc.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
                Console.WriteLine(f.Name + " | " + f.GetTypeName2());
            EquationMgr equations = doc.GetEquationMgr();
            int equationCount = equations.GetCount();
            Console.WriteLine("EQUATION_COUNT=" + equationCount);
            for (int i = 0; i < equationCount; i++) Console.WriteLine("EQ=" + equations.get_Equation(i));
            sw.CloseDoc(doc.GetTitle());
        }
    }
}
