using GLib;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HSFrameWork.ConfigTable.Editor.Impl;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// 无状态工具类
    /// </summary>
    public static class Utils
    {
        public static string DeleteWithMeta(this string file)
        {
            file.Delete();
            (file + ".meta").Delete();
            return file;
        }

        public static string DeleteAsABFile(this string file)
        {
            file.Delete();
            (file + ".meta").Delete();
            (file + ".manifest").Delete();
            (file + ".manifest.meta").Delete();
            return file;
        }

        public static ParallelOptions CreatePO(CancellationToken token)
        {
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = HSCTC.MaxDegreeOfParallelism;
            po.CancellationToken = token;
            return po;
        }

        public static ParallelOptions DefaultPO
        {
            get
            {
                ParallelOptions po = new ParallelOptions();
                po.MaxDegreeOfParallelism = HSCTC.MaxDegreeOfParallelism;
                return po;
            }
        }

        public static void SafeThrowIfCancellationRequested(this CancellationToken token)
        {
            if (token == CancellationToken.None)
            {
                return;
            }
            token.ThrowIfCancellationRequested();
        }

        public static CancellationToken SafeToken(this CancellationTokenSource cts)
        {
            return (cts == null || TE.NoThreadExtention) ? CancellationToken.None : cts.Token;
        }

        public static void SortAndSave(string fileName, IEnumerable<string> names)
        {
            File.WriteAllLines(fileName, Mini.NewList(names).SortC().ToArray());
        }

        public static XmlDocument LoadXmlFile(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            return doc;
        }
    }
}
