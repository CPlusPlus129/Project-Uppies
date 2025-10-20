using UnityEngine;

namespace DialogueModule
{
    class CommandScenarioLabel : CommandBase
    {
        public readonly string labelName;
        public CommandScenarioLabel(GridInfo grid, StringGridRow row) : base(CommandID.ScenarioLabel, row)
        {
            if (!DataParser.TryParseScenarioLabel(grid, row, ColumnName.Command, out labelName))
                Debug.LogError($"Failed to parse scenario label from sheet name {grid.gridName}, Command column, row {row}");
        }

        public override void Execute(DialogueEngine engine)
        {

        }
    }
}