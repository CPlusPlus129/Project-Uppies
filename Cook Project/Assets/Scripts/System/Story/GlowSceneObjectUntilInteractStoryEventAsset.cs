using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GlowSceneObjectUntilInteract", menuName = "Game Flow/Environment Events/Glow Scene Object")]
public sealed class GlowSceneObjectUntilInteractStoryEventAsset : StoryEventAsset
{
    [Header("Target")]
    [SerializeField]
    private string targetObjectName;

    [SerializeField]
    private bool includeInactive = true;

    [SerializeField]
    private bool searchAllScenes = false;

    [Header("Glow Settings")]
    [SerializeField]
    private Color glowColor = Color.cyan;

    [SerializeField]
    private float glowIntensity = 2.5f;

    [SerializeField]
    private bool pulse = true;

    [SerializeField]
    private float pulseSpeed = 2f;

    [SerializeField]
    private bool addGlowComponentIfMissing = true;

    [SerializeField]
    [Tooltip("Use the FridgeGlowController so the glow renders through walls.")]
    private bool useFridgeGlow = false;

    [SerializeField]
    [Tooltip("Only used when Fridge Glow is enabled.")]
    private float fridgeGlowScale = 1.15f;

    [SerializeField]
    [Tooltip("Only used when Fridge Glow is enabled.")]
    private float fridgeGlowFresnelPower = 3f;

    [SerializeField]
    [Tooltip("Only used when Fridge Glow is enabled. Represents how much the intensity pulses around the base value.")]
    private float fridgeGlowPulseVariation = 0.35f;

    [Header("Interaction")]
    [SerializeField]
    private bool waitForInteraction = true;

    [SerializeField]
    private bool disableInteractableAfterUse = true;

    [SerializeField]
    private string signalOnInteract;

    [Header("Quest Hooks")]
    [SerializeField]
    [Tooltip("Automatically start this quest when the glow becomes active.")]
    private bool startQuestOnActivate = false;

    [SerializeField]
    [Tooltip("Automatically complete this quest when the glow interaction succeeds.")]
    private bool completeQuestOnInteract = false;

    [SerializeField]
    [Tooltip("Quest identifier used for auto start/complete.")]
    private string questId;

