using R3;

public interface ICookingSystem : IGameService
{
    ReactiveProperty<string> currentSelectedRecipe { get; }
    void Cook();
    void CompleteCooking();
    bool CheckPlayerHasIngredients(Recipe recipe);
}