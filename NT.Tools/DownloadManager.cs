using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NT.Tools
{
    public class DownloadManager
    {
        int _downloadNum = 1;
        int _downloadingNum = 0;
        /// <summary>
        /// 同时下载量
        /// </summary>
        public int DownloadNum { set { _downloadNum = value; } get { return _downloadNum; } }
        List<MultiDownload> downloads = new List<MultiDownload>();   //下载对象

        public delegate void DownloadProgressChangedEventHandler(object sender, TDownloadProgressChangedEventArgs e);
        public delegate void DownloadCompleteEventHandler(object sender, TDownloadCompleteEventArgs e);
        public delegate void ThreadDownloadCompleteEventHandler(object sender, TDownloadCompleteEventArgs e);
        public delegate void DownloadErrorEventHandler(object sender, TDownloadCompleteEventArgs e);
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public event DownloadCompleteEventHandler DownloadComplete;
        public event ThreadDownloadCompleteEventHandler ThreadDownloadComplete;
        public event DownloadErrorEventHandler DownloadError;


        public class DownloadItem
        {
            public DownloadItem(string url, string savePath, int threadNum = 8, bool canResume = true)
            {
                Url = url;
                SavePath = savePath;
                ThreadNum = threadNum;
                CanResume = canResume;
            }
            public string Url { get; set; }
            public string SavePath { get; set; }
            public int ThreadNum { get; set; }
            public bool Completed { get { return _completed; } }
            public bool IsPaused { get { return _isPaused; } }
            public bool CanResume { get; set; }
            private bool _completed;
            private bool _isPaused;
        }
        #region 事件
        private void Md_DownloadError(object sender, TDownloadCompleteEventArgs e)
        {
            _downloadingNum--;
            for (int i = 0; i < downloads.Count; i++)
            {
                if (downloads[i].SavePath == e.SavePath)
                {
                    downloads.RemoveAt(i);
                    break;
                }
            }
            if (DownloadError != null)
                DownloadError(this, e);
        }

        private void Md_ThreadDownloadComplete(object sender, TDownloadCompleteEventArgs e)
        {
            _downloadingNum--;
            for (int i = 0; i < downloads.Count; i++)
            {
                if (downloads[i].SavePath == e.SavePath)
                {
                    downloads.RemoveAt(i);
                    break;
                }
            }
            if (ThreadDownloadComplete != null)
                ThreadDownloadComplete(this, e);
        }

        private void Md_DownloadComplete(object sender, TDownloadCompleteEventArgs e)
        {
            if (DownloadComplete != null)
                DownloadComplete(this, e);
        }

        private void DownloadManager_DownloadProgressChanged(object sender, TDownloadProgressChangedEventArgs e)
        {
            if (DownloadProgressChanged != null)
                DownloadProgressChanged(this, e);
        }

        #endregion

        public DownloadManager(List<DownloadItem> downloadItems)
        {
            foreach (DownloadItem di in downloadItems)
            {
                MultiDownload md = new MultiDownload(di.Url, di.SavePath, di.ThreadNum, di.CanResume);
                md.DownloadProgressChanged += DownloadManager_DownloadProgressChanged;
                md.DownloadComplete += Md_DownloadComplete;
                md.ThreadDownloadComplete += Md_ThreadDownloadComplete;
                md.DownloadError += Md_DownloadError;
                downloads.Add(md);

            }
        }

        public DownloadManager(DownloadItem downloadItems)
        {
            MultiDownload md = new MultiDownload(downloadItems.Url, downloadItems.SavePath, downloadItems.ThreadNum, downloadItems.CanResume);
            md.DownloadProgressChanged += DownloadManager_DownloadProgressChanged;
            md.DownloadComplete += Md_DownloadComplete;
            md.ThreadDownloadComplete += Md_ThreadDownloadComplete;
            md.DownloadError += Md_DownloadError;
            downloads.Add(md);
        }

        public void Start()
        {
            Thread t = new Thread(Download);
            t.Start();
        }

        public void AddItem(DownloadItem downloadItem)
        {
            MultiDownload md = new MultiDownload(downloadItem.Url, downloadItem.SavePath, downloadItem.ThreadNum, downloadItem.CanResume);
            md.DownloadProgressChanged += DownloadManager_DownloadProgressChanged;
            md.DownloadComplete += Md_DownloadComplete;
            md.ThreadDownloadComplete += Md_ThreadDownloadComplete;
            md.DownloadError += Md_DownloadError;
            downloads.Add(md);
        }

        public void DeletItem(DownloadItem downloadItem)
        {
            downloads.Remove(new MultiDownload(downloadItem.Url, downloadItem.SavePath, downloadItem.ThreadNum, downloadItem.CanResume));
        }

        private void Download()
        {
            while (true)
            {
                if (_downloadingNum < _downloadNum && _downloadNum < downloads.Count)
                {
                    foreach (MultiDownload md in downloads)
                    {
                        if (md.State == "Free")
                        {
                            md.Start();
                            _downloadingNum++;
                            break;
                        }
                    }
                }
            }
        }

        public void Pause(string SavePath)
        { 
            
        }

        public void Continue(string SavePath)
        {

        }

        public void Cancel(string SavePath)
        {

        }
    }

    /// <summary>
    /// 多线程下载,暂停,断点续传
    /// </summary>
    public class MultiDownload
    {
        string _url;   //下载链接
        string _savePath;   //文件保存绝对路径
        string _state = "Free";   //下载状态
        int _threadNum = 8;   //线程数
        int _threadCompleteNum;   //完成线程数
        long _fileSize;   //文件大小
        long _downloadedSize;   //已下载文件大小
        object locker = new object();
        bool _cancel = false;
        bool _pause = false;
        bool _resume = true;
        List<Thread> _threads = new List<Thread>();   //下载线程组
        List<string> _tempFiles = new List<string>();   //临时文件保存路径
        List<string> _uncompleteFiles = new List<string>();   //未下载完成的临时文件保存路径
        List<long[]> _ranges = new List<long[]>();   //下载分段 [0]=开始 [1]=结尾 [2]=线程序号
        List<DownloadThreadInfo> _threadInfos = new List<DownloadThreadInfo>();  //下载线程进度信息
        /// <summary>
        /// Free Downloading Cancelled Paused Error
        /// </summary>
        public string State { get { return _state; } }
        public string SavePath { get { return _savePath; } }
        public string Url { get { return _url; } }

        public delegate void DownloadProgressChangedEventHandler(object sender, TDownloadProgressChangedEventArgs e);
        public delegate void DownloadCompleteEventHandler(object sender, TDownloadCompleteEventArgs e);
        public delegate void ThreadDownloadCompleteEventHandler(object sender, TDownloadCompleteEventArgs e);
        public delegate void DownloadErrorEventHandler(object sender, TDownloadCompleteEventArgs e);
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public event DownloadCompleteEventHandler DownloadComplete;
        public event ThreadDownloadCompleteEventHandler ThreadDownloadComplete;
        public event DownloadErrorEventHandler DownloadError;

        private class DownloadThreadInfo
        {
            public int Index { get; set; }
            public long ThreadBytesReceived { get; set; }
            public long ThreadBytesToReceive { get; set; }
            public long[] DownloadRange { get; set; }
            public long Position { get; set; }
        }

        public MultiDownload(string url, string savePath, int threadNum = 8, bool canResume = true)
        {
            _url = url;
            _savePath = savePath;
            _threadNum = threadNum;
            _resume = canResume;
        }

        public MultiDownload()
        {

        }

        public void Start(string url, string savePath, int threadNum = 8, bool canResume = true)
        {
            _url = url;
            _savePath = savePath;
            _threadNum = threadNum;
            _resume = canResume;
            Start();
        }
        public void Start()
        {
            if (string.IsNullOrEmpty(_url) || string.IsNullOrEmpty(_savePath))
            {
                return;
            }
            _state = "Downloading";
            HttpWebRequest request;
            HttpWebResponse response;
            long singleFileLength = 0;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(_url);
                response = (HttpWebResponse)request.GetResponse();
                _fileSize = response.ContentLength;
                singleFileLength = _fileSize / _threadNum;
                request.Abort();
                response.Close();
            }
            catch (Exception e)
            {
                _state = "Error";
                if (DownloadError != null)
                    DownloadError(this, new TDownloadCompleteEventArgs(_url, _savePath, e));
            }

            for (int i = 0; i < _threadNum; i++)   //分配下载块
            {
                long[] range = new long[3];
                range[0] = i * singleFileLength;
                range[1] = (i + 1) * singleFileLength - 1;
                range[2] = i;
                if (i == _threadNum - 1)
                {
                    range[1] = _fileSize - 1;
                }
                _ranges.Add(range);
                Thread t = new Thread(new ParameterizedThreadStart(AsyncDownload));
                _threads.Add(t);
                t.Start(range);
            }
        }

        private string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        /// <summary>
        /// 参数为 long[] 型, 三个元素分别对应 起始位置,终止位置,线程序号
        /// </summary>
        /// <param name="obj"></param>
        private void AsyncDownload(object obj)
        {
            long[] range = (long[])obj;
            DownloadThreadInfo info = new DownloadThreadInfo()
            {
                Index = (int)range[2],
                ThreadBytesReceived = 0,
                ThreadBytesToReceive = range[1] - range[0] + 1,
                DownloadRange = range,
                Position = 0
            };
            lock (locker) _threadInfos.Add(info);
            Stream httpFileStream = null, localFileStream = null;
            try
            {
                string tempPath = Path.GetTempPath() + Path.GetFileNameWithoutExtension(_url) + Guid.NewGuid().ToString().Substring(0, 4) + ".tmp" + range[2];   //临时文件保存路径

                lock (locker)
                {
                    if (_uncompleteFiles.IndexOf(tempPath) == -1)
                    {
                        lock (locker) _uncompleteFiles.Add(tempPath);
                    }
                }
                lock (locker)
                {
                    if (_tempFiles.IndexOf(tempPath) == -1)
                    {
                        lock (locker) _tempFiles.Add(tempPath);
                    }
                }
                bool createStream = false;
                while (!createStream)   //创建文件流
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            if (_resume)
                            {
                                localFileStream = new FileStream(tempPath, FileMode.Open);
                                localFileStream.Position = localFileStream.Length;
                                info.ThreadBytesReceived = localFileStream.Length;
                                lock (locker) _downloadedSize += localFileStream.Length;
                            }
                            else
                            {
                                File.Delete(tempPath);
                                localFileStream = new FileStream(tempPath, FileMode.Create);
                            }
                        }
                        else
                        {
                            localFileStream = new FileStream(tempPath, FileMode.Create);
                        }
                        createStream = true;
                    }
                    catch
                    { }
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
                if (localFileStream.Length > 0)
                { range[0] += localFileStream.Length; }
                request.AddRange(range[0], range[1]);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36 Edg/85.0.564.68";
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                httpFileStream = response.GetResponseStream();
                byte[] by = new byte[1024 * 16];
                int getByteSize = httpFileStream.Read(by, 0, (int)by.Length); //Read方法将返回读入by变量中的总字节数
                while (getByteSize > 0 && !_cancel && !_pause)
                {
                    Thread.Sleep(20);
                    lock (locker) _downloadedSize += getByteSize; //统计总共下载量
                    info.ThreadBytesReceived += getByteSize; //统计此线程下载量
                    localFileStream.Write(by, 0, getByteSize);
                    getByteSize = httpFileStream.Read(by, 0, (int)by.Length);
                    info.Position = localFileStream.Position;
                    if (DownloadProgressChanged != null)
                        DownloadProgressChanged(this, new TDownloadProgressChangedEventArgs(_url, _savePath, (int)range[2], (float)_downloadedSize / (float)_fileSize, _downloadedSize, _fileSize, (float)info.ThreadBytesReceived / (float)info.ThreadBytesToReceive, info.ThreadBytesReceived, info.ThreadBytesToReceive));
                }
                lock (locker) _threads.Remove(Thread.CurrentThread);
                if (!_cancel && !_pause)
                {
                    lock (locker) _threadCompleteNum++;
                    lock (locker) _uncompleteFiles.Remove(tempPath);
                    if (ThreadDownloadComplete != null)
                        ThreadDownloadComplete(this, new TDownloadCompleteEventArgs(_url, _savePath, (int)range[2]));
                }
            }
            catch (Exception e)
            {
                _state = "Error";
                if (DownloadError != null)
                    DownloadError(this, new TDownloadCompleteEventArgs(_url, _savePath, e));
            }
            finally
            {
                if (httpFileStream != null)
                {
                    httpFileStream.Close();
                }
                if (localFileStream != null)
                {
                    localFileStream.Close();
                }
            }
            if (_threadCompleteNum == _threadNum)
            {
                Thread t = new Thread(MergeFile);
                t.Start();
            }
        }

        /// <summary>
        /// 合并文件块
        /// </summary>
        private void MergeFile()
        {
            Stream mergeFile = new FileStream(_savePath, FileMode.Create);
            BinaryWriter br = new BinaryWriter(mergeFile);
            for (int i = 0; i < _threadNum; i++)
            {
                for (int x = 0; x < _threadNum; x++)
                {
                    string index = Path.GetExtension(_tempFiles[x]).Replace(".tmp", "");
                    if (index == i.ToString())
                    {
                        bool createSucess = false;
                        try
                        {
                            while (!createSucess)
                            {
                                FileStream fs = new FileStream(_tempFiles[x], FileMode.Open);
                                createSucess = true;
                                BinaryReader TempReader = new BinaryReader(fs);
                                br.Write(TempReader.ReadBytes((int)fs.Length));
                                TempReader.Close();
                                File.Delete(_tempFiles[x]);
                            }
                        }
                        catch { }
                    }
                }
            }
            if (DownloadComplete != null)
                DownloadComplete(this, new TDownloadCompleteEventArgs(_url, _savePath));
        }

        public void Cancel()
        {
            _cancel = true;
            _threads.Clear();
            _state = "Cancelled";
            for (int i = 0; i < _threadNum; i++)
            {
                bool delSuccess = false;
                while (!delSuccess)
                {
                    try
                    {
                        for (int x = 0; x < _tempFiles.Count; x++)
                        {
                            if (Int32.Parse(Path.GetExtension(_tempFiles[x]).Replace(".tmp", "")) == i)
                            {
                                File.Delete(_tempFiles[x]);
                            }
                        }
                        delSuccess = true;
                    }
                    catch
                    { Thread.Sleep(100); }
                }

            }
        }

        public void Pause()
        {
            if (!_pause)
            {
                _pause = true;
                _threads.Clear();
                _state = "Paused";
            }
        }

        public void Continue()
        {
            if (_pause)
            {
                _pause = false;
                _state = "Downloading";
                for (int i = 0; i < _uncompleteFiles.Count; i++)
                {
                    for (int x = 0; x < _uncompleteFiles.Count; x++)
                    {
                        if (_uncompleteFiles[x].EndsWith(i.ToString()))
                        {
                            using (Stream fileStream = new FileStream(_uncompleteFiles[x], FileMode.Open))
                            {
                                for (int y = 0; y < _ranges.Count; y++)
                                {
                                    if ((int)_ranges[y][2] == i)
                                    {
                                        _ranges[y][0] += fileStream.Length;
                                        Thread t = new Thread(new ParameterizedThreadStart(AsyncDownload));
                                        t.Start(_ranges[y]);
                                        _threads.Add(t);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
