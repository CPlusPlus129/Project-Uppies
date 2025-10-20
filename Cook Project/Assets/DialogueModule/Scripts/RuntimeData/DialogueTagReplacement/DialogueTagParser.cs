using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DialogueModule
{
    class DialogueTagParser
    {
        private static readonly Regex tagRegex = new Regex(@"<(\w+)=([^>]+)>", RegexOptions.Compiled);
        private readonly Dictionary<string, ITagHandler> handlers = new();

        public void Init()
        {
            RegisterHandler("char", new CharacterNameHandler());
        }

        public void RegisterHandler(string tag, ITagHandler handler)
        {
            handlers[tag] = handler;
        }

        public string Parse(DataManager dataManager, string input)
        {
            return tagRegex.Replace(input, match =>
            {
                string tag = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                if (handlers.TryGetValue(tag, out var handler))
                {
                    return handler.Process(dataManager, value);
                }
                else
                {
                    // 未註冊的標籤就原樣保留
                    return match.Value;
                }
            });
        }
    }
}