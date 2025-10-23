using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;           // Assign Player's AudioSource
    public AudioClip runningClip;             // Single looping running sound
    public float volume = 0.8f;               // Volume level
    public float fadeSpeed = 5f;              // How smoothly sound fades in/out

    [Header("Movement Detection")]
    public float minMoveThreshold = 0.1f;     // Speed before running sound plays

    Rigidbody rb;
    CharacterController cc;
    float targetVolume = 0f;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CharacterController>();

        if (runningClip != null)
        {
            audioSource.clip = runningClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
        }
        else
        {
            Debug.LogWarning("PlayerFootsteps: No running clip assigned.");
        }
    }

    void Update()
    {
        float moveSpeed = GetMoveSpeed();

        // Decide if player is moving
        if (moveSpeed > minMoveThreshold)
        {
            targetVolume = volume;
            if (!audioSource.isPlaying && runningClip != null)
                audioSource.Play();
        }
        else
        {
            targetVolume = 0f;
        }

        // Smoothly fade volume up or down
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);

        // Stop playback if volume fully faded out
        if (audioSource.volume <= 0.01f && audioSource.isPlaying && targetVolume == 0f)
            audioSource.Stop();
    }

    float GetMoveSpeed()
    {
        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            return v.magnitude;
        }

        if (cc != null)
        {
            Vector3 v = cc.velocity;
            v.y = 0f;
            return v.magnitude;
        }

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        return input.magnitude;
    }
}
