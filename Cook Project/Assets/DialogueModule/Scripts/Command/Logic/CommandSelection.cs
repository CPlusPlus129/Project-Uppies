using UnityEngine;

namespace DialogueModule
{
    class CommandSelection : CommandBase
    {
        string jumpLabel;
        string textContent;

        public CommandSelection(GridInfo grid, StringGridRow row) : base(CommandID.Selection, row)
        {
            if(!DataParser.TryParseScenarioLabel(grid, row, ColumnName.Arg1, out jumpLabel))
                Debug.LogError($"Failed to parse scenario label from sheet name {grid.gridName}, Arg1 column, row {row}");
            textContent = DataParser.GetCell(grid, row, ColumnName.Text);
        }

        public override void Execute(DialogueEngine engine)
        {
            engine.adapter.AddSelection(jumpLabel, textContent);
        }
    }
}