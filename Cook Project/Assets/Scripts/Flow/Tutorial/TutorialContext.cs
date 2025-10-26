using NvJ.Rendering;
using System.Collections.Generic;
using UnityEngine;

public class TutorialContext
{
    public IDialogueService DialogueService;
    public IOrderManager OrderManager;
    public IInventorySystem InventorySystem;
    public Customer Customer;
    public Queue<SimpleDoor> Doors;
    public Queue<EmissionIndicator> DoorArrows;
    public Queue<EmissionIndicator> PrevDoorArrows;
    public Queue<FoodSource> Foods;
    public Queue<TriggerZone> TriggerZones;
    public Queue<string> TutorialHints;

    public FlamePillarEffect satanTeleportEffect;
    public FlamePillarEffect stanTeleportEffect;

    public GameObject satanLight;
    public GameObject backArrow;

    public string OrderName;
    
}