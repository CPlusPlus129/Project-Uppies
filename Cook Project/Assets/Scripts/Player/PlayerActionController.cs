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

    public void OnDebugKeyClicked(InputAction.CallbackContext ctx)
    {
        var control = ctx.control;
        char last = control.path[^1];
        if (last is >= '0' and <= '3')
        {
            int index = last - '0';
            switch (index)
            {
                case 0:
                    Debug.Log($"Debug Num0: Set shiftsystem remain time to 3 sec");
                    ShiftSystem.Instance.shiftTimer.Value = 3f;
                    break;
                case 1:
                    Debug.Log($"Debug Num1: Add 1 served order");
                    ShiftSystem.Instance.completedOrderCount.Value += 1;
                    break;
                case 2:
                    Debug.Log($"Debug Num2: Start next shift");
                    ShiftSystem.Instance.StartNextShift();
                    break;
                default:
                    break;
            }
        }
    }
}