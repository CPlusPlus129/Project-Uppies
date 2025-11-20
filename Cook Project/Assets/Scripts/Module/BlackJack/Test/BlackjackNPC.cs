using TMPro;
using UnityEngine;

namespace BlackjackGame
{
    public class BlackjackNPC : InteractableBase
    {
        [SerializeField] private string npcName;
        [SerializeField] private TextMeshPro nameText;

        protected override void Awake()
        {
            base.Awake();
            if (nameText != null)
                nameText.text = npcName;
        }

        public override void Interact()
        {
            UIRoot.Instance.GetUIComponent<BlackjackUI>().Open();
        }
    }
}
