using System;
using System.Collections.Generic;
using System.Text;

namespace NT.Tools
{
    public class TDownloadCompleteEventArgs : EventArgs
    {
        public TDownloadCompleteEventArgs(Exception error)
        {
            Error = error;
        }

        public TDownloadCompleteEventArgs()
        {
        }

        public TDownloadCompleteEventArgs(int threadIndex)
        {
            ThreadIndex = threadIndex;
        }

        public Exception Error { get; }
        public int ThreadIndex { get; }
    }
}
