using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    public class GridInfo
    {
        public readonly string gridName;
        private readonly Dictionary<ColumnName, int> headerIndexMap = new();

        public GridInfo(StringGrid grid)
        {
            this.gridName = grid.Name;
            var headerRow = grid.GetHeaderRow();
            headerIndexMap.Clear();
            for (int i = 0; i < headerRow.Length; i++)
            {
                var value = headerRow.GetCell(i);
                if (string.IsNullOrEmpty(value))
                    continue;

                if (Enum.TryParse<ColumnName>(value, out var enumValue))
                    headerIndexMap.Add(enumValue, i);
                else
                    Debug.LogError($"Unknown ColumnName enum value {value}!");
            }
        }

        public int GetColumnIndex(ColumnName columnName)
        {
            if (headerIndexMap.TryGetValue(columnName, out var index))
                return index;
            return -1;
        }
    }
}