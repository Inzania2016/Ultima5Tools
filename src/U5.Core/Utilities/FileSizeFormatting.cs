namespace U5.Core.Utilities
{
    /// <summary>
    /// Human-readable file size formatting for reports.
    /// </summary>
    public static class FileSizeFormatting
    {
        public static string ToHumanReadable(long bytes)
        {
            string[] units = new[] { "B", "KB", "MB", "GB" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.##} {units[unitIndex]}";
        }
    }
}
