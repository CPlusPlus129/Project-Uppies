using UnityEngine;
using UnityEngine.UI;

namespace DialogueModule
{
    /// <summary>
    /// Controls the color of the dialogue name card background image.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class DialogueNameCardImage : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Color defaultColor = Color.white;

        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            if (targetImage != null)
                defaultColor = targetImage.color;
        }

        public void SetColor(Color color)
        {
            if (targetImage == null)
                return;
            targetImage.color = color;
        }

        public void ResetToDefault()
        {
            if (targetImage == null)
                return;
            targetImage.color = defaultColor;
        }

        public void SetDefaultColor(Color color)
        {
            defaultColor = color;
        }
    }
}
