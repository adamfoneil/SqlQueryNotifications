using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SqlQueryNotifications.Internal
{
    /// <summary>
    /// Stack-based HTML tag writer that manages indents and encodes attributes nicely
    /// </summary>
    internal class HtmlBuilder
    {
        private readonly StringBuilder _sb;
        private readonly Stack<string> _tagNames;

        private string GetIndent()
        {
            return (_tagNames.Count > 0) ?
                new string('\t', _tagNames.Count - 1) :
                string.Empty;
        }

        public HtmlBuilder()
        {
            _sb = new StringBuilder();
            _tagNames = new Stack<string>();
        }

        private string GetAttributes(object attributes)
        {
            return (attributes != null) ?
                " " + GetAttributeString(attributes) :
                null;
        }

        public void StartTag(string name, object attributes = null)
        {
            var attrString = GetAttributes(attributes);
            _tagNames.Push(name);
            _sb.AppendLine(GetIndent() + $"<{name}{attrString}>");
        }

        public void WriteContent(object content)
        {
            _sb.AppendLine(GetIndent() + content?.ToString());
        }

        public void WriteTag(string name, object content, object attributes = null)
        {
            var attrString = GetAttributes(attributes);
            WriteTagInner(name, content, attrString);
        }

        public void WriteTag(string name, object attributes)
        {
            var attrString = " " + GetAttributeString(attributes);
            WriteTagInner(name, attrString);
        }

        public void WriteTag(string name, Dictionary<string, string> attributes)
        {
            var attrString = " " + GetAttributeString(attributes);
            WriteTagInner(name, attrString);
        }

        public void WriteTag(string name, object content, Dictionary<string, string> attributes)
        {
            var attrString = " " + GetAttributeString(attributes);
            WriteTagInner(name, content, attrString);
        }

        private void WriteTagInner(string name, string attrString)
        {
            _sb.AppendLine(GetIndent() + $"\t<{name}{attrString}/>");
        }

        private void WriteTagInner(string name, object content, string attrString)
        {
            _sb.AppendLine(GetIndent() + $"\t<{name}{attrString}>{HttpUtility.HtmlEncode(content)}</{name}>");
        }

        public void WriteHtmlTag(string name, string innerHtml, object attributes = null)
        {
            var attrString = GetAttributes(attributes);
            _sb.AppendLine(GetIndent() + $"\t<{name}{attrString}>{innerHtml}</{name}>");
        }

        public void EndTag()
        {
            _sb.AppendLine(GetIndent() + $"</{_tagNames.Pop()}>");
        }

        public void Append(object content)
        {
            _sb.Append(content);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }

        private static string GetAttributeString(object @object)
        {
            var props = @object.GetType().GetProperties();
            var dictionary = props
                .Where(pi => pi.GetValue(@object) != null)
                .ToDictionary(pi => pi.Name, pi => pi.GetValue(@object).ToString());

            return GetAttributeString(dictionary);
        }

        private static string GetAttributeString(Dictionary<string, string> data) => string.Join(" ", data.Select(kp => $"{kp.Key}=\"{kp.Value}\""));
    }
}
