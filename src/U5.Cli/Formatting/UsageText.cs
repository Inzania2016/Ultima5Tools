namespace U5.Cli
{
    public static class UsageText
    {
        public static string Get()
        {
            return string.Join(
                Environment.NewLine,
                "Ultima5Tools CLI",
                "Usage:",
                "  tlk dump <path>",
                "  npc dump <path>",
                "  gam diff <leftPath> <rightPath>",
                "  dat info <path>",
                "  map render <path>");
        }
    }
}
