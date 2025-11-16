using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class RecipeSelectionPanel : MonoBehaviour, IUIInitializable
{
    [SerializeField] private RecipeInfoBlock infoBlock;
    [SerializeField] private ToggleGroup recipeSelectionToggleGroup;
    [SerializeField] private RecipeItem recipeItemPrefab;
    [SerializeField] private Button cookButton;
    [SerializeField] private UIAnimationController uiAnim;
    public Subject<Unit> OnOpenComplete { get; } = new Subject<Unit>();
    public Subject<Unit> OnCloseComplete { get; } = new Subject<Unit>();
    private IAssetLoader assetLoader;
    private ICookingSystem cookingSystem;
    private ObjectPool<RecipeItem> recipeItemPool;
    private List<RecipeItem> recipeItemList = new List<RecipeItem>();

    public async UniTask Init()
    {
        recipeItemPrefab.gameObject.SetActive(false);
        uiAnim.OnOpenComplete.Subscribe(OnOpenComplete.OnNext).AddTo(this);
        uiAnim.OnCloseComplete.Subscribe(OnCloseComplete.OnNext).AddTo(this);
        assetLoader = await ServiceLocator.Instance.GetAsync<IAssetLoader>();
        cookingSystem = await ServiceLocator.Instance.GetAsync<ICookingSystem>();
        recipeItemPool = new ObjectPool<RecipeItem>(() =>
        {
            var item = Instantiate(recipeItemPrefab, recipeItemPrefab.transform.parent);
            item.cookingSystem = cookingSystem;
            item.tgl.group = recipeSelectionToggleGroup;
            return item;
        });
        await infoBlock.Init(assetLoader, cookingSystem);
        cookingSystem.currentSelectedRecipe
            .Subscribe(_ => UpdateCookButton())
            .AddTo(this);
        cookButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                cookingSystem.Cook();
                UpdateUI();
            }).AddTo(this);
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        UpdateRecipeList();
        UpdateCookButton();
    }

    private void UpdateRecipeList()
    { 
        ClearAllItems();
        foreach (var recipe in Database.Instance.recipeData.datas)
        {
            var item = recipeItemPool.Get();
            item.cookingSystem = cookingSystem;
            item.gameObject.SetActive(true);
            item.Setup(recipe);
            var canCook = cookingSystem.CheckPlayerHasIngredients(recipe);
            item.tgl.interactable = canCook;
            recipeItemList.Add(item);
        }
    }

    private void UpdateCookButton()
    {
        var currentRecipeName = cookingSystem.currentSelectedRecipe.Value;
        if (string.IsNullOrEmpty(currentRecipeName))
        {
            cookButton.interactable = false;
            return;
        }
        var recipe = Database.Instance.recipeData.GetRecipeByName(currentRecipeName);
        cookButton.interactable = cookingSystem.CheckPlayerHasIngredients(recipe);
    }

    private void ClearAllItems()
    {
        foreach (var item in recipeItemList)
        {
            item.ResetUI();
            recipeItemPool.Release(item);
            item.gameObject.SetActive(false);
        }
        recipeItemList.Clear();
    }

    public void Open()
    {
        uiAnim.Open();
    }

    public void Close()
    {
        uiAnim.Close();
    }
}
