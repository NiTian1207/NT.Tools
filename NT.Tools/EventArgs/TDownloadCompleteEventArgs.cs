using System;
using System.Collections.Generic;
using System.Text;

namespace NT.Tools
{
    public class TDownloadCompleteEventArgs : EventArgs
    {
        public TDownloadCompleteEventArgs(string url, string savePath, Exception error)
        {
            SavePath = savePath;
            Url = url;
            Error = error;
        }

        public TDownloadCompleteEventArgs(string url, string savePath)
        {
            SavePath = savePath;
            Url = url;
        }

        public TDownloadCompleteEventArgs(string url, string savePath, int threadIndex)
        {
            ThreadIndex = threadIndex;
            SavePath = savePath;
            Url = url;
        }

        public Exception Error { get; }
        public int ThreadIndex { get; }
        public string Url { get; }
        public string SavePath { get; }
    }
}
