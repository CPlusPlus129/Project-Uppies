using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class VaultDoorController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string doorId = "VaultDoor";
    [SerializeField] private Transform handleBone;
    [SerializeField] private Transform doorBone;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    [Header("Animation Settings")]
    [SerializeField] private float handleSpinDuration = 1.5f;
    [SerializeField] private Vector3 handleSpinRotation = new Vector3(0, 0, 360f);
    [SerializeField] private int handleSpinLoops = 2;
    [SerializeField] private float doorOpenDuration = 2.0f;
    [SerializeField] private Vector3 doorOpenRotation = new Vector3(0, 110f, 0f);
    [SerializeField] private Ease animationEase = Ease.InOutQuad;

    [Header("Events")]
    public UnityEvent OnOpenStart;
    public UnityEvent OnOpenComplete;
    public UnityEvent OnCloseStart;
    public UnityEvent OnCloseComplete;

    private bool isOpen = false;
    private static Dictionary<string, VaultDoorController> instances = new Dictionary<string, VaultDoorController>();

    private void Awake()
    {
        if (!string.IsNullOrEmpty(doorId))
        {
            if (instances.ContainsKey(doorId))
            {
                Debug.LogWarning($"Duplicate VaultDoorController ID '{doorId}' detected on {gameObject.name}. Overwriting.");
                instances[doorId] = this;
            }
            else
            {
                instances.Add(doorId, this);
            }
        }

        // Auto-setup if missing
        if (doorBone == null)
        {
            // Try to find recursively or direct child
            doorBone = transform.Find("SM_Bld_Vault_Door_01_Door_01");
        }
        
        if (handleBone == null && doorBone != null)
        {
            handleBone = doorBone.Find("SM_Bld_Vault_Door_01_Handle_01");
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(doorId) && instances.ContainsKey(doorId) && instances[doorId] == this)
        {
            instances.Remove(doorId);
        }
    }

    public static VaultDoorController Get(string id)
    {
        if (instances.TryGetValue(id, out var controller))
        {
            return controller;
        }
        return null;
    }

    public async UniTask OpenAsync(CancellationToken ct = default)
    {
        if (isOpen) return;
        isOpen = true;
        
        OnOpenStart?.Invoke();
        PlaySound(openSound);

        // Kill existing tweens to prevent conflict
        if (handleBone != null) handleBone.DOKill();
        if (doorBone != null) doorBone.DOKill();

        // 1. Spin Handle (if assigned)
        if (handleBone != null)
        {
            await handleBone.DOLocalRotate(handleSpinRotation, handleSpinDuration, RotateMode.FastBeyond360)
                .SetEase(animationEase)
                .SetRelative(true)
                .SetLoops(handleSpinLoops, LoopType.Incremental)
                .AwaitTween(cancellationToken: ct);
        }

        // 2. Open Door (if assigned)
        if (doorBone != null)
        {
            await doorBone.DOLocalRotate(doorOpenRotation, doorOpenDuration)
                .SetEase(animationEase)
                .AwaitTween(cancellationToken: ct);
        }

        OnOpenComplete?.Invoke();
    }

    public async UniTask CloseAsync(CancellationToken ct = default)
    {
        if (!isOpen) return;
        isOpen = false;

        OnCloseStart?.Invoke();
        PlaySound(closeSound);

        if (handleBone != null) handleBone.DOKill();
        if (doorBone != null) doorBone.DOKill();

        // 1. Close Door
        if (doorBone != null)
        {
            await doorBone.DOLocalRotate(Vector3.zero, doorOpenDuration)
                .SetEase(animationEase)
                .AwaitTween(cancellationToken: ct);
        }

        // 2. Spin Handle Back
        if (handleBone != null)
        {
            await handleBone.DOLocalRotate(-handleSpinRotation, handleSpinDuration, RotateMode.FastBeyond360)
                .SetEase(animationEase)
                .SetRelative(true)
                .SetLoops(handleSpinLoops, LoopType.Incremental)
                .AwaitTween(cancellationToken: ct);
        }

        OnCloseComplete?.Invoke();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Debug test methods
    [ContextMenu("Open Door")]
    public void DebugOpen() => OpenAsync().Forget();
    
    [ContextMenu("Close Door")]
    public void DebugClose() => CloseAsync().Forget();
}

public static class VaultDoorDOTweenExtensions
{
    public static async UniTask AwaitTween(this Tween tween, CancellationToken cancellationToken = default)
    {
        if (tween == null) return;

        var tcs = new UniTaskCompletionSource();

        tween.OnComplete(() => tcs.TrySetResult());
        tween.OnKill(() => tcs.TrySetResult());

        using (cancellationToken.Register(() => 
        {
            tween.Kill();
            tcs.TrySetCanceled();
        }))
        {
            await tcs.Task;
        }
    }
}
