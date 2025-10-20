namespace DialogueModule
{
    class CommandSelectionEnd : CommandBase
    {
        public CommandSelectionEnd() : base(CommandID.SelectionEnd, null)
        {
        }

        public override void Execute(DialogueEngine engine)
        {
            engine.adapter.ShowSelections();
            isWaiting = true;
        }
    }
}