using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DMPlugin_DGJ;

namespace XiamiFinder
{
    public class XiamiDownload : DMPlugin_DGJ.SongsSearchModule
    {
        private XiamiFetcher fetcher;
        public XiamiDownload()
        {
            fetcher = new XiamiFetcher();
            setInfo("虾米搜索", "Finnite", "recollectionforgot@gmail.com", "0.0.2", "喵呜", true);
        }

        protected override SongItem Search(string who, string what, bool needLyric = false)
        {
            var search_result = fetcher.SearchSong(what);
            if (string.IsNullOrEmpty(search_result))
            {
                Log($"虾米：没有搜索到\"{what}\"相关的结果");
                return null;
            }

            var download_dir = fetcher.getSongInfo(search_result);
            if (download_dir == null)
            {
                Log($"虾米：无法获取\"{what}\"({search_result})的下载地址");
                return null;
            }

            SongItem item = SongItem.init(this, download_dir.Item1, search_result, who,
                new string[] {download_dir.Item2}, download_dir.Item3, download_dir.Item4,"");
            return item;
        }

        protected override void Setting()
        {

        }
    }
}
