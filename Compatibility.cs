using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using SolidWorks.Interop.sldworks;

namespace SolidWorksPrimitiveBuilderV2
{
    internal static class SolidWorksCompatibility
    {
        internal static int YearFromRevision(string revision)
        {
            if (String.IsNullOrWhiteSpace(revision)) throw new InvalidOperationException("SOLIDWORKS 未返回版本号。");
            string majorText = revision.Split('.')[0];
            int major;
            if (!Int32.TryParse(majorText, NumberStyles.None, CultureInfo.InvariantCulture, out major) || major < 8 || major > 100)
                throw new InvalidOperationException("无法识别 SOLIDWORKS 版本号：" + revision);
            return major + 1992;
        }

        internal static int InstalledYear(SldWorks app)
        {
            return YearFromRevision(app.RevisionNumber());
        }

        internal static string ResolveMother(string libraryRoot, int year, string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("母版文件名无效。", "fileName");

            // 2024 母版可由新版本升级打开；旧版本只能使用为其单独保存的母版。
            if (year >= 2024)
            {
                string exact = Path.Combine(libraryRoot, "SW" + year, fileName);
                if (File.Exists(exact)) return exact;
                string bundled = Path.Combine(libraryRoot, fileName);
                if (File.Exists(bundled)) return bundled;
                throw new FileNotFoundException("安装包缺少母版：" + fileName, bundled);
            }

            string older = Path.Combine(libraryRoot, "SW" + year, fileName);
            if (File.Exists(older)) return older;
            throw new FileNotFoundException(
                "当前 SOLIDWORKS " + year + " 无法编辑安装包内的 2024 母版。请用 SOLIDWORKS 2024 将该母版另存为 " + year +
                " 版本，检查兼容特征后放入此目录；不能仅复制或改名 2024 文件：\r\n" + older,
                older);
        }

        internal static bool IsFutureTemplatePath(string path, int runningYear)
        {
            Match match = Regex.Match(path ?? String.Empty, @"SOLIDWORKS\s+(\d{4})", RegexOptions.IgnoreCase);
            int templateYear;
            return match.Success && Int32.TryParse(match.Groups[1].Value, out templateYear) && templateYear > runningYear;
        }

        internal static void RequireEditableMother(ModelDoc2 model, string path)
        {
            if (model.Extension.IsFutureVersion())
                throw new InvalidOperationException("此母版仍是高版本文件，当前 SOLIDWORKS 只能只读查看，不能修改方程。请真正另存为当前版本后再使用：\r\n" + path);
            if (model.IsOpenedReadOnly() || model.IsOpenedViewOnly())
                throw new InvalidOperationException("母版副本以只读或仅查看方式打开，无法修改方程。请检查文件权限、SOLIDWORKS 加载状态及其他占用程序：\r\n" + model.GetPathName());
        }
    }
}
