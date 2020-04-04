﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Integrations
{
    public class Account : IEquatable<Account>
    {
        public static Dictionary<string, Provider> Providers = new Dictionary<string, Provider>() {
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
    public class ChildAccount
    {
        public string id;
        public string name;
        [System.Xml.Serialization.XmlIgnore] public UnityEngine.Sprite image;

        public bool Equals(ChildAccount other) => id == other.id && name == other.name && image == other.image;
        public override bool Equals(object obj) => Equals(obj as ChildAccount);
        public override int GetHashCode() => string.Format("{0}-{1}", id, name).GetHashCode();
    }


    public static class ProviderExtension
    {
        public static bool TryGetModule<T>(this Provider provider, out T module)
        {
            module = GetModule<T>(provider);
            return !module?.Equals(null) ?? false;
        }
        public static T GetModule<T>(this Provider provider)
        {
            try { return (T)provider; }
            catch { return default; }
        }
        public static IEnumerable<string> Modules(this Provider provider) => provider.GetType().GetInterfaces().Where(i => i.Namespace == "Integrations").Select(i => i.ToString().Substring("Integrations.".Length));


        public static string HtmlToRichText(string html)
        {
            html = html.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("&", "£|µ");
            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml($"<root>{html}</root>");
                return System.Net.WebUtility.HtmlDecode(AnalyseNode(xmlDoc?.DocumentElement)?.Replace("£|µ", "&") ?? "");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e + "\n\n" + html);
                return html;
            }

            string AnalyseNode(System.Xml.XmlNode node)
            {
                var result = "";
                if (!node.HasChildNodes) return node.Value;
                foreach (System.Xml.XmlNode item in node)
                {
                    if (item.Name == "p" | item.Name == "div") result += AnalyseNode(item) + "\n";
                    else if (item.Name == "strong") result += $"<b>{AnalyseNode(item)}</b>";
                    else if (item.Name == "em") result += $"<i>{AnalyseNode(item)}</i>";
                    else if (item.Name == "a") result += $"<link={item.Attributes["href"].Value}>{AnalyseNode(item)}</link>";
                    else if (item.Name == "li") result += $"• {AnalyseNode(item)}\n";
                    else if (item.Name == "span")
                    {
                        string style = item.Attributes["style"]?.Value ?? "";
                        if (style.StartsWith("font-size:")) result += $"<size={style.Substring("font-size:".Length).TrimStart(' ').Replace("px;", "")}>{AnalyseNode(item)}</size>";
                        else if (style.StartsWith("color:"))
                        {
                            var value = style.Substring("color:".Length).TrimStart(' ').TrimEnd(';');
                            if (value.StartsWith("rgb("))
                                value = "#" + string.Join("", value.Substring("rgb(".Length).TrimEnd(')').Replace(" ", "").Split(',').Select(d => byte.TryParse(d, out var b) ? b.ToString("X2") : "00"));
                            else if (value.StartsWith("rgba("))
                                value = "#" + string.Join("", value.Substring("rgba(".Length).TrimEnd(')').Split(',').Cast<byte>().Select(d => d.ToString("X2")));
                            result += $"<color={value}>{AnalyseNode(item)}</color>";
                        }
                        else result += AnalyseNode(item);
                    }
                    else result += AnalyseNode(item);
                }
                return result;
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
