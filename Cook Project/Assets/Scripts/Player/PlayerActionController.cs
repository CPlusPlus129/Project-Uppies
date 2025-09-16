using UnityEngine;
using UnityEngine.InputSystem;

class PlayerActionController
{
    public void ScrollHotBar(InputAction.CallbackContext ctx)
    {
        var delta = ctx.ReadValue<Vector2>().y;
        if (delta > 0)
            InventorySystem.Instance.SelectSlot(InventorySystem.Instance.SelectedIndex.Value - 1);
        else if (delta < 0)
            InventorySystem.Instance.SelectSlot(InventorySystem.Instance.SelectedIndex.Value + 1);
    }

    public void DropItem()
    {
        var selectedItem = InventorySystem.Instance.GetSelectedItem();
        if (selectedItem == null) return;
        // just discard it for now
        InventorySystem.Instance.RemoveSelectedItem();
    }

    public void OnItemHotbarClicked(InputAction.CallbackContext ctx)
    {
        var control = ctx.control;
        char last = control.path[^1];
        if (last is >= '1' and <= '4')
        {
            int index = last - '1';
            InventorySystem.Instance.SelectSlot(index);
        }
    }
}