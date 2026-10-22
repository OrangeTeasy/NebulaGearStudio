using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconBuilder
{
    private sealed class Entry { internal int Size; internal byte[] Data; }

    private static int Main(string[] args)
    {
        if (args.Length != 2) return 2;
        int[] sizes = { 256, 128, 64, 48, 32, 24, 16 };
        var entries = new List<Entry>();
        using (var source = new Bitmap(args[0]))
        {
            foreach (int size in sizes)
            {
                using (var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(canvas))
                using (var stream = new MemoryStream())
                {
                    g.Clear(Color.Transparent);
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(source, new Rectangle(0, 0, size, size));
                    canvas.Save(stream, ImageFormat.Png);
                    entries.Add(new Entry { Size = size, Data = stream.ToArray() });
                }
            }
        }

        using (var file = File.Create(args[1]))
        using (var writer = new BinaryWriter(file))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)entries.Count);
            int offset = 6 + entries.Count * 16;
            foreach (Entry e in entries)
            {
                writer.Write((byte)(e.Size == 256 ? 0 : e.Size));
                writer.Write((byte)(e.Size == 256 ? 0 : e.Size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(e.Data.Length);
                writer.Write(offset);
                offset += e.Data.Length;
            }
            foreach (Entry e in entries) writer.Write(e.Data);
        }
        return 0;
    }
}
