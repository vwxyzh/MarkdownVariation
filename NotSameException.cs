using Newtonsoft.Json.Linq;
using System;

namespace MarkdownVariation
{
    public class NotSameException : Exception
    {
        public NotSameException(string message, JToken oldToken, JToken newToken)
            : base(message)
        {
            OldToken = oldToken;
            NewToken = newToken;
        }
        public JToken OldToken { get; }
        public JToken NewToken { get; }
    }
}
