public class NumberGuessingGame : IPuzzle
{
    readonly IPuzzleGameManager puzzleGameManager;
    public PuzzleGameType puzzleType => PuzzleGameType.NumberGuessing;
    private string answer;
    private string hint;

    public NumberGuessingGame(IPuzzleGameManager puzzleGameManager)
    {
        this.puzzleGameManager = puzzleGameManager;
        GenerateAnswerAndHint();
    }

    private void GenerateAnswerAndHint()
    {
        var answerNum = UnityEngine.Random.Range(0, 10000);
        answer = answerNum.ToString("D4");
        var randomNumber = UnityEngine.Random.Range(0, 10000);


        string comparisonHint = answerNum <= randomNumber ?
            $"ans<={randomNumber:D4}" : $"ans>={randomNumber:D4}";

        int oddCount = 0;
        int evenCount = 0;
        int sum = 0;

        foreach (char digit in answer)
        {
            int num = digit - '0';
            sum += num;
            if (num % 2 == 0)
                evenCount++;
            else
                oddCount++;
        }

        string countHint = UnityEngine.Random.Range(0, 2) == 1 ?
            $"ans has {oddCount} odd numbers" : $"ans has {evenCount} even numbers";

        hint = $"{comparisonHint}, {countHint}, sum of four digits is {sum}";
    }

    public string GetHint()
    {
        return hint;
    }
    
    public string GetAnswer()
    {
        return answer;
    }

    public bool GuessNumber(string guess)
    {
        bool isCorrect = answer == guess.PadLeft(4, '0');

        if (isCorrect)
        {
            puzzleGameManager.CompletePuzzleGame(PuzzleGameType.NumberGuessing);
        }

        return isCorrect;
    }
}