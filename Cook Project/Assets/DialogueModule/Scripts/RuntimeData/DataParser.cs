using UnityEngine;

namespace DialogueModule
{
    public static class DataParser
    {
        public static CommandBase ParseCommand(GridInfo grid, StringGridRow row)
        {
            if (row == null || row.IsEmpty || row.IsCommentOut)
                return null;

            var commandID = GetCell(grid, row, ColumnName.Command);
            var parsedCommandID = ParseCommandID(commandID, grid, row);
            if(parsedCommandID == null && !row.IsEmpty)
            {
                Debug.LogError($"Failed to parse command from sheet {grid.gridName}, row {row}");
                return null;
            }
            var command = CommandFactory.Create(parsedCommandID, grid, row);
            return command;
        }

        private static string ParseCommandID(string id, GridInfo grid, StringGridRow row)
        {
            if (string.IsNullOrEmpty(id))
            {
                var arg1 = GetCell(grid, row, ColumnName.Arg1);
                if (!string.IsNullOrEmpty(arg1))
                    return CommandID.Character.ToStringFast();
                var text = GetCell(grid, row, ColumnName.Text);
                if(!string.IsNullOrEmpty(text))
                    return CommandID.Text.ToStringFast();

                return null;
            }
            else if(IsScenarioLabel(id))
                return CommandID.ScenarioLabel.ToStringFast();

            return id;
        }

        public static bool TryParseScenarioLabel(GridInfo grid, StringGridRow row, ColumnName columnName, out string label)
        {
            var cellValue = row.GetCell(grid.GetColumnIndex(columnName));
            if (!IsScenarioLabel(cellValue))
            {
                label = cellValue;
                return false;
            }
            else
            {
                if (cellValue.Length >= 3 && cellValue[1] == '*')
                {
                    label = grid.gridName + '*' + cellValue.Substring(2);
                }
                else
                {
                    label = cellValue.Substring(1);
                }
                return true;
            }
        }

        private static bool IsScenarioLabel(string str)
        {
            return (!string.IsNullOrEmpty(str) && str.Length >= 2 && (str[0] == '*'));
        }

        public static string GetCell(GridInfo grid, StringGridRow row, ColumnName columnName)
        {
            return row.GetCell(grid.GetColumnIndex(columnName));
        }

    }
}