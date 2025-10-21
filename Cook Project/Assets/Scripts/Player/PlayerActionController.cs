using UnityEngine;
using UnityEngine.InputSystem;

class PlayerActionController
{
    public IInventorySystem inventorySystem { get; set; }

    public void ScrollHotBar(InputAction.CallbackContext ctx)
    {
        var delta = ctx.ReadValue<Vector2>().y;
        if (delta > 0)
            inventorySystem.SelectSlot(inventorySystem.SelectedIndex.Value - 1);
        else if (delta < 0)
            inventorySystem.SelectSlot(inventorySystem.SelectedIndex.Value + 1);
    }

    public void DropItem(InputAction.CallbackContext ctx)
    {
        var selectedItem = inventorySystem.GetSelectedItem();
        if (selectedItem == null) return;
        // just discard it for now
        inventorySystem.RemoveSelectedItem();
    }

    public void OnItemHotbarClicked(InputAction.CallbackContext ctx)
    {
        var control = ctx.control;
        char last = control.path[^1];
        if (last is >= '1' and <= '4')
        {
            int index = last - '1';
            inventorySystem.SelectSlot(index);
        }
    }

    
}