using System;
using System.Collections.Generic;
using System.Text;

namespace NT.Tools
{
    public class NewDownloadEventArgs
    {
        public NewDownloadEventArgs(string url, string savePath, int threadNum)
        {
            Url = url;
            SavePath = savePath;
            ThreadNum = threadNum;
        }
        public string Url { get; }
        public string SavePath { get; }
        public int ThreadNum { get; }
    }
}
