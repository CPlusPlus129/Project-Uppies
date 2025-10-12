using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table<TRow> : ITable where TRow : new()
{
    private readonly List<TRow> dataList = new List<TRow>();
    public IReadOnlyList<TRow> DataList => dataList;

    public void Clear()
    {
        dataList.Clear();
    }

    public async UniTask LoadFromCsvFile(string csvAssetPath)
    {
        var assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        var csvText = await assetLoader.LoadAsync<TextAsset>(csvAssetPath);
        var list = CsvReader.ReadFromString<TRow>(csvText.text);
        dataList.AddRange(list);
    }

    public TRow GetRow(Func<TRow, bool> predicate)
    {
        return dataList.FirstOrDefault(predicate);
    }

    public List<TRow> GetRows(Func<TRow, bool> predicate)
    {
        return dataList.Where(predicate).ToList();
    }

    public List<TRow> GetAllRows()
    {
        return new List<TRow>(dataList);
    }
}
