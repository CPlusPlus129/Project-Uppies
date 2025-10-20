namespace DialogueModule
{
    class CharacterNameHandler : ITagHandler
    {
        public string Process(DataManager dataManager, string value)
        {
            if (dataManager.settingDataManager.characterSettings.DataDict.TryGetValue(value, out var characterSettings))
                return characterSettings.displayName;
            return value;
        }
    }
}