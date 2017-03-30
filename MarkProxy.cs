using Microsoft.DocAsCode.Build.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DocAsCode.Plugins;
using Newtonsoft.Json.Linq;

namespace MarkdownVariation
{
    public sealed class MarkProxy : MarshalByRefObject
    {
        private static readonly IMarkdownService _markdown;

        static MarkProxy()
        {
            var provider = new DfmJsonTokenTreeServiceProvider();
            _markdown = provider.CreateMarkdownService(new MarkdownServiceParameters { });
        }

        public string Parse(string markdown)
        {
            try
            {
                return _markdown.Markup(markdown, "c:\\").Html;
            }
            catch (Exception ex)
            {
                var result = new JObject();
                result["error"] = ex.Message;
                return result.ToString();
            }
        }
    }
}
