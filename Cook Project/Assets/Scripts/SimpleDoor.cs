using R3;
using UnityEngine;

public class SimpleDoor : MonoBehaviour
{
    public Animator anim;
    public Collider col;
    public ReactiveProperty<bool> isOpen = new ReactiveProperty<bool>();

    private void Awake()
    {
        col ??= GetComponent<Collider>();
        isOpen.Subscribe(isOn =>
        {
            anim.SetBool("IsOpen", isOn);
            col.enabled = !isOn;
        }).AddTo(this);
    }

    public void Open() => isOpen.Value = true;
    public void Close() => isOpen.Value = false;
}