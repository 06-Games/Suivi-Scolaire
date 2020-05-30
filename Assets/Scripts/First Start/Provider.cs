using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Integrations
{
    public class Account : IEquatable<Account>
    {
        internal static readonly Dictionary<string, Provider> Providers = new Dictionary<string, Provider> {
            //{ "Local", new Local() },
            { "EcoleDirecte", new EcoleDirecte() },
            { "CambridgeKids", new CambridgeKids() }
        };

        public string provider;
        public Provider GetProvider => Providers.TryGetValue(provider, out var p) ? p : null;
        public string username;
        public string id;
        public string password;
        public ChildAccount child;

        public bool Equals(Account other) => provider == other.provider && username == other.username && id == other.id;
        public override bool Equals(object obj) => Equals(obj as Account);
        public override int GetHashCode() => string.Format("{0}-{1}-{2}", provider, username, id).GetHashCode();
    }
    public class ChildAccount : IEquatable<ChildAccount>
    {
        public string id;
        public string name;
        [System.Xml.Serialization.XmlIgnore] public UnityEngine.Sprite image;
        [System.Xml.Serialization.XmlIgnore] public List<string> modules;
        [System.Xml.Serialization.XmlIgnore] public Dictionary<string, string> extraData;

        public bool Equals(ChildAccount other) => id == other.id && name == other.name && image == other.image;
        public override bool Equals(object obj) => Equals(obj as ChildAccount);
        public override int GetHashCode() => string.Format("{0}-{1}", id, name).GetHashCode();
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
            if (!whiteList.Contains(moduleName) && (!FirstStart.selectedAccount?.child?.modules?.Contains(moduleName) ?? false)) return default;
            try { return (T)provider; }
            catch { return default; }
        }

        public static string HtmlToRichText(string Html)
        {
            var html = Html.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("&", "£|µ");
            try { return AnalyseHTML(); }
            catch //The HTML isn't well formed
            {
                try { FixHTML(); return AnalyseHTML(); } //Try to fix the HTML
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + "\n\n" + html + "\n\n" + Html);
                    return Html;
                }
            }

            void FixHTML()
            {
                html = string.Join("=",
                    html.Split(new[] { '=' }).Skip(1)
                    .Select(p =>
                    {
                        var spaceNb = p.Count(x => x == '"');
                        if (spaceNb != 0 && (spaceNb % 2F) != 0) return p; //We are in a string

                        var end = p.IndexOfAny(new[] { ' ', '>' });
                        return p.StartsWith("\"") ? p : $"\"{p.Substring(0, end == -1 ? 0 : end)}\"{p.Substring(end == -1 ? 0 : end)}";
                    })
                    .Prepend(html.Substring(0, html.IndexOf('=') == -1 ? html.Length : html.IndexOf('=')))
                );
                foreach (var t in new[] { "img", "br" }.SelectMany(s => new[] { s, s.ToUpper() }))
                {
                    html = string.Join($"<{t}",
                        html.Split(new[] { $"<{t}" }, StringSplitOptions.RemoveEmptyEntries).Skip(1)
                        .Select(p =>
                        {
                            if (p.Contains($"</{t}>")) return p; //Well formed html

                            var end = p.IndexOfAny(new[] { '>' });
                            return p[end <= 0 ? 0 : end - 1] == '/' ? p : $"{p.Substring(0, end <= 0 ? 0 : end)}/{p.Substring(end <= 0 ? 0 : end)}";
                        })
                        .Prepend(html.Substring(0, html.IndexOf($"<{t}") == -1 ? html.Length : html.IndexOf($"<{t}")))
                    );
                }
            }

            string AnalyseHTML()
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml($"<root>{html}</root>");
                return System.Net.WebUtility.HtmlDecode(AnalyseNode(xmlDoc?.DocumentElement)?.Replace("£|µ", "&") ?? "");
            }
            string AnalyseNode(System.Xml.XmlNode node)
            {
                var result = new System.Text.StringBuilder();
                if (!node.HasChildNodes) return node.Value;
                foreach (System.Xml.XmlNode item in node)
                {
                    var itemName = item.Name.ToLower();
                    if (itemName == "p" || itemName == "div") result.AppendLine(AnalyseNode(item));
                    else if (itemName == "strong") result.Append($"<b>{AnalyseNode(item)}</b>");
                    else if (itemName == "em") result.Append($"<i>{AnalyseNode(item)}</i>");
                    else if (itemName == "a") result.Append($"<link={item.Attributes["href"].Value}>{AnalyseNode(item)}</link>");
                    else if (itemName == "br") result.AppendLine("");
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
                    else
                    {
                        Logging.Log("Unknown HTML element: " + itemName, UnityEngine.LogType.Warning);
                        result.Append(AnalyseNode(item));
                    }
                }
                return result.ToString();
            }
        }
        public static string RemoveEmptyLines(string lines) => lines == null ? "" : System.Text.RegularExpressions.Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline).TrimEnd();
    }

    public interface Provider { string Name { get; } }
    public interface Auth : Provider
    {
        IEnumerator Connect(Account account, Action<Account, List<ChildAccount>> onComplete, Action<string> onError);
    }
    public interface Periods : Provider { IEnumerator GetPeriods(Action<List<global::Periods.Period>> onComplete); }
    public interface Schedule : Provider { IEnumerator GetSchedule(TimeRange period, Action<List<global::Schedule.Event>> onComplete); }
    public interface Homeworks : Provider
    {
        IEnumerator GetHomeworks(global::Homeworks.Period period, Action<List<global::Homeworks.Homework>> onComplete);
        IEnumerator<global::Homeworks.Period> DiaryPeriods();
    }
    public interface Marks : Provider { IEnumerator GetMarks(Action<List<global::Marks.Period>, List<Subject>, List<global::Marks.Mark>> onComplete); }
    public interface Messanging : Provider
    {
        IEnumerator GetMessages(Action<List<global::Messanging.Message>> onComplete);
        IEnumerator LoadExtraMessageData(global::Messanging.Message data, Action<global::Messanging.Message> onComplete);
    }
}
