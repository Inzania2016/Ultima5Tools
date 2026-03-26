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
                "  tlk dump <path>        (auto-loads sibling DATA.OVL when present)",
                "  npc dump <path>        (auto-loads sibling DATA.OVL when present)",
                "  ovl info <path>",
                "  gam diff <leftPath> <rightPath>",
                "  dat info <path>",
                "  map render <path>");
        }
    }
}
