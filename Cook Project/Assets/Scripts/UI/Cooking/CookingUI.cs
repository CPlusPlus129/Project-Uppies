using UnityEngine;

public class CookingUI : MonoBehaviour
{
    public RecipeSelectionPanel selectionPanel;

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}