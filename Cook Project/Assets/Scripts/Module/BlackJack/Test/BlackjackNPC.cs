using TMPro;
using UnityEngine;

namespace BlackjackGame
{
    public class BlackjackNPC : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName;
        [SerializeField] private TextMeshPro nameText;

        private void Awake()
        {
            if (nameText != null)
                nameText.text = npcName;
        }

        public void Interact()
        {
            UIRoot.Instance.GetUIComponent<BlackjackUI>().Open();
        }

    }
}