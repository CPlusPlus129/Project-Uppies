using R3;

public interface ICookingSystem : IGameService
{
	ReactiveProperty<string> currentSelectedRecipe { get; }
	void Cook();
	void CompleteCooking(MinigamePerformance performance);
	bool CheckPlayerHasIngredients(Recipe recipe);
}
