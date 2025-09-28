using R3;

public class FridgeSpawnList : SpawnPointList
{
    public int SpawnCount = 7;
    public FoodSource fridgePrefab;
    public string[] essentialIngredients;

    protected override void Awake()
    {
        base.Awake();
        ShiftSystem.Instance.OnGameStart.Subscribe(_ => SpawnFridges()).AddTo(this);
    }

    private void SpawnFridges()
    {
        Reset();
        var arr = RandomHelper.PickWithoutReplacement(spawnPoints, SpawnCount);
        var essentailIndex = 0;
        foreach (var spawnPoint in arr)
        {
            var f = spawnPoint.Spawn(fridgePrefab);
            if (essentailIndex < essentialIngredients.Length)
            {
                f.SetItemName(essentialIngredients[essentailIndex]);
                essentailIndex++;
            }
            else
            {
                // Pick a random ingredient from essential. This is subject to change later.
                var randomIngredient = RandomHelper.PickOne(essentialIngredients);
                f.SetItemName(randomIngredient);
            }
        }
    }

    private void Reset()
    {
        foreach (var spawnPoint in spawnPoints)
        {
            spawnPoint.Reset();
        }
    }

}