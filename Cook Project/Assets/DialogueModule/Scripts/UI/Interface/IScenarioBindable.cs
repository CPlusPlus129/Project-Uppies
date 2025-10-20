namespace DialogueModule
{
    interface IScenarioBindable
    {
        void BindToScenario(ScenarioUIAdapter adapter);
        void UnbindFromScenario(ScenarioUIAdapter adapter);
    }
}