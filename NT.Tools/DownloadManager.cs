using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NT.Tools
{
    /// <summary>
    /// 多线程下载,暂停,断点续传
    /// </summary>
    public class MultiDownload
    {
        string _url;   //下载链接
        string _savePath;   //文件保存绝对路径
        string _state;   //下载状态
        int _threadNum;   //线程数
        int _threadCompleteNum;   //完成线程数
        long _fileSize;   //文件大小
        long _downloadedSize;   //已下载文件大小
        object locker = new object();
        bool _cancel = false;
        bool _pause = false;
        List<Thread> _threads = new List<Thread>();   //下载线程组
        List<string> _tempFiles = new List<string>();   //临时文件保存路径
        List<string> _uncompleteFiles = new List<string>();   //未下载完成的临时文件保存路径
        List<long[]> _ranges = new List<long[]>();   //下载分段 [0]=开始 [1]=结尾 [2]=线程序号
        List<DownloadThreadInfo> _threadInfos = new List<DownloadThreadInfo>();  //下载线程进度信息
        public string State { get { return _state; } }

        public delegate void DownloadProgressChangedEventHandler(object sender, TDownloadProgressChangedEventArgs e);
        public delegate void DownloadCompleteEventHandler(object sender, TDownloadCompleteEventArgs e);
        public delegate void ThreadDownloadCompleteEventHandler(object sender, TDownloadCompleteEventArgs e);
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public event DownloadCompleteEventHandler DownloadComplete;
        public event ThreadDownloadCompleteEventHandler ThreadDownloadComplete;

        private class DownloadThreadInfo
        {
            public int Index { get; set; }
            public long ThreadBytesReceived { get; set; }
            public long ThreadBytesToReceive { get; set; }
            public long[] DownloadRange { get; set; }
            public long Position { get; set; }
        }

        public MultiDownload(string url, string savePath, int threadNum = 8)
        {
            _url = url;
            _savePath = savePath;
            _threadNum = threadNum;
        }

        public void Start()
        {
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
                if (DownloadComplete != null)
                    DownloadComplete(this, new TDownloadCompleteEventArgs(e));
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
                string tempPath = Path.GetTempPath() + Path.GetFileNameWithoutExtension(_url) + ".tmp" + range[2];   //临时文件保存路径

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
                            localFileStream = new FileStream(tempPath, FileMode.Open);
                            localFileStream.Position = localFileStream.Length;
                            info.ThreadBytesReceived = localFileStream.Length;
                            lock (locker) _downloadedSize += localFileStream.Length;
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
                        DownloadProgressChanged(this, new TDownloadProgressChangedEventArgs((int)range[2], (float)_downloadedSize / (float)_fileSize, _downloadedSize, _fileSize, (float)info.ThreadBytesReceived / (float)info.ThreadBytesToReceive, info.ThreadBytesReceived, info.ThreadBytesToReceive));
                }
                lock (locker) _threads.Remove(Thread.CurrentThread);
                if (!_cancel && !_pause)
                {
                    lock (locker) _threadCompleteNum++;
                    lock (locker) _uncompleteFiles.Remove(tempPath);
                    if (ThreadDownloadComplete != null)
                        ThreadDownloadComplete(this, new TDownloadCompleteEventArgs((int)range[2]));
                }
            }
            catch (Exception e)
            {
                _state = "Error";
                if (DownloadComplete != null)
                    DownloadComplete(this, new TDownloadCompleteEventArgs(e));
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
                DownloadComplete(this, new TDownloadCompleteEventArgs());
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
                        File.Delete(Path.GetTempPath() + Path.GetFileNameWithoutExtension(_url) + ".tmp" + i);
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
