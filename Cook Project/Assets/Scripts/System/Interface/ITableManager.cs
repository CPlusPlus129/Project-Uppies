using Cysharp.Threading.Tasks;

public interface ITableManager : IGameService
{
    UniTask LoadAllTables();
    void Register<T>(T service, string[] csvPaths) where T : ITable;
    Table<TRow> GetTable<TRow>() where TRow : class, new();
}