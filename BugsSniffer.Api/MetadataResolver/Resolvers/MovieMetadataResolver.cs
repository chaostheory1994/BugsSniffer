using BugsSniffer.Api.Constants;
using BugsSniffer.Api.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace BugsSniffer.Api.MetadataResolver.Resolvers
{
    public class MovieMetadataResolver : IMetadataResolver
    {
        private readonly HttpClient _client;
        private readonly ILogger<SongMetadataResolver> _logger;
        private readonly string _endpoint;

        public MovieMetadataResolver(HttpClient client, ILoggerFactory factory)
        {
            _client = client;
            _logger = factory.CreateLogger<SongMetadataResolver>();
            _endpoint = "https://api.bugs.co.kr/3/mvs/";
            SupportedFileTypes = new List<string>
            {
                FileTypes.Mp4
            };
        }

        public List<string> SupportedFileTypes { get; }

        public async Task<Metadata> GetMetadata(string id)
        {
            try
            {
                _logger.LogInformation($"Attempting to get movie metadata for track {id}.");
                HttpRequestMessage request = new HttpRequestMessage();
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri($"{_endpoint}{id}");

                HttpResponseMessage response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Movie information recieved for movie {id}");

                MovieJson movie = JsonConvert.DeserializeObject<MovieJson>(responseJson);

                return new Movie
                {
                    Title = movie.result.mv_title,
                    Artist = movie.result.mv_main_artist_nm
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured trying to download the file.");
                return null;
            }
        }

        private class MovieJson
        {
            public Result result { get; set; }
            public Info info { get; set; }
            public int ret_code { get; set; }
            public string ret_msg { get; set; }
            public string ret_detail_msg { get; set; }
        }

        private class Result
        {
            public int mv_id { get; set; }
            public string album_tp { get; set; }
            public int track_id { get; set; }
            public int artist_id { get; set; }
            public string mv_title { get; set; }
            public string nation_cd { get; set; }
            public string attr_tp { get; set; }
            public string attr_tp_nm { get; set; }
            public object actor { get; set; }
            public object dscr { get; set; }
            public string release_ymd { get; set; }
            public string media_yn { get; set; }
            public string svc_fullhd_yn { get; set; }
            public string svc_sd_yn { get; set; }
            public string svc_hd_yn { get; set; }
            public string svc_mp4_yn { get; set; }
            public object artist_type { get; set; }
            public int album_id { get; set; }
            public string multi_artist_yn { get; set; }
            public long upd_dt { get; set; }
            public string fullhd_rights_yn { get; set; }
            public string hd_rights_yn { get; set; }
            public string sd_rights_yn { get; set; }
            public int mv_fullhd_price { get; set; }
            public int mv_hd_price { get; set; }
            public int mv_sd_price { get; set; }
            public object mv_lyrics { get; set; }
            public string mv_grade { get; set; }
            public int comment_group_id { get; set; }
            public int like_count { get; set; }
            public int comment_cnt { get; set; }
            public int play_count { get; set; }
            public object bside_info { get; set; }
            public string mv_main_artist_nm { get; set; }
            public bool likes { get; set; }
            public string artist_disp_nm { get; set; }
            public string album_title { get; set; }
            public Img_Urls img_urls { get; set; }
            public string bside_yn { get; set; }
            public string mv_adult_yn { get; set; }
            public Hd_Img_Urls hd_img_urls { get; set; }
            public string mv_str_rights { get; set; }
            public bool is_ppv { get; set; }
            public bool is_mv_premium { get; set; }
            public string mv_duration { get; set; }
            public string artist_link_yn { get; set; }
            public string album_link_yn { get; set; }
            public bool is_mv_dnl_rights { get; set; }
            public string track_link_yn { get; set; }
        }

        private class Img_Urls
        {
            public string _200 { get; set; }
            public string _70 { get; set; }
            public string _80 { get; set; }
            public string _60 { get; set; }
            public string _100 { get; set; }
            public string _500 { get; set; }
        }

        private class Hd_Img_Urls
        {
            public string _200 { get; set; }
            public string _140 { get; set; }
            public string _350 { get; set; }
            public string _100 { get; set; }
            public string _75 { get; set; }
            public string _500 { get; set; }
        }

        private class Info
        {
            public int mv_id { get; set; }
            public int comment_group_id { get; set; }
            public int like_count { get; set; }
            public int comment_cnt { get; set; }
            public int play_count { get; set; }
            public object descr { get; set; }
        }

    }
}
