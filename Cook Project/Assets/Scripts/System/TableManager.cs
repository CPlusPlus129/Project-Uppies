using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class TableManager : ITableManager
{
    private Dictionary<Type, ITable> tables = new Dictionary<Type, ITable>();
    private Dictionary<Type, string[]> tableCsvPaths = new Dictionary<Type, string[]>();

    public async UniTask Init()
    {
        Register(new Table<QuestRow>(), new[] { "Res:/Tables/Quest.csv" });
        //Register(new Table<DialogueNodeRow>(), new[] { "Res:/Tables/DialogueNodes.csv", "Res:/Tables/DialogueNodes2.csv" });

        await UniTask.CompletedTask;
    }

    public async UniTask LoadAllTables()
    {
        foreach (var kvp in tables)
        {
            var table = kvp.Value;
            var csvPaths = tableCsvPaths[kvp.Key];
            table.Clear();
            foreach (var csvPath in csvPaths)
            {
                await table.LoadFromCsvFile(csvPath);
            }
        }
    }

    public void Register<T>(T service, string[] csvPaths) where T : ITable
    {
        tables[typeof(T)] = service;
        tableCsvPaths[typeof(T)] = csvPaths;
    }

    public Table<TRow> GetTable<TRow>() where TRow : class, new()
    {
        return (Table<TRow>)tables[typeof(Table<TRow>)];
    }

    public void Dispose()
    {
        // Clear all tables
        foreach (var table in tables.Values)
        {
            table?.Clear();
        }
        tables.Clear();
        tableCsvPaths.Clear();
    }
}