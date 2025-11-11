using BlackjackGame;
using Cysharp.Threading.Tasks;
using R3;
using System.Linq;
using UnityEngine;

public class GambleTable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        UIRoot.Instance.GetUIComponent<BlackjackUI>().Open();
    }


}