using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft;
using System.Text.RegularExpressions;
using DMPlugin_DGJ;
using Newtonsoft.Json.Linq;

namespace XiamiFinder
{
    public class XiamiFetcher
    {
        private static string urlParts = "http://www.xiami.com/song/playlist/id/{1}/type/{0}/cat/json";
        private static string SongUrl = string.Format(urlParts, 0, "{0}");
        private static string CollectUrl = string.Format(urlParts, 3, "{0}");
        private static string searchUrl = "https://www.xiami.com/search?key={0}";
        private static string searchUrl_Baidu = "https://www.baidu.com/s?wd={0}";
        private static string playUrl = "http://www.xiami.com/play?ids=";
        private static string xiamiUrl = "http://www.xiami.com";

        private HttpClient _client;

        public XiamiFetcher()
        {
            //HttpClientHandler httpClientHandler = new HttpClientHandler();
            //httpClientHandler.AllowAutoRedirect = false;
            _client = new HttpClient();//(httpClientHandler);
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A356 Safari/604.1");
            //client.DefaultRequestHeaders.Add("Content-Type", "application /x-www-form-urlencoded");
            //_client.DefaultRequestHeaders.Add("Referer", "http://music.163.com");
            _client.DefaultRequestHeaders.Add("Origin", "http://music.163.com");
            _client.DefaultRequestHeaders.Add("Host", "www.xiami.com");
            _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        }

        private static Regex lrc_proc = new Regex("<\\d+>");
        private static Regex lrc_translate = new Regex(@"\[(\d+:\d+\.\d+)\]([^\r\n]+)[\r\n]+\[x-trans\]([^\r\n]+)");
        public Tuple<string, string, string, string, string> getSongInfo(string id)
        {
            var request_url = string.Format(SongUrl, id);
            var result_json = HttpGet(request_url);
            if (result_json.Length == 0)
                return null;
            JObject json;
            try
            {
                json = JObject.Parse(result_json);
            }
            catch (Exception)
            {
                return null;
            }

            if (json["data"]["trackList"].HasValues == false)
                return null;
            var song = json["data"]["trackList"][0];
            return ParseJsonSongInfo(song);
        }

        private Tuple<string, string, string, string, string> ParseJsonSongInfo(JToken song)
        {
            if (song["location"] == null) return null;
            var loc = song["location"].Value<string>();
            var lyric = "http:" + song["lyricInfo"]["lyricFile"];
            var lyric_str = lyric == "http:" ? "" : HttpGet(lyric);
            lyric_str = lrc_proc.Replace(lyric_str, "");
            lyric_str = lrc_translate.Replace(lyric_str, "[$1]$2($3)");
            var real_loc = xiamiUrlDecode(loc);
            var artist = song["artist"].Value<string>();
            var songName = song["songName"].Value<string>();
            var songId = song["songId"].Value<string>();
            var myMusicFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            var myMusicFolder = new DirectoryInfo(myMusicFolderPath);
            if (IsValidFilename($"{artist}-{songName}.mp3"))
            {
                try
                {
                    foreach (var ele in myMusicFolder.EnumerateFiles($"{artist}-{songName}.mp3",
                        SearchOption.AllDirectories))
                    {
                        return new Tuple<string, string, string, string, string>(songName, artist, $"file://{ele.FullName}",
                            lyric_str, songId);
                    }
                }
                catch (Exception)
                {
                    //此处总会有奇葩的文件名，所以直接忽略异常了- -
                }
            }

            return new Tuple<string, string, string, string, string>(songName, artist, real_loc, lyric_str, songId);
        }

        public List<Tuple<string, string, string, string, string>> GetCollection(string id)
        {
            var request_url = string.Format(CollectUrl, id);
            var result_json = HttpGet(request_url);
            var songList = new List<Tuple<string, string, string, string, string>>();
            if (result_json.Length == 0)
                return null;
            var json = JObject.Parse(result_json);
            if (json["data"]["trackList"].HasValues == false)
                return null;
            foreach (var song in json["data"]["trackList"] as JArray)
            {
                var songInfo = ParseJsonSongInfo(song);
                if (songInfo != null)
                {
                    songList.Add(songInfo);
                }
            }
            return songList;
        }

