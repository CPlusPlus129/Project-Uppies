namespace BlackjackGame
{
    public enum GameState
    {
        WaitingForBet,
        DealingInitialCards,
        PlayerTurn,
        DealerTurn,
        RoundEnd,
        GameOver
    }

    public enum RoundResult
    {
        None,
        PlayerWin,
        DealerWin,
        Push,
        PlayerBlackjack
    }

    public enum PlayerAction
    {
        Hit,
        Stand
    }
}