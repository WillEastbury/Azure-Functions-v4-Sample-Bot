using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using SampleFunctionBot.GamePlay;
using System.Threading;
using System.Threading.Tasks;

namespace SampleFunctionBot.BotMechanics.Dialogs
{
    public class GameSetupDialog : ComponentDialog
    {

        private readonly GameData _pdata;

        public GameSetupDialog(GameData pdata) : base(nameof(GameSetupDialog))
        {

            _pdata = pdata;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                QuestionStepAsync,
                GameChoiceStepAsync,
                NameStepAsync,
                PlayGameStepAsync,
                EndGameStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new GamePlayQuestionDialog(nameof(GamePlayQuestionDialog)));

        }

        private async Task<DialogTurnResult> QuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BuildChoicePrompt("What kind of game would you like to play?", new[] { "Phonics", "Maths", "None" }, cancellationToken);

        }

        private async Task<DialogTurnResult> GameChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if (stepContext.GetFoundChoiceFromResultAs<string>() != "None")
            {
                return await stepContext.BuildChoicePrompt($"Amazing, let's play a {stepContext.GetFoundChoiceFromResultAs<string>()} game, which one would you like to play? ", _pdata.GetGameList(stepContext.GetFoundChoiceFromResultAs<string>()).ToArray(), cancellationToken);
            }
            else
            {
                
                await stepContext.Context.SendMultipleTextActivity(new[] {
                    "That's a shame, let me know when you do want to play by sending me any message to restart, I'm going for a nap ...",
                    "ZZZZZZZZZZzzzzzzzzzzzzzzzzzzzzzz......."
                });

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Find the matching game to play and set up our state
            ScoreState ss = stepContext.SetStateAs(new ScoreState(_pdata.GetGameData(stepContext.GetResultAs<FoundChoice>().ToLowerCaseTrimmedString())));
            return await stepContext.BuildTextPrompt($"Great, let's play {ss.CurrentGame.GameName}, Please enter your name to begin.", cancellationToken);
        }

        private async Task<DialogTurnResult> PlayGameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var ss = stepContext.GetStateAs<ScoreState>();
            ss.PlayerName = stepContext.Context.Activity.Text;

            await stepContext.Context.SendTextActivity($"Welcome, young {ss.PlayerName} - let's play!");

            // Call the child question dialog
            return await stepContext.BeginDialogAsync(nameof(GamePlayQuestionDialog), stepContext.GetStateAs<ScoreState>(), cancellationToken);

        }

        private async Task<DialogTurnResult> EndGameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Find the matching game to play and set up our state
            ScoreState so = stepContext.GetResultAs<ScoreState>();

            await stepContext.Context.SendMultipleTextActivity(new[] {
                $"{so.PlayerName}, you finished the {so.CurrentGame.GameName} game!, with {so.GetCurrentScore()} points!",
                "Talk to me to restart" });

            return await stepContext.EndDialogAsync(so, cancellationToken);

        }
    }

    public class GamePlayQuestionDialog : ComponentDialog
    {

        public GamePlayQuestionDialog(string dialogId) : base(dialogId)
        {

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                    AskQuestionDialogStepAsync,
                    CheckAnswerDialogStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

        }

        private async Task<DialogTurnResult> AskQuestionDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Read in the passed down values from the parent dialog
            ScoreState ss = stepContext.SetStateAs(stepContext.GetOptionsAs<ScoreState>());

            await stepContext.Context.SendTextActivity(
                 $"Question {ss.GetCurrentPositionGame() + 1} coming up");

            await Task.Delay(2500);

            switch (ss.CurrentGame.GameType)
            {

                case "Maths":

                    return await stepContext.BuildNumberPrompt(ss.GetCurrentQuestion(), cancellationToken);

                case "Phonics":

                    return await stepContext.BuildTextPrompt(ss.GetCurrentQuestion(), cancellationToken);

            }

            return await stepContext.BuildTextPrompt("Something went wrong here, you don't seem to be following one of our standard games", cancellationToken);

        }

        private async Task<DialogTurnResult> CheckAnswerDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            ScoreState ss = stepContext.GetStateAs<ScoreState>();
            bool questioncorrect = false;

            switch (ss.CurrentGame.GameType)
            {

                case "Maths":

                    // Split the question out into operands
                    string[] qarray = ss.GetCurrentAnswer().Split(" ");
                    int op1 = int.Parse(qarray[0]);
                    string op = qarray[1];
                    int op2 = int.Parse(qarray[2]);
                    int answer = 0;

                    // Now calculate and evaluate the answer 

                    switch (op)
                    {
                        case "+":
                            answer = op1 + op2;
                            break;

                        case "-":
                            answer = op1 - op2;
                            break;

                        case "*":
                            answer = op1 * op2;
                            break;

                        case "/":
                            answer = op1 / op2;
                            break;

                        default:
                            break;

                    }

                    questioncorrect = (stepContext.GetResultAs<int>() == answer);
                    break;

                case "Phonics":

                    questioncorrect = (stepContext.GetResultAs<string>().ToLower().Replace(".", "").Trim() == ss.GetCurrentAnswer().ToLower().Trim());
                    break;

                default:

                    break;
            }

            if (questioncorrect)
            {
                ss.MoveToNextTurn(true);
                await stepContext.Context.SendTextActivity($"That's Right, an extra 2 points for you, taking you to {ss.GetCurrentScore()} in total");
            }
            else
            {
                ss.MoveToNextTurn(false);
                await stepContext.Context.SendTextActivity($"That's incorrect so you lose a point, leaving you with {ss.GetCurrentScore()}in total.");
            }

            if (ss.IsGameFinished())
            {
                // This is the last question, They're done, exit and return their ScoreState Object back.
                return await stepContext.EndDialogAsync(ss, cancellationToken);
            }
            else
            {
                // More to come ... so lets ask a new question 
                return await stepContext.ReplaceDialogAsync(nameof(GamePlayQuestionDialog), ss, cancellationToken);
            }
        }
    }

}
