using HtmlAgilityPack;
using System.Linq;

namespace Integrations.Renderer
{
    public static class HTML
    {
        public static string ToRichText(string Html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Html);
            return AnalyseNode(htmlDoc.DocumentNode) ?? "";

            string AnalyseNode(HtmlNode node)
            {
                string NodeName(HtmlNode n) => n.Name.ToLower();
                var result = new System.Text.StringBuilder();
                if (!node.HasChildNodes) return System.Net.WebUtility.HtmlDecode(node.InnerText);
                foreach (var item in node.ChildNodes)
                {
                    var itemName = NodeName(item);
                    if (itemName == "p" || itemName == "div") result.AppendLine(AnalyseNode(item));
                    else if (itemName == "strong" || itemName == "b") result.Append($"<b>{AnalyseNode(item)}</b>");
                    else if (itemName == "em" || itemName == "i") result.Append($"<i>{AnalyseNode(item)}</i>");
                    else if (itemName == "u") result.Append($"<u>{AnalyseNode(item)}</u>");
                    else if (itemName == "a") result.Append($"<link={item.Attributes["href"].Value}>{AnalyseNode(item)}</link>");
                    else if (itemName == "br") result.AppendLine("");
                    else if (itemName == "ul") result.Append(AnalyseNode(item));
                    else if (itemName == "li") result.AppendLine($"• {AnalyseNode(item)}");
                    else if (itemName == "h1") result.AppendLine($"<style=H1>{AnalyseNode(item)}</style>");
                    else if (itemName == "h2") result.AppendLine($"<style=H2>{AnalyseNode(item)}</style>");
                    else if (itemName == "h3") result.AppendLine($"<style=H3>{AnalyseNode(item)}</style>");
                    else if (itemName == "span" || itemName == "font" || itemName == "body")
                    {
                        string styleAttribute = item.GetAttributeValue("style", null);
                        var css = new System.Collections.Generic.List<string>();
                        css.AddRange(styleAttribute?.Split(new[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim(' ')) ?? System.Array.Empty<string>());
                        css.AddRange(item.Attributes.Where(a => a.Name != "style").Select(a => $"{a.Name}={a.Value}") ?? System.Array.Empty<string>());
                        if (css.Count == 0) result.Append(AnalyseNode(item));
                        else
                        {
                            var styles = css.Select(s => AnalyseCSS(s));
                            result.Append($"{string.Join("", styles.Select(s => s.Item1))}{AnalyseNode(item)}{string.Join("", styles.Select(s => s.Item2))}");
                        }

                        (string, string) AnalyseCSS(string style)
                        {
                            var property = style.Split(':', '=').FirstOrDefault().ToLower();
                            var value = style.Substring(property.Length + 1).Trim(' ');
                            if (property == "font-size" || property == "size")
                            {
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
                                return ($"<size={parsedValue}>", "</size>");
                            }
                            else if (property == "color" || property == "text")
                            {
                                UnityEngine.ColorUtility.TryParseHtmlString(value, out var c);
                                UnityEngine.Color.RGBToHSV(c, out var h, out var s, out var v);
                                var color = UnityEngine.Color.HSVToRGB(h, s, v < 0.5F ? 1 - v : v);
                                color.a = c.a;
                                return ($"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(color)}>", "</color>");
                            }
                            else if (property.StartsWith("mso-")) return ("", ""); //MS Office related style
                            else Logging.Log("Unknown CSS element: " + property + "\nAt: " + item.OuterHtml, UnityEngine.LogType.Warning); return ("", "");
                        }
                    }
                    else if (itemName == "table")
                    {
                        try
                        {
                            var ITEM = item.FirstChild;
                            if (ITEM.Name != "tr") ITEM = ITEM.FirstChild;
                            var cellNumber = ITEM.ChildNodes.Sum(c => int.TryParse(c.Attributes["colspan"]?.Value, out var colspan) ? colspan : 1);

                            var firstLine = true;
                            var tableLenght = 150;
                            var cellSize = (tableLenght - cellNumber - 1) / cellNumber;
                            tableLenght = cellSize * cellNumber + cellNumber + 1; //Adjust table size

                            result.AppendLine("<font=\"Courier New SDF\"><size=60%>");
                            result.AppendLine($"┌{new string('─', tableLenght - 2)}┐");
                            foreach (var child in item.ChildNodes)
                            {
                                var childName = NodeName(child);
                                bool thead = childName == "thead";
                                foreach (var row in new[] { "thead", "tbody", "tfoot" }.Contains(childName) ? child.ChildNodes : item.ChildNodes)
                                {
                                    var cells = row.ChildNodes.Select(cell =>
                                    {
                                        int.TryParse(cell.Attributes["colspan"]?.Value, out var colspan);
                                        var _cellSize = cellSize * (colspan > 1 ? colspan : 1);

                                        var innerText = System.Net.WebUtility.HtmlDecode(cell.InnerText);

                                        var startWhite = whiteSpaces((_cellSize - innerText.Length) / 2);
                                        var text = AnalyseNode(cell).Replace("\n", "");
                                        var endWhite = whiteSpaces(_cellSize - startWhite.Length - innerText.Length);

                                        return $"{startWhite}{(thead ? "<b>" : "")}{text}{(thead ? "</b>" : "")}{endWhite}";

                                        string whiteSpaces(float size) => new string(' ', size < 0 ? 0 : UnityEngine.Mathf.FloorToInt(size));
                                    });
                                    var line = $"│{string.Join("│", cells)}│";
                                    if (!firstLine) result.AppendLine($"├{new string('─', tableLenght - 2)}┤");
                                    result.AppendLine(line);
                                    firstLine = false;
                                }
                            }
                            result.AppendLine($"└{new string('─', tableLenght - 2)}┘");
                            result.Append("</font><size=100%>");
                        }
                        catch (System.Exception e)
                        {
                            Logging.Log("Experimental display of tables failed with the following error\n\n" + e + "\n\n At: " + item.OuterHtml, UnityEngine.LogType.Error);
                            result.Append(AnalyseNode(item));
                        }
                    }
                    else if (itemName.StartsWith("o:")) result.Append(AnalyseNode(item)); //MS Office related tag
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
    }
}
