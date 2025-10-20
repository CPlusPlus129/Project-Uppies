using UnityEngine;

namespace DialogueModule
{
    class CommandCharacter : CommandBase
    {
        public string characterId;
        public bool isHiding;
        public string layerName;
        public string textContent;
        public CharacterSettingData characterSettingData;

        public CommandCharacter(GridInfo grid, StringGridRow row) : base(CommandID.Character, row)
        {
            characterId = DataParser.GetCell(grid, row, ColumnName.Arg1);
            isHiding = DataParser.GetCell(grid, row, ColumnName.Arg2) == "Off";
            layerName = DataParser.GetCell(grid, row, ColumnName.Arg3);
            textContent = DataParser.GetCell(grid, row, ColumnName.Text);
        }

        public override void Execute(DialogueEngine engine)
        {
            if (!engine.dataManager.settingDataManager.characterSettings.DataDict.TryGetValue(characterId, out characterSettingData))
            {
                Debug.LogError($"Could not find character setting for character ID: {characterId}");
                return;
            }

            if (isHiding)
            {
                engine.adapter.characterAdapter.HideLayer(layerName);
            }
            else
            {
                Sprite sprite = null;
                if (!string.IsNullOrEmpty(characterSettingData.fileName))
                {
                    if (engine.assetManager.CurrentUsingAssetDict.TryGetValue(characterSettingData.fileName, out var obj))
                        sprite = obj as Sprite;
                    else
                        Debug.LogError($"Cannot find character sprite in assetmanager! fileName: {characterSettingData.fileName}, characterID {characterId}");
                }
                engine.adapter.characterAdapter.ShowCharacter(layerName, characterSettingData, sprite);
            }

            if (!string.IsNullOrEmpty(textContent))
            {
                var parsedText = engine.dataManager.ParseDialogueText(textContent);
                engine.adapter.PlayText(characterSettingData.displayName, parsedText);
                isWaiting = true;
            }
        }
    }
}