using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overwhelming Kitchen order card UI.
/// Displays individual order information including dish name and required ingredients.
/// </summary>
public class OverwhelmingKitchenOrderCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI mealNameText;
    [SerializeField] private TextMeshProUGUI customerNameText;
    [SerializeField] private Transform ingredientListContainer;
    [SerializeField] private GameObject ingredientTextPrefab;
    [SerializeField] private Image backgroundImage;

    [Header("Animation Settings")]
    [SerializeField] private float completionAnimDuration = 0.5f;
    [SerializeField] private AnimationCurve completionScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private Color completionColor = Color.green;

    private Order currentOrder;
    private Color originalBackgroundColor;

    private void Awake()
    {
        if (backgroundImage != null)
        {
            originalBackgroundColor = backgroundImage.color;
        }
    }

    /// <summary>
    /// Set order data
    /// </summary>
    public void SetOrder(Order order)
    {
        currentOrder = order;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (currentOrder == null) return;

        // Set meal name
        if (mealNameText != null)
        {
            mealNameText.text = currentOrder.MealName;
        }

        // Set customer name
        if (customerNameText != null)
        {
            customerNameText.text = currentOrder.CustomerName;
        }

        // Display ingredients
        DisplayIngredients();
    }

    private void DisplayIngredients()
    {
        if (ingredientListContainer == null)
            return;

        // Clear existing ingredient displays
        foreach (Transform child in ingredientListContainer)
        {
            Destroy(child.gameObject);
        }

        // Add ingredient texts
        if (currentOrder.Recipe != null && currentOrder.Recipe.ingredients != null)
        {
            foreach (var ingredient in currentOrder.Recipe.ingredients)
            {
                CreateIngredientText(ingredient);
            }
        }
    }

    private void CreateIngredientText(string ingredientName)
    {
        GameObject textObject;

        if (ingredientTextPrefab != null)
        {
            textObject = Instantiate(ingredientTextPrefab, ingredientListContainer);
        }
        else
        {
            // Fallback: create simple text object
            textObject = new GameObject("Ingredient");
            textObject.transform.SetParent(ingredientListContainer);
            textObject.AddComponent<TextMeshProUGUI>();
        }

        var text = textObject.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"• {ingredientName}";
        }
    }

    /// <summary>
    /// Play completion animation
    /// </summary>
    public void PlayCompleteAnimation()
    {
        StartCoroutine(CompleteAnimationCoroutine());
    }

    private IEnumerator CompleteAnimationCoroutine()
    {
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        Color originalColor = backgroundImage != null ? backgroundImage.color : Color.white;

        while (elapsed < completionAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / completionAnimDuration;

            // Scale animation
            float scaleMultiplier = completionScaleCurve.Evaluate(t);
            transform.localScale = originalScale * scaleMultiplier;

            // Color animation
            if (backgroundImage != null)
            {
                backgroundImage.color = Color.Lerp(originalColor, completionColor, t);
            }

            yield return null;
        }

        // Animation complete, destroy self
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("Test Complete Animation")]
    private void TestCompleteAnimation()
    {
        if (Application.isPlaying)
        {
            PlayCompleteAnimation();
        }
    }
#endif
}
