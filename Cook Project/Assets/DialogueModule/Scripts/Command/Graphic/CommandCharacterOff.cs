namespace DialogueModule
{
    class CommandCharacterOff : CommandBase
    {
        string targetName;
        float fadeTime;

        public CommandCharacterOff(GridInfo grid, StringGridRow row) : base(CommandID.CharacterOff, row)
        {
            targetName = row.GetCell(grid.GetColumnIndex(ColumnName.Arg1));
            if (!float.TryParse(row.GetCell(grid.GetColumnIndex(ColumnName.Arg2)), out fadeTime))
                fadeTime = 0.2f;
        }

        public override void Execute(DialogueEngine engine)
        {
            if (string.IsNullOrEmpty(targetName))
            {
                engine.adapter.characterAdapter.HideAll();
            }
            else
            {
                var layerData = GetLayerDataByEitherCharacterIdOrLayerName(engine);
                if (layerData != null)
                {
                    engine.adapter.characterAdapter.HideLayer(layerData.LayerName);
                }
            }
        }

        private CharacterLayerData GetLayerDataByEitherCharacterIdOrLayerName(DialogueEngine engine)
        {
            var characterAdapter = engine.adapter.characterAdapter;
            return characterAdapter.GetLayerByCharacterId(targetName) ?? characterAdapter.GetLayer(targetName);
        }

    }
}