using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace SampleFunctionBot
{
    /// <summary>
    /// These extend the Dialog Class to manage the spinup of the initial dialog 
    /// (Usually passed in via DI to the IBot Startup methods)
    /// </summary>
    public static class DialogExtensions
    {
        public static async Task Run(this Dialog dialog, ITurnContext turnContext, IStatePropertyAccessor<DialogState> dialogAccessor, CancellationToken cancellationToken = default(CancellationToken))
        {
            DialogSet dialogSet = new DialogSet(dialogAccessor);
            dialogSet.Add(dialog);

            DialogContext dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
            DialogTurnResult results = await dialogContext.ContinueDialogAsync(cancellationToken);

            if (results.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken);
            }
        }
    }


    /// <summary>
    /// Some extension methods to wrap common dialog activities
    /// </summary>
    public static class WaterfallStepContextExtensions
    {
        // Unwrap the result of the prior dialog step and cast it as Type T
        public static T GetResultAs<T>(this WaterfallStepContext wfsc)
        {

            return (T)(wfsc.Result);

        }
        
        // Unwrap the result of passed in options object from the prior waterfall step / dialog and cast it as Type T
        public static T GetOptionsAs<T>(this WaterfallStepContext wfsc)
        {

            return (T)(wfsc.Options);

        }

        // Unwrap a property in the current DialogState and cast it as Type T (using the name of the class passed in as the state placeholder variable name if valuename is null)
        public static T GetStateAs<T>(this WaterfallStepContext wfsc, string valuename = null)
        {
            valuename = valuename ?? typeof(T).FullName;
            T temp = (T)(wfsc.Values[valuename]);
            if (temp == null) temp = default(T);
            return temp;

        }

        // Wrap a property of type T and store it in the current DialogState (using the name of the class passed in as the state placeholder variable name if valuename is null) 
        // If my T (setthis) instance is null then call the default constructor and store an empty object
        public static T SetStateAs<T>(this WaterfallStepContext wfsc, T setthis, string valuename = null)
        {
            valuename = valuename ?? typeof(T).FullName;

            if (setthis != null)
            {
                wfsc.Values[valuename] = setthis;
            }
            else
            {
                wfsc.Values[valuename] = default(T);
            }

            return (T)wfsc.Values[valuename];

            // experimental features would let us do
            // return (T)(wfsc.Result) || default(T);

        }
    }

    /// <summary>
    /// Extensions on ITurnContext can be used from both WaterfallDialog Context (Via the stepContext.Context property) 
    /// or directly from the root ITurnContext instance
    /// </summary>
    public static class TurnDialogAndWaterfallContextExtensions
    {
        public static async Task<ResourceResponse> SendTextActivity(this ITurnContext dc, string message, CancellationToken cancel = default(CancellationToken))
        {
            return await dc.SendActivityAsync(MessageFactory.Text(message), cancel);
        }
        public static async Task SendMultipleTextActivity(this ITurnContext dc, string[] messages, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (string msg in messages)
            {
                await SendTextActivity(dc, msg, cancellationToken);
            }
        }
        public static OAuthPrompt BuildOAuthPrompt(this DialogContext dc, string _ConnectionName, string _SignInText)
        {
            return new OAuthPrompt(nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = _ConnectionName,
                    Text = _SignInText,
                    Title = "Sign In",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                });
        }

        public static async Task<DialogTurnResult> BuildChoicePrompt(this DialogContext dc, string message, string[] choicelist, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dc.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text(message),
                Choices = ChoiceFactory.ToChoices(choicelist.ToList<string>()),
            }, cancellationToken);
        }

        public static async Task<DialogTurnResult> BuildConfirmYesNoPrompt(this DialogContext dc, string message = "Please confirm: Yes to proceed or No to cancel", CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dc.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text(message),
                Choices = ChoiceFactory.ToChoices(new List<string>() { "Yes", "No" }),
            }, cancellationToken);
        }

        public static async Task<DialogTurnResult> BuildNumberPrompt(this DialogContext dc, string message = "Please confirm: Yes to proceed or No to cancel", CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dc.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions
            {
                Prompt = MessageFactory.Text(message)                

            }, cancellationToken);
        }

        public static async Task<DialogTurnResult> BuildMultilineTextPrompt(this DialogContext dc, string[] messages, string finalMessage, CancellationToken cancellationToken = default(CancellationToken))
        {
            
            foreach (string msg in messages)
            {
                await SendTextActivity(dc.Context, msg, cancellationToken);
            }
            return await dc.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text(finalMessage)
            }, cancellationToken);
        }

        public static async Task<DialogTurnResult> BuildTextPrompt(this WaterfallStepContext wsfc, string message, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await wsfc.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text(message)
            }, cancellationToken);
        }

        public static async Task<ResourceResponse> SendSuggestedActionCardActivity(this ITurnContext dc, string message, string[] options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var reply = dc.Activity.CreateReply(message);
            reply.SuggestedActions = new SuggestedActions() { Actions = new List<CardAction>() };
            foreach (string option in options)
            {
                reply.SuggestedActions.Actions.Add(new CardAction() { Title = option, Type = ActionTypes.ImBack, Value = option });
            };
            return await dc.SendActivityAsync(reply, cancellationToken);

        }
        public static async Task<ResourceResponse> SendHeroCardActivity(this ITurnContext dc, string message, Dictionary<string, string> options, string actiontotake = ActionTypes.ImBack, CancellationToken cancellationToken = default(CancellationToken))
        {
            var reply = dc.Activity.CreateReply(message);
            var card = new HeroCard
            {
                Text = message,
                Buttons = new List<CardAction>()
            };

            foreach (KeyValuePair<string, string> option in options)
            {
                card.Buttons.Add(new CardAction() { Title = option.Key, Type = actiontotake, Value = option.Value, Text = option.Key, DisplayText = option.Key });
            };

            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
            return await dc.SendActivityAsync(reply, cancellationToken);

        }

        public static async Task<ResourceResponse> SendHeroCardActivityWithActions(this ITurnContext dc, string _Title, string _Subtitle, string _Message, IList<CardAction> options = default(List<CardAction>), CancellationToken cancellationToken = default(CancellationToken))
        {
            var reply = dc.Activity.CreateReply();
            var card = new HeroCard
            {
                Title = _Title,
                Subtitle = _Subtitle,
                Text = _Message

            };
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
            return await dc.SendActivityAsync(reply, cancellationToken);
        }

        public static bool MatchResultChoiceStartsWithLCTrim(this WaterfallStepContext wsfc, string matchwith)
        {
            return wsfc.GetResultAs<FoundChoice>().StartsWithLowerCaseTrimmedString(matchwith);
        }

        public static string GetFoundChoiceFromResultAs<T>(this WaterfallStepContext wsfc) {

            return (string)wsfc.GetResultAs<FoundChoice>().Value;

        }

    }
    public static class FoundChoiceExtensions
    {

        public static string ToLowerCaseTrimmedString(this FoundChoice foundchoice)
        {
            return foundchoice.Value.ToString().ToLower().Trim();
        }

        public static bool StartsWithLowerCaseTrimmedString(this FoundChoice foundchoice, string matchwith)
        {
            return foundchoice.Value.ToString().ToLower().Trim().StartsWith(matchwith);
        }
               
        public static string ToTrimmedString(this FoundChoice foundchoice)
        {
            return foundchoice.Value.ToString().Trim();
        }
    }
}
