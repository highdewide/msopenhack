using System;
using System.Threading.Tasks;
using System.Collections.Generic;


using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;

using Newtonsoft.Json;
using AdaptiveCards;


namespace Microsoft.Bot.Sample.SimpleEchoBot
{
	// Question class
    public class QuestionOption
    {
        public int id { get; set; }
        public string text { get; set; }
    }

    public class QuestionObject
    {
        public int id { get; set; }
        public string text { get; set; }
        public List<QuestionOption> questionOptions { get; set; }
    }

	// Answer class
	public class AnswerObject
	{
		public bool correct { get; set; }
		public string achievementBadge { get; set; }
		public string achievementBadgeIcon { get; set; }
	}

    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;

		//for bot api parameter
        protected string  userId    ;
        protected int questionId    = 0;
        protected int answerId    	= 0;

//		protected int questionNum = 4;
        protected String[] choiceText = new string[4];
        protected int[] choiceId = new int[4];

//		QuestionObject question1 = new QuestionObject();



        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            if (message.Text == "quiz")
            {
                var httpClient = new HttpClient();
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "id", "4ca3eb2c-9880-4337-bad1-32edb3285e08" }
                        });
				// 質問を取得
				QuestionObject question = await GetQuestionAsync(content);

				questionId = question.id;

                choiceText[0] = question.questionOptions[0].text.ToString();
				choiceText[1] = question.questionOptions[1].text.ToString();
				choiceText[2] = question.questionOptions[2].text.ToString();
				choiceText[3] = question.questionOptions[3].text.ToString();

                choiceId[0] = question.questionOptions[0].id;
				choiceId[1] = question.questionOptions[1].id;
				choiceId[2] = question.questionOptions[2].id;
				choiceId[3] = question.questionOptions[3].id;

                PromptDialog.Choice(
                    context,
                    this.AnswerDialog,
					new[]
					{
                    choiceText[0], 
					choiceText[1],
					choiceText[2],
					choiceText[3],
					},
                    question.text.ToString()
                    );

            }
            else
            {
                await context.PostAsync($"{this.count++}: You said {message.Text}");
                context.Wait(MessageReceivedAsync);
            }
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }

        private async Task<QuestionObject> GetQuestionAsync(FormUrlEncodedContent content)
        {
            // API から質問を取得 
            var httpClient = new HttpClient();
            var questionResult = await httpClient.PostAsync("https://msopenhack.azurewebsites.net/api/trivia/question", content);

            // API 取得したデータをデコードして QuestionObject に取得
            string questionString = await questionResult.Content.ReadAsStringAsync();
            var questionObject = JsonConvert.DeserializeObject<QuestionObject>(questionString);
            return questionObject;
        }

        public async Task AnswerDialog(IDialogContext context, IAwaitable<object> result)  
        { 
            var selectedMenu = await result;    
			setIdFromAnswer(selectedMenu.ToString());

            var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "userId", "4ca3eb2c-9880-4337-bad1-32edb3285e08" },
                        { "questionId", $"{questionId}" },
                        { "answerId", $"{answerId}" }
                    });

            // API から質問を取得 
            var answerResult = await httpClient.PostAsync("https://msopenhack.azurewebsites.net/api/trivia/answer", content);

            // API 取得したデータをデコードして AnserObject に取得
            string answerString = await answerResult.Content.ReadAsStringAsync();
            var answerObject = JsonConvert.DeserializeObject<AnswerObject>(answerString);

			var activity = await result as Activity;
		    // 返答メッセージを作成
		    var message = context.MakeMessage();

			var body = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock
				{
					Text = $"Your Answer is {answerObject.correct}",
					Size = AdaptiveTextSize.Large
				},
				new AdaptiveTextBlock
				{
					Text = $"Your Class is {answerObject.achievementBadge.ToString()}",
					Spacing = AdaptiveSpacing.None
				},
				new AdaptiveImage
				{
					Url = new Uri($"{answerObject.achievementBadgeIcon.ToString()}")
				}
			};
			var contents = new AdaptiveCard
			{
				Version = "1.0",
				Body = body
			};
			var attachment = new Attachment
			{
				ContentType = "application/vnd.microsoft.card.adaptive",
				Content = contents
			};
			message.Attachments.Add(attachment);
			await context.PostAsync(message);
			context.Wait(MessageReceivedAsync);
        }

        private async Task setIdFromAnswer(String answerText)
        {
			if(answerText == choiceText[0])
			{
				answerId = choiceId[0];
			}
			else if(answerText == choiceText[1])
			{
				answerId = choiceId[1];
			}
			else if(answerText == choiceText[2])
			{
				answerId = choiceId[2];
			}
			else if(answerText == choiceText[3])
			{
				answerId = choiceId[3];
			}
        }
 
//        private async Stirng GetDialogValue(IDialogContext context, IAwaitable<object> result)  
//        { 
//            var selectedMenu = await result;    
//			Console.WriteLine("Press any key to exit.");
//			Console.WriteLine(selectedMenu);
//
//            return selectedMenu;
//        }
    }
}
