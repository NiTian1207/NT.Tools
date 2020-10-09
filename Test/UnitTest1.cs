using NUnit.Framework;
using NT.Tools;
using System.Collections.Generic;

namespace Test
{
    //QQœ¬‘ÿµÿ÷∑ https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            MultiDownload md = new MultiDownload("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq.exe", 1);
            md.DownloadProgressChanged += Md_DownloadProgressChanged;
            md.DownloadComplete += Md_DownloadComplete;
            md.ThreadDownloadComplete += Md_ThreadDownloadComplete;
            md.Start();
            Assert.Pass();
        }

        private void Md_ThreadDownloadComplete(object sender, TDownloadCompleteEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Md_DownloadComplete(object sender, TDownloadCompleteEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Md_DownloadProgressChanged(object sender, TDownloadProgressChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        [Test]
        public void Test2()
        {
            DownloadManager.DownloadItem md = new DownloadManager.DownloadItem("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq.exe", 8);
            DownloadManager.DownloadItem md1 = new DownloadManager.DownloadItem("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq1.exe", 8);
            DownloadManager.DownloadItem md2 = new DownloadManager.DownloadItem("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq2.exe", 8);
            DownloadManager.DownloadItem md3 = new DownloadManager.DownloadItem("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq3.exe", 8);
            DownloadManager.DownloadItem md4 = new DownloadManager.DownloadItem("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq4.exe", 8);
            DownloadManager.DownloadItem md5 = new DownloadManager.DownloadItem("https://down.qq.com/qqweb/PCQQ/PCQQ_EXE/PCQQ2020.exe", @"C:\Users\NiTian1207\Desktop\qq5.exe", 8);
            List<DownloadManager.DownloadItem> mds = new List<DownloadManager.DownloadItem>();
            mds.Add(md);
            mds.Add(md1);
            mds.Add(md2);
            mds.Add(md3);
            mds.Add(md4);
            mds.Add(md5);
            DownloadManager dm = new DownloadManager(mds);
            dm.DownloadProgressChanged += Dm_DownloadProgressChanged;
            dm.DownloadComplete += Dm_DownloadComplete;
            dm.DownloadNum = 3;
            dm.Start();
        }

        private void Dm_DownloadComplete(object sender, TDownloadCompleteEventArgs e)
        {
            
        }

        private void Dm_DownloadProgressChanged(object sender, TDownloadProgressChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}