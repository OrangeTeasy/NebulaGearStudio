// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

internal static class VerifyDirectHollows
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            SldWorks app;
            try { app = (SldWorks)Marshal.GetActiveObject("SldWorks.Application"); }
            catch { app = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")); }
            int errors = 0, warnings = 0;
            ModelDoc2 model = app.OpenDoc6(args[0], 1, 1, "", ref errors, ref warnings);
            if (model == null) throw new InvalidOperationException("OpenDoc6 failed: " + errors);
            Array bodies = ((PartDoc)model).GetBodies2(0, false) as Array;
            if (bodies == null || bodies.Length != 1) throw new InvalidOperationException("Expected one solid body.");
            double[] mass = ((Body2)bodies.GetValue(0)).GetMassProperties(1) as double[];
            double actual = mass[3];
            double outer = Double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
            double inner = Double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
            double height = Double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
            double expected = Math.PI * (outer * outer - inner * inner) * height / 4.0;
            Console.WriteLine("BODY_COUNT=" + bodies.Length);
            Console.WriteLine("VOLUME=" + actual.ToString("R") + ";EXPECTED=" + expected.ToString("R"));
            if (Math.Abs(actual - expected) / expected > 0.0001) throw new InvalidOperationException("Volume mismatch: hollow feature was not built correctly.");
            Console.WriteLine("PASS_HOLLOW_VOLUME");
            app.CloseDoc(model.GetTitle());
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
