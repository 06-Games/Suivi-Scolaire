using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Integrations
{
    public class Account : IEquatable<Account>
    {
        internal static readonly Dictionary<string, Provider> Providers = new Dictionary<string, Provider> {
            { "Local", new Local() },
            { "EcoleDirecte", new EcoleDirecte() },
            { "CambridgeKids", new CambridgeKids() }
        };

        public string provider;
        public Provider GetProvider => Providers.TryGetValue(provider, out var p) ? p : null;
        public string username;
        public string id;
        public string password;
        public string child;

        public bool Equals(Account other) => provider == other.provider && username == other.username && id == other.id;
        public override bool Equals(object obj) => Equals(obj as Account);
        public override int GetHashCode() => string.Format("{0}-{1}-{2}", provider, username, id).GetHashCode();
    }


    public static class ProviderExtension
    {
        static string[] whiteList = new[] { "Auth" };
        public static bool TryGetModule<T>(this Provider provider, out T module)
        {
            module = GetModule<T>(provider);
            return !module?.Equals(default) ?? false;
        }
        public static T GetModule<T>(this Provider provider)
        {
            var moduleName = typeof(T).ToString().Substring("Integrations.".Length);
            if (!whiteList.Contains(moduleName) && (!Manager.Child.modules?.Contains(moduleName) ?? false)) return default;
            try { return (T)provider; }
            catch { return default; }
        }

        public static string HtmlToRichText(string Html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Html);
            return AnalyseNode(htmlDoc.DocumentNode) ?? "";

            string AnalyseNode(HtmlNode node)
            {
                var result = new System.Text.StringBuilder();
                if (!node.HasChildNodes) return System.Net.WebUtility.HtmlDecode(node.InnerText);
                foreach (var item in node.ChildNodes)
                {
                    var itemName = item.Name.ToLower();
                    if (itemName == "p" || itemName == "div") result.AppendLine(AnalyseNode(item));
                    else if (itemName == "strong") result.Append($"<b>{AnalyseNode(item)}</b>");
                    else if (itemName == "em") result.Append($"<i>{AnalyseNode(item)}</i>");
                    else if (itemName == "u") result.Append($"<u>{AnalyseNode(item)}</u>");
                    else if (itemName == "a") result.Append($"<link={item.Attributes["href"].Value}>{AnalyseNode(item)}</link>");
                    else if (itemName == "br") result.AppendLine("");
                    else if (itemName == "ul") result.Append(AnalyseNode(item));
                    else if (itemName == "li") result.AppendLine($"• {AnalyseNode(item)}");
                    else if (itemName == "span" || itemName == "body")
                    {
                        string style = item.Attributes["style"]?.Value.ToLower() ?? "";
                        if (style.StartsWith("font-size:"))
                        {
                            var value = style.Substring("font-size:".Length).TrimStart(' ').TrimEnd(';');
                            var medium = 20F;
                            var parsedValue = medium;
                            if (value.EndsWith("px")) parsedValue = float.TryParse(value.Replace("px", ""), out var vFloat) ? vFloat : medium;
                            else if (value == "xx-small") parsedValue = 0.6F * medium;
                            else if (value == "x-small") parsedValue = 0.75F * medium;
                            else if (value == "small") parsedValue = 8F / 9F * medium;
                            else if (value == "medium") parsedValue = 1F * medium;
                            else if (value == "large") parsedValue = 1.2F * medium;
                            else if (value == "x-large") parsedValue = 1.5F * medium;
                            else if (value == "xx-large") parsedValue = 2F * medium;
                            result.Append($"<size={parsedValue}>{AnalyseNode(item)}</size>");
                        }
                        else if (style.StartsWith("color:"))
                        {
                            var value = style.Substring("color:".Length).TrimStart(' ').TrimEnd(';');
                            if (value.StartsWith("rgb("))
                                value = "#" + string.Join("", value.Substring("rgb(".Length).TrimEnd(')').Replace(" ", "").Split(',').Select(d => byte.TryParse(d, out var b) ? b.ToString("X2") : "00"));
                            else if (value.StartsWith("rgba("))
                                value = "#" + string.Join("", value.Substring("rgba(".Length).TrimEnd(')').Split(',').Cast<byte>().Select(d => d.ToString("X2")));
                            result.Append($"<color={value}>{AnalyseNode(item)}</color>");
                        }
                        else result.Append(AnalyseNode(item));
                    }
                    else if (item.NodeType == HtmlNodeType.Text) result.Append(AnalyseNode(item));
                    else if (item.NodeType == HtmlNodeType.Comment) return "";
                    else
                    {
                        Logging.Log("Unknown HTML element: " + itemName + "\nAt: " + item.OuterHtml, UnityEngine.LogType.Warning);
                        result.Append(AnalyseNode(item));
                    }
                }
                return result.ToString();
            }
        }
        public static string RemoveEmptyLines(string lines) => lines == null ? "" : System.Text.RegularExpressions.Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline).TrimEnd();
    }

    public interface Provider
    {
        string Name { get; }
        IEnumerator Connect(Account account, Action<Data.Data> onComplete, Action<string> onError);
    }
    public interface Auth : Provider { }
    public interface Periods : Provider { IEnumerator GetPeriods(Action onComplete = null); }
    public interface Schedule : Provider { IEnumerator GetSchedule(TimeRange period, Action<IEnumerable<Data.ScheduledEvent>> onComplete = null); }
    public interface Homeworks : Provider
    {
        IEnumerator GetHomeworks(Data.Homework.Period period, Action onComplete);
        IEnumerator<Data.Homework.Period> DiaryPeriods();
    }
    public interface Marks : Provider { IEnumerator GetMarks(Action onComplete = null); }
    public interface Messanging : Provider
    {
        IEnumerator GetMessages(Action onComplete = null);
        IEnumerator LoadExtraMessageData(uint messageID, Action onComplete = null);
    }
    public interface Books : Provider { IEnumerator GetBooks(Action onComplete); }
    public interface Documents : Provider { IEnumerator GetDocuments(Action onComplete); }
}
