using AdaptiveCards;
using Microsoft.Bot.Connector;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    class CardBuilder
    {
        public static AdaptiveCard IntroductionCard()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveContainer titleContainer = new AdaptiveContainer();
            AdaptiveTextBlock heading = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.WhoIsBot,
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Large,
                Wrap = true
            };
            AdaptiveTextBlock botDescription = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.BotDescription,
                Wrap = true
            };
            AdaptiveTextBlock optionDescription = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.StartingOptionDescription,
                Wrap = true
            };

            titleContainer.Items.Add(heading);
            titleContainer.Items.Add(botDescription);
            titleContainer.Items.Add(optionDescription);

            card.Body.Add(titleContainer);

            AdaptiveColumnSet options = new AdaptiveColumnSet();

            options.Columns.Add(AdaptiveCardHelper.CreateAdaptiveColumnWithImage(
                PresentationDialogStrings.WebResearch, PresentationDialogStrings.WebResearchUrl
            ));
            options.Columns.Add(AdaptiveCardHelper.CreateAdaptiveColumnWithImage(
                PresentationDialogStrings.PresentationDesign, PresentationDialogStrings.PresentationDesignUrl
            ));
            options.Columns.Add(AdaptiveCardHelper.CreateAdaptiveColumnWithImage(
                PresentationDialogStrings.PersonalTasks, PresentationDialogStrings.PersonalTasksUrl
            ));

            card.Body.Add(options);

            return card;

        }

        public static AdaptiveCard PresentationIntro()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.LetsBeginDescription,
                Spacing = AdaptiveSpacing.ExtraLarge,
                Wrap = true
            };
            AdaptiveTextBlock whatWeDo = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.LetsBeginWhatWeDo,
                Spacing = AdaptiveSpacing.Medium,
                Wrap = true
            };
            AdaptiveTextBlock confirmation = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.LetsBeginConfirmation,
                Spacing = AdaptiveSpacing.Medium,
                Wrap = true
            };

            AdaptiveSubmitAction action = new AdaptiveSubmitAction()
            {
                Title = PresentationDialogStrings.LetsBegin,
                Data = new AdaptiveCardHelper.ResponseObject()
                {
                    msteams = new CardAction()
                    {
                        Text = PresentationDialogStrings.LetsBegin,
                        DisplayText = PresentationDialogStrings.LetsBegin,
                        Type = ActionTypes.MessageBack
                    }
                }
            };

            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText(string.Empty, PresentationDialogStrings.LetsBegin);
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;

            card.Body.Add(description);
            card.Body.Add(whatWeDo);
            card.Body.Add(confirmation);
            card.Body.Add(ctaContainer);

            return card;
        }

        public static AdaptiveCard PresentationPurposeOptions()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.PurposeDescription,
                Spacing = AdaptiveSpacing.ExtraLarge,
                Wrap = true
            };

            card.Body.Add(description);

            AdaptiveColumnSet options = new AdaptiveColumnSet()
            {
                Columns = {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.NewProject,
                        PresentationDialogStrings.NewProjectDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.ProgressReport,
                        PresentationDialogStrings.ProgressReportDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.Educate,
                        PresentationDialogStrings.EducateDesc
                    )
                },
                Spacing = AdaptiveSpacing.ExtraLarge
            };

            card.Body.Add(options);

            return card;
        }

        public static AdaptiveCard PresentationStyleCard(string deck)
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = $"Since you're making a {deck} deck, we recommend one of these styles. Which visual style do you prefer?",
                Wrap = true,
            };

            AdaptiveColumnSet styleOptions = new AdaptiveColumnSet()
            {
                Columns = {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Modern", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_modern_1.png")),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Corporate", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_corporate_1.png")),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Abstract", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_abstract_1.png")),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(string.Empty ,"Pick for me")
                }
            };

            card.Body.Add(description);
            card.Body.Add(styleOptions);

            return card;
        }
    }
}