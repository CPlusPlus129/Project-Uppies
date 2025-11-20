using UnityEngine;

public class DeathUI : MonoBehaviour
{
    [SerializeField] private Animator anim;

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}