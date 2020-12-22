namespace Integrations.Renderer
{
    public static class Markdown
    {
        public static string ToHtml(string markdown) => CommonMark.CommonMarkConverter.Convert(markdown);
        public static string ToRichText(string markdown) => HTML.ToRichText(ToHtml(markdown).Replace("\r", "").Replace("\n", "")); // Quite heavy but I have no other solution for the moment
    }
}
