namespace DialogueModule
{
    public interface IScenarioFileReader
    {
        bool TryReadFile(string path, out StringGridDictionary stringGridDictionary);
    }
}