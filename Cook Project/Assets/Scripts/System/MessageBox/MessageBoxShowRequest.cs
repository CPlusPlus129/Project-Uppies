/// <summary>
/// Request data sent from Manager to UI
/// </summary>
public struct MessageBoxShowRequest
{
    public string Message;
    public string[] ButtonTexts;
    public float AutoCloseDuration; // 0 = no auto-close
}