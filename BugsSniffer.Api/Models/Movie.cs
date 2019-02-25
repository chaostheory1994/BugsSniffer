namespace BugsSniffer.Api.Models
{
    public class Movie : Metadata
    {
        public override string FileName => Title;

        public string Title { get; set; }
        public string Artist { get; set; }
    }
}
