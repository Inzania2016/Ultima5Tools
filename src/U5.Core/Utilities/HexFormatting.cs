namespace U5.Core.Utilities
{
    /// <summary>
    /// Helpers for rendering byte arrays as hex text.
    /// </summary>
    public static class HexFormatting
    {
        public static string ToHex(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            return BitConverter.ToString(bytes).Replace("-", " ");
        }
    }
}