        public static Regex pat_baidu_collection = new Regex("https://www.xiami.com/collect/([^\'\"\\?]+)");
        public string SearchCollection(string txt)
        {
            var result_txt = HttpGet(string.Format(searchUrl_Baidu, txt + Uri.EscapeUriString(" site:www.xiami.com")));
            result_txt = result_txt.Substring(result_txt.IndexOf("<body"));//略过head部分提高效率?
            var result_1 = pat_baidu_collection.Match(result_txt);
            if (!result_1.Success)
                return null;
            return result_1.Groups[1].ToString();
        }
        
        public static Regex pat_baidu_2 = new Regex("https://www.xiami.com/song/([^\'\"\\?]+)");
        public string SearchSong(string txt)
        {
            var result_txt = HttpGet(string.Format(searchUrl_Baidu, txt + Uri.EscapeUriString(" site:www.xiami.com")));
            result_txt = result_txt.Substring(result_txt.IndexOf("<body"));//略过head部分提高效率?
            var result_1 = pat_baidu_2.Match(result_txt);
            if (!result_1.Success)
                return SearchSong_Legacy(txt);
            var result = result_1.Groups[1].ToString();
            //Verify
            var songInfo = getSongInfo(result);
            txt = Uri.UnescapeDataString(txt);
            if (songInfo == null || txt.IndexOf(songInfo.Item1.Replace(" ","+")) == -1)
                return SearchSong_Legacy(txt);
            return result;
        }



        public static Regex pat = new Regex("<td class=\"song_name\">[\r\n\t ]*<a target=\"_blank\" href=\"//www.xiami.com/song/([^\"?]+)\"");
        public string SearchSong_Legacy(string txt)
        {
            var result_txt = HttpGet(string.Format(searchUrl, txt));
            var result = pat.Match(result_txt);
            if (result.Success)
                return result.Groups[1].ToString();
            else
                return "";
        }

        public string xiamiUrlDecode(string str)
        {
            int rows = str[0] - '0';
            var url = str.Substring(1);
            var len_url = url.Length;
            var cols = len_url / rows;
            var re_col = len_url % rows;
            StringBuilder ret_url = new StringBuilder();

            for (int i = 0; i < len_url; i++)
            {
                int index = (i % rows) * (cols + 1) + (i / rows);
                if ((i % rows) >= re_col)
                {
                    index -= (i % rows) - re_col;
                }
                if (index >= url.Length)
                    ret_url.Append('-');
                else
                    ret_url.Append(url[index]);
            }

            var ret = ret_url.ToString();
            ret = Uri.UnescapeDataString(ret);
            ret = ret.Replace("^", "0");
            if (!ret.StartsWith("http:"))
                ret = "http:" + ret;
            return ret;
        }


        //From:https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows
        static Regex containsABadCharacter = new Regex("["
                                                + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");
        public static bool IsValidFilename(string testName)
        {
            
            if (containsABadCharacter.IsMatch(testName)) { return false; };

            // other checks for UNC, drive-path format, etc

            return true;
        }

        protected string HttpPost(string uri, FormUrlEncodedContent content)
        {
            content.Headers.Add("Referer", uri.Replace("/cat/json", "").Replace(xiamiUrl, playUrl));
            var result = _client.PostAsync(uri, content).Result;
            if (result.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                var rawResult = result.Content.ReadAsByteArrayAsync().Result;
                var finResult = GZipHelper.Decompress_GZip(rawResult);
                return Encoding.UTF8.GetString(finResult);
            }

            return result.Content.ReadAsStringAsync().Result;
        }


        private object context_lock = new object();
        protected string HttpGet(string uri)
        {
            string hoststr = uri.Replace("https://", "").Replace("http://", "");
            Task<HttpResponseMessage> task;
            lock (context_lock)
            {
                hoststr = hoststr.Substring(0, hoststr.IndexOf("/"));
                _client.DefaultRequestHeaders.Host = hoststr;
                _client.DefaultRequestHeaders.Referrer =
                    new Uri(uri.Replace("/cat/json", "").Replace(xiamiUrl, playUrl));
                task = _client.GetAsync(uri);
            }

            var result = task.Result;
            if (result.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                var rawResult = result.Content.ReadAsByteArrayAsync().Result;
                var finResult = GZipHelper.Decompress_GZip(rawResult);
                return Encoding.UTF8.GetString(finResult);
            }

            return result.Content.ReadAsStringAsync().Result;
        }
    }

    public static class GZipHelper
    {
        //From : https://stackoverflow.com/questions/13879911/decompress-a-gzip-compressed-http-response-chunked-encoding
        public static byte[] Decompress_GZip(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                byte[] buffer = new byte[1024];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, 1024);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
