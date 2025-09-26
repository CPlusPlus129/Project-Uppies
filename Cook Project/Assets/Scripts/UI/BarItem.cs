using UnityEngine;
using UnityEngine.UI;

public class BarItem : MonoBehaviour
{
    public Image barFill;

    public void UpdateValue(float curr, float max)
    {
        if(max <= 0)
        {
            barFill.fillAmount = 0;
            return;
        }

        barFill.fillAmount = (float)curr / max;
    }
}