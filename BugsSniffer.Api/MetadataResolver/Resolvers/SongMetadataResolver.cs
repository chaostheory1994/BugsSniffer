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
    public class SongMetadataResolver : IMetadataResolver
    {
        private readonly HttpClient _client;
        private readonly ILogger<SongMetadataResolver> _logger;
        private readonly string _endpoint;

        public SongMetadataResolver(HttpClient client, ILoggerFactory factory)
        {
            _client = client;
            _logger = factory.CreateLogger<SongMetadataResolver>();
            _endpoint = "https://api.bugs.co.kr/3/tracks/";
            SupportedFileTypes = new List<string>
            {
                FileTypes.Flac,
                FileTypes.M4a
            };
        }

        public List<string> SupportedFileTypes { get; }

        public async Task<Metadata> GetMetadata(string id)
        {
            try
            {
                _logger.LogInformation($"Attempting to get track metadata for track {id}.");
                HttpRequestMessage request = new HttpRequestMessage();
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri($"{_endpoint}{id}");

                HttpResponseMessage response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Track information recieved for track {id}");

                TrackJson track = JsonConvert.DeserializeObject<TrackJson>(responseJson);

                return new Track
                {
                    Album = track.result.album_title,
                    Artist = track.result.artist_nm,
                    DiscNumber = track.result.disc_id,
                    Lyrics = track.result.normal_lyrics,
                    Title = track.result.track_title_original,
                    TrackNumber = track.result.track_no,
                    Year = track.result.release_ymd.Substring(0, 4),
                    AlbumArtUrl = track.result.img_urls.original
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured trying to download the file.");
                return null;
            }
        }

        public class TrackJson
        {
            public Result result { get; set; }
            public int ret_code { get; set; }
            public string ret_msg { get; set; }
            public string ret_detail_msg { get; set; }
        }

        public class Result
        {
            public int track_id { get; set; }
            public int disc_id { get; set; }
            public int track_no { get; set; }
            public string track_title { get; set; }
            public string title_yn { get; set; }
            public string len { get; set; }
            public string adult_yn { get; set; }
            public string lyrics_tp { get; set; }
            public int album_id { get; set; }
            public int artist_id { get; set; }
            public string svc_192_yn { get; set; }
            public string svc_320_yn { get; set; }
            public string svc_flac_yn { get; set; }
            public object search_title { get; set; }
            public string amp3_yn { get; set; }
            public int price { get; set; }
            public string fullhd_rights_yn { get; set; }
            public string hd_rights_yn { get; set; }
            public string sd_rights_yn { get; set; }
            public string release_ymd { get; set; }
            public string artist_nm { get; set; }
            public string artist_disp_nm { get; set; }
            public string album_title { get; set; }
            public string album_artist_nm { get; set; }
            public int mv_id { get; set; }
            public string mv_status { get; set; }
            public string svc_sd_yn { get; set; }
            public string svc_hd_yn { get; set; }
            public string svc_fullhd_yn { get; set; }
            public string mv_adult_yn { get; set; }
            public int agency_id { get; set; }
            public bool is_buy { get; set; }
            public long upd_dt { get; set; }
            public int popular { get; set; }
            public string multi_artist_yn { get; set; }
            public string multi_mv_yn { get; set; }
            public string svc_flac_sample_rate { get; set; }
            public float track_gain { get; set; }
            public int comment_group_id { get; set; }
            public string track_title_original { get; set; }
            public string album_tp { get; set; }
            public object cast_info { get; set; }
            public object bside_info { get; set; }
            public object classic_info { get; set; }
            public int track_play_count { get; set; }
            public int like_count { get; set; }
            public object artist_type { get; set; }
            public bool logged { get; set; }
            public bool likes { get; set; }
            public string normal_lyrics { get; set; }
            public string time_lyrics { get; set; }
            public object search_lyrics { get; set; }
            public string bside_yn { get; set; }
            public Img_Urls img_urls { get; set; }
            public bool track_str_rights { get; set; }
            public bool track_dnl_rights { get; set; }
            public bool track_rent_rights { get; set; }
            public bool is_album_buy { get; set; }
            public bool is_str_premium { get; set; }
            public bool is_save_premium { get; set; }
            public bool is_ppd { get; set; }
            public bool is_dnl_premium { get; set; }
            public bool is_ppv { get; set; }
            public bool is_mv_premium { get; set; }
            public bool is_pps { get; set; }
            public string svc_flac24_yn { get; set; }
            public string svc_aac256_yn { get; set; }
            public string artist_link_yn { get; set; }
            public string album_link_yn { get; set; }
            public bool is_track_buy { get; set; }
            public bool is_flac_str_premium { get; set; }
            public bool is_mv_dnl_rights { get; set; }
            public bool mv_str_rights { get; set; }
        }

        public class Img_Urls
        {
            public string _200 { get; set; }
            public string original { get; set; }
            public string _140 { get; set; }
            public string _1000 { get; set; }
            public string _350 { get; set; }
            public string _75 { get; set; }
            public string _500 { get; set; }
        }
    }
}