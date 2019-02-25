using BugsSniffer.Api.Constants;
using BugsSniffer.Api.Models;
using LibFlacSharp;
using LibFlacSharp.Metadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BugsSniffer.Api.MetadataApplier.Appliers
{
    public class FlacMetadataApplier : IMetadataApplier
    {
        private readonly ILogger<FlacMetadataApplier> _logger;

        public FlacMetadataApplier(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<FlacMetadataApplier>();
            SupportedFileTypes = new List<string>
            {
                FileTypes.Flac
            };
        }

        public List<string> SupportedFileTypes { get; }

        public async Task ApplyMetadata(string filePath, string filename, Track metadata)
        {
            try
            {
                string fullPath = Path.Combine(filePath, filename);

                using (MemoryStream modifiedFlac = new MemoryStream())
                {
                    using (FileStream originalFile = File.Open(fullPath, FileMode.Open))
                    {
                        FlacFile flac = new FlacFile(originalFile);

                        string albumArtLocation = Path.Combine(filePath, $"{metadata.Album}{Path.GetExtension(metadata.AlbumArtUrl)}");
                        if (File.Exists(albumArtLocation))
                        {
                            _logger.LogInformation("Album art was discovered. Adding it to the flac.");
                            using (FileStream stream = File.Open(albumArtLocation, FileMode.Open))
                            {
                                flac.AddPicture(stream);
                            }
                        }

                        flac.VorbisComment.CommentList[VorbisCommentType.Title] = metadata?.Title;
                        flac.VorbisComment.CommentList[VorbisCommentType.TrackNumber] = metadata.TrackNumber?.ToString();
                        flac.VorbisComment.CommentList[VorbisCommentType.TrackTotal] = metadata.TrackTotal?.ToString();
                        flac.VorbisComment.CommentList[VorbisCommentType.Year] = metadata.Year;
                        flac.VorbisComment.CommentList[VorbisCommentType.Album] = metadata.Album;
                        flac.VorbisComment.CommentList[VorbisCommentType.Artist] = metadata.Artist;
                        flac.VorbisComment.CommentList[VorbisCommentType.Composer] = metadata.Composer;
                        flac.VorbisComment.CommentList[VorbisCommentType.Copyright] = metadata.Copyright;
                        flac.VorbisComment.CommentList[VorbisCommentType.DiscNumber] = metadata.DiscNumber?.ToString();
                        flac.VorbisComment.CommentList[VorbisCommentType.DiscTotal] = metadata.DiscTotal?.ToString();
                        flac.VorbisComment.CommentList[VorbisCommentType.Genre] = metadata.Genre;
                        flac.VorbisComment.CommentList[VorbisCommentType.Lyricist] = metadata.Lyricist;
                        flac.VorbisComment.CommentList[VorbisCommentType.Lyrics] = metadata.Lyrics;

                        await flac.SaveAsync(modifiedFlac);
                    }

                    modifiedFlac.Position = 0;

                    using (FileStream originalFile = File.Open(fullPath, FileMode.Create))
                    {
                        await modifiedFlac.CopyToAsync(originalFile);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to apply metadata.");
            }

            return;
        }
    }
}