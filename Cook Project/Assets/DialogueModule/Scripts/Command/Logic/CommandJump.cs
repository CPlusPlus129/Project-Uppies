using UnityEngine;

namespace DialogueModule
{
    class CommandJump : CommandBase
    {
        string jumpLabel;
        public CommandJump(GridInfo grid, StringGridRow row) : base(CommandID.Jump, row)
        {
            if(!DataParser.TryParseScenarioLabel(grid, row, ColumnName.Arg1, out jumpLabel))
                Debug.LogError($"Failed to parse scenario label from sheet name {grid.gridName}, Arg1 column, row {row}");
        }

        public override void Execute(DialogueEngine engine)
        {
            var labelData = engine.dataManager.GetLabelData(jumpLabel);
            if(labelData == null)
            {
                Debug.LogError($"Failed to find jump label name: {jumpLabel}!");
                return;
            }
            engine.scenarioManager.SetNextLabel(labelData);
        }
    }
}