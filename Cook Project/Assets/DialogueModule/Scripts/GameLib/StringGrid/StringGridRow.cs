using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueModule
{
    [System.Serializable]
    public class StringGridRow
    {
        [SerializeField]
        private string[] strings;

        private bool? _IsEmpty = null;
        private bool? _IsCommentOut = null;
        public bool IsEmpty => IsEmptyGetter();
        public bool IsCommentOut => IsCommentOutGetter();
        public int Length => strings?.Length ?? 0;

        public StringGridRow(List<string> cells)
        {
            strings = cells.ToArray();
            //initialize isEmpty and isCommentOut
            IsEmptyGetter();
            IsCommentOutGetter();
        }

        public void Init(string csvText)
        {
            strings = csvText.Split(new char[] { ',' });
            //initialize isEmpty and isCommentOut
            IsEmptyGetter();
            IsCommentOutGetter();
        }

        public string GetCell(int index)
        {
            if (index < 0 || index >= strings.Length)
                return string.Empty;
            return strings[index];
        }

        bool IsEmptyGetter()
        {
            _IsEmpty ??= CheckIsEmpty();
            return _IsEmpty.Value;
        }

        bool IsCommentOutGetter()
        {
            _IsCommentOut ??= CheckIsCommentOut();
            return _IsCommentOut.Value;
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