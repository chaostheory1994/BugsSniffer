using System.IO;
using System.Text.RegularExpressions;

namespace BugsSniffer.Api.Models
{
    public class Track : Metadata
    {
        public override string FileName => $"{TrackNumber?.ToString("D2")} {Title}";

        public string Title { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }
        public string Composer { get; set; }

        public int? TrackNumber { get; set; }
        public int? TrackTotal { get; set; }

        public string Year { get; set; }
        public string Copyright { get; set; }

        public int? DiscNumber { get; set; }
        public int? DiscTotal { get; set; }

        public string Genre { get; set; }

        public string Lyrics { get; set; }
        public string Lyricist { get; set; }


        public string AlbumArtUrl { get; set; }
    }
}