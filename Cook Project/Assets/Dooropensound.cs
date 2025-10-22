using UnityEngine;

public class DoorOpenSound : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var audio = animator.GetComponent<AudioSource>();
        if (audio != null && audio.clip != null)
            audio.Play();
    }
}
