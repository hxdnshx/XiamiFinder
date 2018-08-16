using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XiamiFinder;
namespace XiamiFinder.Test
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void DecodeUrl()
        {
            XiamiFetcher fetcher = new XiamiFetcher();
            var encoded_url = @"9%8e23E231p_35-939d2.tF5%%833k4E%d196Fx%6%5259%e7%5c65%%i262EF123y35E21a52aF3F2162F%4E-ea1EFm29257__a3%-fc3%2mi321275luD5%3%95%1.59%259.t1E5156E52n%25445mh5%E4E77E";
            var result = fetcher.xiamiUrlDecode(encoded_url);
            Assert.AreEqual(result,
                "http://m128.xiami.net/235/663929235/2100252242/1775438516_59513922_l.mp3?auth_key=1534734000-0-0-f3149dc2ec03161a3967995a107d6020");
        }

        [TestMethod]
        public void Search()
        {
            XiamiFetcher fetch = new XiamiFetcher();
            var search = "さようなら、花泥棒さん 鎖那";
            var result = fetch.SearchSong(search);
            Assert.AreEqual("1775438516", result);
            var search2 = "Unknown+Alstroemeria+Records";
            var result2 = fetch.SearchSong(search2);
            Assert.AreEqual("1770729242", result2);
        }

        [TestMethod]
        public void GetDownloadDir()
        {
            XiamiFetcher fetch = new XiamiFetcher();
            var id = "mQ7xXi841fa";
            var result = fetch.getSongInfo(id);
            Assert.AreNotEqual(null, result);
            var id2 = "1770729242";
            var result2 = fetch.getSongInfo(id2);
            Assert.AreNotEqual(null, result2);
        }

        [TestMethod]
        public void SongNameSpecialChar()
        {
            //由于曲目中含有特殊的字符，所以会导致搜索缓存时出现错误
            XiamiDownload down = new XiamiDownload();
            //Re:GENERATION (Extended Mix) 田口康裕
            var search = "Re%3aGENERATION+(Extended+Mix)+%e7%94%b0%e5%8f%a3%e5%ba%b7%e8%a3%95";
            var result = down.SafeSearch("主播", search, true);
            Assert.AreNotEqual(null, result);
        }

        [TestMethod]
        public void LimitedSong()
        {
            //这个曲目处于被限制的状态，无法获取到其曲目信息
            XiamiFetcher fetch = new XiamiFetcher();
            var id = "8HMzgCf6429";
            var result = fetch.getSongInfo(id);
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void AllTest()
        {
            XiamiDownload down = new XiamiDownload();

            var result = down.SafeSearch("主播", "positive+dance+%22Final+RAVE%22+Cranky", true);
            Assert.AreNotEqual(null, result);
            var result2 = down.SafeSearch("主播", "ILIAS", true);
            Assert.AreNotEqual(null, result2);
        }
    }
}
