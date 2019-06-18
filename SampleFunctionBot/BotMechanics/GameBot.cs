using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using SampleFunctionBot.GamePlay;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleFunctionBot
{
    public class GameBot : ActivityHandler
    {
        protected readonly ConversationState _conversationState;
        protected readonly GameData _pd;
        protected readonly Dialog _dialog;

        public GameBot(ConversationState conversationState, GameData pd, Dialog dialog)
        {

            _conversationState = conversationState;
            _pd = pd;
            _dialog = dialog;

        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            
            // Invoke the extension method to pass control to whatever Dialog was injected via the DI startup method
            await _dialog.Run(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);

        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {


            // Close down and save state at the end of the turn
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // Greet a new joiner to the conversation
            foreach (var member in membersAdded)
            {

                await turnContext.SendTextActivity($"Welcome, {member.Name}");

            }
        }
    }



}
