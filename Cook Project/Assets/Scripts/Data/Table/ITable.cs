using Cysharp.Threading.Tasks;

public interface ITable
{
    void Clear();
    UniTask LoadFromCsvFile(string csvAssetPath);
}
