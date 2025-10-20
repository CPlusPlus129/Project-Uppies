using System.Collections.Generic;
using UnityEngine;

namespace DialogueModule
{
    [System.Serializable]
    public class StringGridDictionary
    {
        [SerializeField]
        private List<string> keys = new List<string>();

        [SerializeField]
        private List<StringGrid> values = new List<StringGrid>();

        public Dictionary<string, StringGrid> ToDictionary()
        {
            var dict = new Dictionary<string, StringGrid>();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }

        public void Add(string key, StringGrid grid)
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
            {
                values[index] = grid;
            }
            else
            {
                keys.Add(key);
                values.Add(grid);
            }
        }

        public bool TryGetValue(string key, out StringGrid grid)
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
            {
                grid = values[index];
                return true;
            }
            grid = null;
            return false;
        }

        public IEnumerable<string> Keys => keys;
        public IReadOnlyList<StringGrid> Values => values;
        public int Count => keys.Count;
    }
}
