using System;
using System.Collections.Generic;

public interface IFridgeGlowManager : IGameService
{
    event Action<IReadOnlyCollection<FoodSource>> EligibleFridgesChanged;

    int EligibilityVersion { get; }
    IReadOnlyCollection<FoodSource> GetEligibleFridgesSnapshot();
    void RefreshGlowStates();
    void RegisterFoodSource(FoodSource foodSource);
    void UnregisterFoodSource(FoodSource foodSource);
    void SetInventoryCheckInterval(float seconds);
}