    [SerializeField]
    [Tooltip("Wait for the specified quest to complete before finishing this event (useful when the target already has its own interaction logic).")]
    private bool waitForQuestCompletion = false;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetObjectName))
        {
            return StoryEventResult.Failed("Target object name is not set.");
        }

        var targets = FindTargets(targetObjectName);
        if (targets.Count == 0)
        {
            return StoryEventResult.Failed($"No object named '{targetObjectName}' was found in the loaded scenes.");
        }

        var requiresQuestService = startQuestOnActivate || completeQuestOnInteract || waitForQuestCompletion;
        IQuestService questService = null;
        if (requiresQuestService)
        {
            questService = await ResolveQuestServiceAsync(context);
        }

        if (startQuestOnActivate)
        {
            TryStartQuest(questService);
        }

        var waitList = new List<UniTask>();
        var disableGlowActions = new List<Action>();
        IDisposable questCompletionSubscription = null;

        try
        {
            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                var interactable = target.GetComponent<UnityInteractable>() ?? target.AddComponent<UnityInteractable>();
                Action disableGlow;
                if (!TryEnableGlow(target, out disableGlow))
                {
                    continue;
                }

                if (disableGlow != null)
                {
                    disableGlowActions.Add(disableGlow);
                }

                if (waitForInteraction)
                {
                    var tcs = new UniTaskCompletionSource<bool>();
                    UnityAction handler = null;
                    handler = () =>
                    {
                        disableGlow?.Invoke();
                        disableGlowActions.Remove(disableGlow);
                        interactable.RemoveOnInteractListener(handler);
                        if (disableInteractableAfterUse)
                        {
                            interactable.enabled = false;
                        }

                        if (!string.IsNullOrWhiteSpace(signalOnInteract))
                        {
                            context.SendSignal(signalOnInteract);
                        }

                        if (completeQuestOnInteract)
                        {
                            TryCompleteQuest(questService);
                        }

                        tcs.TrySetResult(true);
                    };

                    interactable.AddOnInteractListener(handler);
                    waitList.Add(tcs.Task);
                }
            }

            if (waitForQuestCompletion && questService != null && !string.IsNullOrWhiteSpace(questId))
            {
                if (questService.GetQuestStatus(questId) == QuestStatus.Completed)
                {
                    DisableAllGlows(disableGlowActions);
                }
                else
                {
                    var questCompletionTcs = new UniTaskCompletionSource<bool>();
                    questCompletionSubscription = questService.OnQuestCompleted
                        .Where(q => q != null && string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase))
                        .Take(1)
                        .Subscribe(_ =>
                        {
                            DisableAllGlows(disableGlowActions);
                            questCompletionTcs.TrySetResult(true);
                        });
                    waitList.Add(questCompletionTcs.Task);
                }
            }

            if (waitList.Count > 0)
            {
                await UniTask.WhenAll(waitList).AttachExternalCancellation(cancellationToken);
            }

            return StoryEventResult.Completed(waitForInteraction ? "Glow object interacted." : "Glow applied to targets.");
        }
        finally
        {
            questCompletionSubscription?.Dispose();
        }
    }

    private void DisableAllGlows(List<Action> disableActions)
    {
        if (disableActions == null)
        {
            return;
        }

        foreach (var action in disableActions)
        {
            action?.Invoke();
        }

        disableActions.Clear();
    }

    private void TryStartQuest(IQuestService questService)
    {
        if (questService == null)
        {
            Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] Cannot auto-start quest because IQuestService is unavailable.");
            return;
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] startQuestOnActivate enabled but questId is empty.");
            return;
        }

        questService.StartQuest(questId);
    }

    private void TryCompleteQuest(IQuestService questService)
    {
        if (questService == null)
        {
            Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] Cannot auto-complete quest because IQuestService is unavailable.");
            return;
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] completeQuestOnInteract enabled but questId is empty.");
            return;
        }

        questService.CompleteQuest(questId);
    }

    private bool TryEnableGlow(GameObject target, out Action disableGlow)
    {
        disableGlow = null;
        if (target == null)
        {
            return false;
        }

        if (useFridgeGlow)
        {
            var fridgeGlow = target.GetComponent<FridgeGlowController>();
            if (fridgeGlow == null && addGlowComponentIfMissing)
            {
                fridgeGlow = target.AddComponent<FridgeGlowController>();
            }

            if (fridgeGlow == null)
            {
                Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] Target '{target.name}' lacks {nameof(FridgeGlowController)} and could not add one.");
                return false;
            }

            var configuredPulseVariation = pulse ? Mathf.Max(0f, fridgeGlowPulseVariation) : 0f;
            var configuredPulseSpeed = pulse ? Mathf.Max(0f, pulseSpeed) : 0f;

            fridgeGlow.ConfigureGlow(
                color: glowColor,
                intensity: glowIntensity,
                newPulseSpeed: configuredPulseSpeed,
                newScale: fridgeGlowScale,
                newFresnelPower: fridgeGlowFresnelPower,
                newPulseVariation: configuredPulseVariation);

            fridgeGlow.EnableGlow();
            disableGlow = fridgeGlow.DisableGlow;
            return true;
        }

        var glow = target.GetComponent<SceneObjectGlowController>();
        if (glow == null && addGlowComponentIfMissing)
        {
            glow = target.AddComponent<SceneObjectGlowController>();
        }

        if (glow == null)
        {
            Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] Target '{target.name}' lacks {nameof(SceneObjectGlowController)} and could not add one.");
            return false;
        }

        glow.Configure(glowColor, glowIntensity, pulse, pulseSpeed);
        glow.EnableGlow();
        disableGlow = glow.DisableGlow;
        return true;
    }

    private List<GameObject> FindTargets(string desiredName)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        var results = new List<GameObject>();
        if (searchAllScenes)
        {
            var count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                Collect(scene, desiredName, comparison, results);
            }
        }
        else
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() && SceneManager.sceneCount > 0)
            {
                active = SceneManager.GetSceneAt(0);
            }

            if (active.IsValid())
            {
                Collect(active, desiredName, comparison, results);
            }
        }

        return results;
    }

    private void Collect(Scene scene, string desiredName, StringComparison comparison, List<GameObject> buffer)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Traverse(roots[i], desiredName, comparison, buffer);
        }
    }

    private void Traverse(GameObject node, string desiredName, StringComparison comparison, List<GameObject> buffer)
    {
        if (node == null)
        {
            return;
        }

        if (!includeInactive && !node.activeInHierarchy)
        {
            return;
        }

        if (string.Equals(node.name, desiredName, comparison))
        {
            buffer.Add(node);
        }

        var transform = node.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            Traverse(transform.GetChild(i).gameObject, desiredName, comparison, buffer);
        }
    }

    private static async UniTask<IQuestService> ResolveQuestServiceAsync(GameFlowContext context)
    {
        try
        {
            if (context != null && context.IsServiceReady<IQuestService>())
            {
                return await context.GetServiceAsync<IQuestService>();
            }

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.IsServiceReady<IQuestService>())
            {
                return await ServiceLocator.Instance.GetAsync<IQuestService>();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{nameof(GlowSceneObjectUntilInteractStoryEventAsset)}] Failed to resolve IQuestService: {ex.Message}");
        }

        return null;
    }
}
