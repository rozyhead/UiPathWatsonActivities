using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.ComponentModel;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
using System.IO.Ports;

namespace WatsonAssistantV2.Activities
{
    public class CallMessageStateless : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> ServiceUrl { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> ApiKey { get; set; }

        [Category("Input")]
        public InArgument<string> VersionDate { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> AssistantId { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Text { get; set; }

        [Category("Output")]
        public OutArgument<string> SuggestedIntent { get; set; }

        [Category("Output")]
        public OutArgument<IDictionary<string, double>> Confidencies { get; set; }

        public CallMessageStateless()
        {
            VersionDate = new InArgument<string>("2020-04-01");
        }

        protected override void Execute(CodeActivityContext context)
        {
            var serviceUrl = ServiceUrl.Get(context);
            var apiKey = ApiKey.Get(context);
            var versionDate = VersionDate.Get(context);

            var authenticator = new IamAuthenticator(apikey: apiKey);
            var service = new AssistantService(versionDate, authenticator);
            service.SetServiceUrl(serviceUrl);

            var assistantId = AssistantId.Get(context);
            var text = Text.Get(context);

            // SuggestedIntentの取得
            var suggestedIntent = FetchSuggestedIntent(assistantId, text, service);
            SuggestedIntent.Set(context, suggestedIntent);

            // Confidenciesの取得
            var confidencies = FetchIntentConfidencies(assistantId, text, service);
            Confidencies.Set(context, confidencies);
        }

        private string FetchSuggestedIntent(string assistantId, string text, AssistantService service)
        {
            var input = new MessageInputStateless()
            {
                MessageType = "text",
                Text = text
            };

            var response = service.MessageStateless(assistantId, input);
            var intents = response.Result.Output.Intents;
            return intents.Count > 0 ? intents[0].Intent : null;
        }

        private IDictionary<string, double> FetchIntentConfidencies(string assistantId, string text, AssistantService service)
        {
            var input = new MessageInputStateless()
            {
                MessageType = "text",
                Text = text,
                Options = new MessageInputOptionsStateless()
                {
                    AlternateIntents = true
                }
            };

            var response = service.MessageStateless(assistantId, input);
            var intents = response.Result.Output.Intents;

            return intents.ToDictionary(it => it.Intent, it => it.Confidence.GetValueOrDefault(0));
        }
    }
}
