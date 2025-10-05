using System.Collections;
using UnityEngine;

public class GameFlow : MonoSingleton<GameFlow>
{
    protected override void Awake()
    {
        StartCoroutine(StartGameSeconds(2));
    }

    private void StartGame()
    {
        ShiftSystem.Instance.StartGame();
    }

    private IEnumerator StartGameSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StartGame();
    }
}