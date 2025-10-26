using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class TutorialUI : MonoBehaviour, IUIInitializable
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMPro.TextMeshProUGUI tutorialText;

    public async UniTask Init()
    {
        HideTutorial();
        WorldBroadcastSystem.Instance.onTutorialHint.Subscribe(tuple =>
        {
            string message = tuple.message;
            bool isOn = tuple.isOn;
            if (isOn)
            {
                ShowTutorial(message);
            }
            else
            {
                HideTutorial();
            }
        }).AddTo(this);
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