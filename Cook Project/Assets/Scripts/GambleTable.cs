using BlackjackGame;
using Cysharp.Threading.Tasks;
using R3;
using System.Linq;
using UnityEngine;

public class GambleTable : InteractableBase
{
    public override void Interact()
    {
        UIRoot.Instance.GetUIComponent<BlackjackUI>().Open();
    }


}
