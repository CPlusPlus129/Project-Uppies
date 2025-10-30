public interface IFridgeGlowManager : IGameService
{
    void RefreshGlowStates();
    void RegisterFoodSource(FoodSource foodSource);
    void UnregisterFoodSource(FoodSource foodSource);
}
