namespace DialogueModule
{
    class CommandText : CommandBase
    {
        string textContent;

        public CommandText(GridInfo grid, StringGridRow row) : base(CommandID.Text, row)
        {
            textContent = DataParser.GetCell(grid, row, ColumnName.Text);
        }

        public override void Execute(DialogueEngine engine)
        {
            engine.adapter.characterAdapter.HideLayer("");
            var parsedText = engine.dataManager.ParseDialogueText(textContent);
            engine.adapter.PlayText("", parsedText, null, 1f);
            isWaiting = true;
        }
    }
}
