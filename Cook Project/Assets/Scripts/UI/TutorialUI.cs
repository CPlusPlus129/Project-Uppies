using Cysharp.Threading.Tasks;
using UnityEngine;

public class TutorialUI : MonoBehaviour, IUIInitializable
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMPro.TextMeshProUGUI tutorialText;

    public async UniTask Init()
    {
        HideTutorial();
        await UniTask.CompletedTask;
    }

    public void ShowTutorial(string message)
    {
        tutorialText.text = message;
        tutorialPanel.SetActive(true);
    }

    public void HideTutorial()
    {
        tutorialPanel.SetActive(false);
    }
}