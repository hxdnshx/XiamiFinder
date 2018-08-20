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
            SetInfo("虾米搜索", "Finnite", "recollectionforgot@gmail.com", "0.0.2", "喵呜");
        }

        protected override List<SongInfo> GetPlaylist(string keyword)
        {
            string search_result = "";
            int tmp;
            if (int.TryParse(keyword, out tmp))
                search_result = keyword;
            else
                search_result = fetcher.SearchCollection(keyword);
            var result = fetcher.GetCollection(search_result);
            if (result == null)
            {
                return null;
            }
            
            return result.Select(song =>
                new SongInfo(this, song.Item5, song.Item1, new string[] {song.Item2}, song.Item4)).ToList();
            //return result.Select(song => SongItem.init(this, song.Item1, search_result + "_" + song.Item1.GetHashCode(), who,
            //    new string[] {song.Item2}, song.Item3, song.Item4, "")).ToList();
        }

        protected override string GetDownloadUrl(SongItem songInfo)
        {
            var download_dir = fetcher.getSongInfo(songInfo.SongId);
            if (download_dir == null)
            {
                Log($"虾米：无法获取\"{songInfo.SongName}\"({songInfo.SongId})的下载地址");
                return null;
            }

            return download_dir.Item3;
        }

        protected override SongInfo Search(string keyword)
        {
            var search_result = fetcher.SearchSong(keyword);
            if (string.IsNullOrEmpty(search_result))
            {
                Log($"虾米：没有搜索到\"{keyword}\"相关的结果");
                return null;
            }

            var download_dir = fetcher.getSongInfo(search_result);
            if (download_dir == null)
            {
                Log($"虾米：无法获取\"{keyword}\"({search_result})的下载地址");
                return null;
            }
            
            return new SongInfo(this, search_result, download_dir.Item1, new string[] { download_dir.Item2 }, download_dir.Item4);
        }

        protected override DownloadStatus Download(SongItem item)
        {
            throw new NotImplementedException();
        }

        protected override void Setting()
        {

        }
    }
}
