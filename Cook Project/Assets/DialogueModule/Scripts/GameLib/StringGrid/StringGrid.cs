using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueModule
{
    [System.Serializable]
    public class StringGrid
    {
        public string Name { get; set; }
        public List<StringGridRow> Rows => rows;
        public int RowCount => rows.Count;

        [SerializeField]
        private List<StringGridRow> rows = new List<StringGridRow>();

        public StringGrid(string name = "")
        {
            Name = name;
        }

        public void AddRow(StringGridRow row)
        {
            rows.Add(row);
        }

        public StringGridRow GetRow(int index)
        {
            if (index < 0 || index >= rows.Count)
                return null;
            return rows[index];
        }

        public void Clear()
        {
            rows.Clear();
        }

        public int GetCoulumnIndex(string columnName)
        {
            var header = GetHeaderRow();
            for (int i = 0; i < header.Length; i++)
            {
                var cell = header.GetCell(i);
                if (cell == columnName)
                    return i;
            }
            return -1;
        }

        public StringGridRow GetHeaderRow()
        {
            var row = GetRow(0);
            return row;
        }

        public int GetFirstDataRowIndex()
        {
            if (rows.Count < 2)
                return -1;
            var index = rows.FindIndex(1, x => !x.IsEmpty && !x.IsCommentOut);
            return index;
        }

        public GridInfo CreateGridInfo()
        {
            var info = new GridInfo(this);
            return info;
        }
    }
}