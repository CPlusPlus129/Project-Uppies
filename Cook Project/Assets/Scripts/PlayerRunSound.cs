using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerRunSound : MonoBehaviour
{
    public float moveThreshold = 0.1f;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = new Vector2(horizontal, vertical).magnitude > moveThreshold;

        if (isMoving && !audioSource.isPlaying)
            audioSource.Play();
        else if (!isMoving && audioSource.isPlaying)
            audioSource.Stop();
    }
}
