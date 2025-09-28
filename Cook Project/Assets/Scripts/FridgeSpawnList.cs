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
        var spArr = RandomHelper.PickWithoutReplacement(spawnPoints, SpawnCount);
        var eArr = RandomHelper.PickWithoutReplacement(essentialIngredients, essentialIngredients.Length);
        var essentailIndex = 0;
        foreach (var spawnPoint in spArr)
        {
            var f = spawnPoint.Spawn(fridgePrefab);
            if (essentailIndex < eArr.Length)
            {
                f.SetItemName(eArr[essentailIndex]);
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