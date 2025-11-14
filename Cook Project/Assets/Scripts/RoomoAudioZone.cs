using UnityEngine;
using System.Collections;

public class RoomAudioZone : MonoBehaviour
{
    public AudioSource audioSource;

    private Coroutine fadeCoroutine;
    private bool playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeIn());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeIn()
    {
        audioSource.loop = true;
        if (!audioSource.isPlaying) audioSource.Play();

        float targetVolume = 1f;
        while (audioSource.volume < targetVolume && playerInside)
        {
            audioSource.volume += Time.deltaTime * 1f; // fade speed
            yield return null;
        }
    }

    IEnumerator FadeOut()
    {
        float targetVolume = 0f;
        while (audioSource.volume > targetVolume && !playerInside)
        {
            audioSource.volume -= Time.deltaTime * 1f; // fade speed
            yield return null;
        }

        if (!playerInside)
            audioSource.Stop();
    }
}
