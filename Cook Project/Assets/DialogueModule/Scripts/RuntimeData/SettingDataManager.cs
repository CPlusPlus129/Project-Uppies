namespace DialogueModule
{
    class SettingDataManager
    {
        public CharacterSettings characterSettings = new CharacterSettings();
        public LayerSettings layerSettings = new LayerSettings();

        public void Init(SettingsBook settingsBook)
        {
            StringGridDictionary gridDict = null;
            if (!settingsBook.TryGetSettings("Character", out gridDict))
                throw new System.Exception("Character sheet is not in settings book!");
            characterSettings.Init(gridDict);
            if(!settingsBook.TryGetSettings("Layer", out gridDict))
                throw new System.Exception("Layer sheet is not in settings book!");
            layerSettings.Init(gridDict);
        }
    }
}