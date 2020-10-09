using System;
using System.Collections.Generic;
using System.Text;

namespace NT.Tools
{
    public class TDownloadProgressChangedEventArgs : EventArgs
    {
        public TDownloadProgressChangedEventArgs(string url, string savePath, int threadIndex, float totalProgressPercentage, long totalBytesReceived, long totalBytesToReceive, float threadProgressPercentage, long threadBytesReceived, long threadTotalBytesToReceive)
        {
            TotalProgressPercentage = totalProgressPercentage;
            TotalBytesReceived = totalBytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
            ThreadProgressPercentage = threadProgressPercentage;
            ThreadIndex = threadIndex;
            ThreadBytesReceived = threadBytesReceived;
            ThreadTotalBytesToReceive = threadTotalBytesToReceive;
            Url = url;
            SavePath = savePath;
        }
        public string Url { get; }
        public string SavePath { get; }
        /// <summary>
        /// 总文件下载百分比
        /// </summary>
        public float TotalProgressPercentage { get; }
        /// <summary>
        /// 文件已下载量
        /// </summary>
        public long TotalBytesReceived { get; }
        /// <summary>
        /// 文件总下载量
        /// </summary>
        public long TotalBytesToReceive { get; }
        /// <summary>
        /// 线程序号
        /// </summary>
        public int ThreadIndex { get; }
        /// <summary>
        /// 单线程下载百分比
        /// </summary>
        public float ThreadProgressPercentage { get; }
        /// <summary>
        /// 线程已下载量
        /// </summary>
        public long ThreadBytesReceived { get; }
        /// <summary>
        /// 线程分配下载量
        /// </summary>
        public long ThreadTotalBytesToReceive { get; }
    }
}
