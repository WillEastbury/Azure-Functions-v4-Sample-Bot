using System.Collections.Generic;
using System.Linq;

namespace SampleFunctionBot.GamePlay
{
    /// <summary>
    /// Schema for the JSON Game File to deserialize to (The list of games) with some convenience methods
    /// </summary>
    public class GameData
    {
        public IEnumerable<Game> Games;

        public List<string> GetGameList(string GameType)
        {
            return Games.Where(e => e.GameType == GameType).Select(e => e.GameName).ToList();
        }

        public Game GetGameData(string GameName)
        {
            return Games.Where(e => e.GameName.ToLower().Trim() == GameName).FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Schema for an individual game in the list of games (
    /// </summary>
    public class Game
    {

        public string GameName;
        public string GameType;
        public string GameStepText;
        public IEnumerable<string> QuestionList;

    }

    /// <summary>
    /// Scoring and convenience methods for the game engine 
    /// </summary>
    public class ScoreState
    {

        public Game CurrentGame;

        public string PlayerName;

        public int GetCurrentPositionGame()
        {
            return QuestionsCorrect + QuestionsIncorrect;
        }

        public string GetCurrentQuestion()
        {

            return CurrentGame.GameStepText + " " + CurrentGame.QuestionList.ElementAt(GetCurrentPositionGame()) + " ?";

        }

        public string GetCurrentAnswer()
        {

            return CurrentGame.QuestionList.ElementAt(GetCurrentPositionGame());

        }

        public int GetCurrentScore()
        {
            return (QuestionsCorrect * 2) - QuestionsIncorrect;
        }

        public void MoveToNextTurn(bool answerCorrect)
        {
            if (answerCorrect)
            {
                QuestionsCorrect = QuestionsCorrect + 1;
            }
            else
            {
                QuestionsIncorrect = QuestionsIncorrect + 1;
            }

        }

        public int QuestionsCorrect;
        public int QuestionsIncorrect;

        public string GetWordForCurrentTurn()
        {

            return this.CurrentGame.QuestionList.ElementAt(this.GetCurrentPositionGame());

        }

        public ScoreState(Game whichGame)
        {

            CurrentGame = whichGame;
            QuestionsCorrect = 0;
            QuestionsIncorrect = 0;

        }

        public ScoreState()
        {

        }

        public bool IsGameFinished()
        {

            return this.GetCurrentPositionGame() >= this.CurrentGame.QuestionList.Count();

        }
    }
}
