using NUnit.Framework;
using NT.Tools;

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
    }
}