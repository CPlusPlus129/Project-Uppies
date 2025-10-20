using UnityEngine;

namespace DialogueModule
{
    [CreateAssetMenu(fileName = "ScenarioBook", menuName = "Dialogue/Scenario Book")]
    public class ScenarioBook : ScriptableObject
    {
        [SerializeField]
        private StringGridDictionary scenarioData = new StringGridDictionary();

        public StringGridDictionary ScenarioData => scenarioData;

        public void SetScenarioData(StringGridDictionary data)
        {
            scenarioData = data;
        }

        public bool TryGetScenario(string scenarioName, out StringGrid grid)
        {
            return scenarioData.TryGetValue(scenarioName, out grid);
        }

        public int ScenarioCount => scenarioData.Count;
    }
}
