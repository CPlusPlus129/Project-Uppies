using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    [CreateAssetMenu(fileName = "SettingsBook", menuName = "Dialogue/Settings Book")]
    public class SettingsBook : ScriptableObject
    {
        //doing this for unity serialization
        [SerializeField]
        private List<string> keys = new List<string>();
        [SerializeField]
        private List<StringGridDictionary> settings = new List<StringGridDictionary>();

        public void SetSettings(string key, StringGridDictionary data)
        {
            var index = keys.IndexOf(key);
            if (index >= 0)
            {
                settings[index] = data;
            }
            else
            {
                keys.Add(key);
                settings.Add(data);
            }
        }

        public bool TryGetSettings(string key, out StringGridDictionary data)
        {
            var index = keys.IndexOf(key);
            if (index >= 0)
                data = settings[index];
            else
                data = null;

            return data != null;
        }

        public void ClearSettings()
        {
            settings.Clear();
        }

        public int GetSettingsCount(string key)
        {
            if (TryGetSettings(key, out StringGridDictionary data))
            {
                return data.Count;
            }
            return 0;
        }

        public int TotalSettingsTypes => settings.Count;
    }
}
