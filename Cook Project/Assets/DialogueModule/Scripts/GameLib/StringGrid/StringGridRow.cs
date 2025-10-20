using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

namespace DialogueModule
{
    [System.Serializable]
    public class StringGridRow
    {
        [SerializeField]
        private string[] strings;

        public bool IsEmpty { get; private set; }
        public bool IsCommentOut { get; private set; }
        public int Length => strings?.Length ?? 0;

        public StringGridRow(List<string> cells)
        {
            strings = cells.ToArray();
            IsEmpty = CheckIsEmpty();
            IsCommentOut = CheckIsCommentOut();
        }

        public void Init(string csvText)
        {
            strings = csvText.Split(new char[] { ',' });
            IsEmpty = CheckIsEmpty();
            IsCommentOut = CheckIsCommentOut();
        }

        public string GetCell(int index)
        {
            if (index < 0 || index >= strings.Length)
                return string.Empty;
            return strings[index];
        }

        bool CheckIsEmpty()
        {
            return strings == null || !strings.Any(s => !string.IsNullOrEmpty(s));
        }

        bool CheckIsCommentOut()
        {
            return strings != null && strings.Length > 0 && strings[0].StartsWith("//");
        }

        public bool CheckIsScenarioHeaderRow()
        {
            var str = ToString();
            return str.Contains("Command") && str.Contains("Arg1") && str.Contains("Text");
        }

        public override string ToString()
        {
            return string.Join(",", strings);
        }
    }
}