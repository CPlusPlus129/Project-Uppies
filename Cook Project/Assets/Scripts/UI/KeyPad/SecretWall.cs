using UnityEngine;

public class SecretWall : MonoBehaviour
{
    [SerializeField]
    private Animator anim;

    private void Awake()
    {
        SetOpen(false);
    }

    public void SetOpen(bool isOpen)
    {
        anim.SetBool("IsWallOpen", isOpen);
    }
}