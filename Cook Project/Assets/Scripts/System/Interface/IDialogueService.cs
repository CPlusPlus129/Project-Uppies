using Cysharp.Threading.Tasks;
using R3;

public interface IDialogueService : IGameService
{
    Subject<Unit> onBeginScenario { get; }
    Subject<Unit> onEndScenario { get; }
    UniTask StartDialogueAsync(string label);
    void StartDialogue(string label);
}