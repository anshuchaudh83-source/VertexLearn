using System.Text.RegularExpressions;

namespace VertexLearn.Infrastructure
{
    public static class MediaHelper
    {
        private static readonly Regex YouTubeId = new(
            @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Turns a normal YouTube link into a privacy-enhanced embed URL, or null if it isn't YouTube.</summary>
        public static string? ToYouTubeEmbed(string url)
        {
            var match = YouTubeId.Match(url ?? string.Empty);
            return match.Success ? $"https://www.youtube-nocookie.com/embed/{match.Groups[1].Value}" : null;
        }

        public static bool IsVideoFile(string url) =>
            Regex.IsMatch(url ?? string.Empty, @"\.(mp4|webm|ogg)$", RegexOptions.IgnoreCase);

        /// <summary>Short label + symbol shown on resource badges.</summary>
        public static string TypeIcon(string type) => type switch
        {
            "Video" => "▶",
            "PDF" => "📄",
            "Link" => "🔗",
            "Notes" => "📝",
            "Image" => "🖼",
            "Audio" => "♪",
            _ => "•"
        };

        public static bool IsExternal(string url) => url.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }
}
