// Copyright (c) 2026 OrangeTeasy. Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
class VerifyGear
{
 [STAThread] static int Main(string[] args)
 {
  try {return Run(args);}catch(Exception ex){Console.WriteLine(ex.GetType().Name+": "+ex.Message);return 1;}
 }
 static int Run(string[] args)
 {
  SldWorks app;
  try {app=(SldWorks)Marshal.GetActiveObject("SldWorks.Application");}
  catch {app=(SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));}
  app.Visible=true;
  foreach(string file in Directory.GetFiles(args[0],"*.SLDPRT"))
  {
   if(Path.GetFileName(file).StartsWith("~$"))continue;
   Console.WriteLine("Checking "+Path.GetFileName(file));
   ModelDoc2 model=(ModelDoc2)app.GetOpenDocumentByName(file);
   if(model==null) {int errors=0,warnings=0; model=app.OpenDoc6(file,1,1,"",ref errors,ref warnings);}
   if(model==null)throw new Exception("Document missing: "+file);
   Array bodies=((PartDoc)model).GetBodies2(0,false) as Array;
   if(bodies==null || bodies.Length!=1)throw new Exception("Not one solid");
   double[] mass=(double[])((Body2)bodies.GetValue(0)).GetMassProperties(1);
   Console.WriteLine(Path.GetFileName(file)+" volume_mm3="+(mass[3]*1e9));
   for(Feature f=model.FirstFeature();f!=null;f=f.GetNextFeature())
   {
    bool warning=false;int error=f.GetErrorCode2(out warning);
    if(error!=0)throw new Exception(f.Name+" error="+error);
   }
   if(file.Contains("z16"))
   {
    model.ShowNamedView2("*Isometric",7);model.ViewZoomtofit2();
    if(!model.SaveBMP(Path.Combine(args[0],"gear-preview.bmp"),0,0))throw new Exception("Preview failed");
   }
  }
  return 0;
 }
}
