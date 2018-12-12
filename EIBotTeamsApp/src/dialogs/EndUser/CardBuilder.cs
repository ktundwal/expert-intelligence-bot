using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using static Microsoft.Office.EIBot.Service.dialogs.EndUser.AdaptiveCardHelper;

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
                    )
                }
                //Spacing = AdaptiveSpacing.ExtraLarge
            };
            AdaptiveColumnSet options2 = new AdaptiveColumnSet()
            {
                Columns = {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.Educate,
                        PresentationDialogStrings.EducateDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.OtherOption,
                        PresentationDialogStrings.OtherDec
                    )
                }
            };

            card.Body.Add(options);
            card.Body.Add(options2);
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

            var styleA = AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Modern", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_modern_1.png"));
            var styleB = AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Corporate", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_corporate_1.png"));
            var styleC = AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Abstract", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_abstract_1.png"));

            AdaptiveColumnSet styleOptions = new AdaptiveColumnSet()
            {
                Columns = {
                    styleA.column,
                    styleB.column,
                    styleC.column,
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(string.Empty ,"Pick for me")
                }
            };

            card.Body.Add(description);
            card.Body.Add(styleOptions);
            card.Actions.AddRange(new AdaptiveAction[] { styleA.preview, styleB.preview, styleC.preview });

            return card;
        }

        public static AdaptiveCard PresentationColorVariationCard(string style)
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = $"Which of theses variations do you like best?",
                Wrap = true,
            };

            var dark = AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Dark", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_dark_modern_2.png"));
            var light = AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Light", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_light_modern_2.png"));
            var colorful = AdaptiveCardHelper.CreateAdaptiveColumnWithImagePreview("Colorful", PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_colorful_modern_2.png"));

            AdaptiveColumnSet styleOptions = new AdaptiveColumnSet()
            {
                Columns = {
                    dark.column,
                    light.column,
                    colorful.column,
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText("None of these" ,"Let me describe what I want")
                }
            };

            card.Body.Add(description);
            card.Body.Add(styleOptions);
            card.Actions.AddRange(new AdaptiveAction[] { dark.preview, light.preview, colorful.preview });

            return card;
        }

        public static AdaptiveCard AnythingElseCard()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock last = new AdaptiveTextBlock()
            {
                Text = $"Great. Last question: Is there anything that you specifically DON'T want the designer to do? Any pet peeves? \n " +
                $"**Let us know in the reply box below, in one single message (we'll progress to the next step afterwards)**",
                Wrap = true,
            };

            //AdaptiveTextBlock question = new AdaptiveTextBlock()
            //{
            //    Text = $"Is there anything that you specifically DON'T want the designer to do? Any pet peeves?" +
            //    $"\n\n **Please reply using your text below**",
            //    Wrap = true
            //};

            //AdaptiveTextInput comment = new AdaptiveTextInput()
            //{
            //    Id = "comment",
            //    IsMultiline = true,
            //    MaxLength = 500,
            //    Placeholder = "Comments"
            //};

            //AdaptiveSubmitAction submit = new AdaptiveSubmitAction()
            //{
            //    Title = "Submit",
            //    Data = new AdaptiveCardHelper.ResponseObject()
            //    {
            //        msteams = new CardAction()
            //        {
            //            Type = ActionTypes.MessageBack,
            //            DisplayText = comment.Value
            //        }
            //    }
            //};

            card.Body.Add(last);
            //card.Body.Add(question);
            //card.Body.Add(comment);

            //card.Actions.Add(submit);
            return card;
        }

        public static AdaptiveCard ConfirmationCard()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock title = new AdaptiveTextBlock()
            {
                Text = $"Okay, here's a link to an online PowerPoint template file",
                Wrap = true,
            };

            AdaptiveTextBlock header = new AdaptiveTextBlock()
            {
                Text = $"In the file, you'll find prompts that ask you for...",
                Wrap = true,
            };

            AdaptiveTextBlock information = new AdaptiveTextBlock()
            {
                Text = "\n" +
                     "\n- The images you’d like to use" +
                     "\n- Any logos, icons or other assets for the designer" +
                     "\n- Any text or outline on the pages to give the designer an idea of the structure you’d like" +
                     "\n- Instructions for key slides the designer should focus on" +
                     "\n- Links or screenshots of examples we can use as a reference" +
                     "\n- Where you’ll be presenting (conference room on projector, online meeting, etc), it may help the designer",
                Wrap = true,
            };

            AdaptiveTextBlock lastDescription = new AdaptiveTextBlock()
            {
                Text = $"Let us know when you've added these to the file, and the designer will work on it." +
                $"\nPlease don’t include any sensitive information you don’t want the freelancer to see",
                Wrap = true,
            };

            card.Body.AddRange(new AdaptiveElement[] { title, header, information, lastDescription });

            card.Actions.Add(CreateSubmitAction("Okay I've added everthing to the file", "Done"));
            
            return card;
        }
        public static AdaptiveCard PresentationSummaryCard(IDialogContext context)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock title = new AdaptiveTextBlock()
            {
                Text = $"All right, here’s a summary of your PowerPoint design order.",
                Wrap = true,
            };

            List<AdaptiveElement> list = new List<AdaptiveElement> { title };

            if (context.UserData.TryGetValue<string>(PresentationDialog.PurposeValue, out string purposeInfo))
            {
                AdaptiveTextBlock intent = new AdaptiveTextBlock()
                {
                    Text = $"**Intent:**\n\n {purposeInfo}",
                    Wrap = true,
                };
                list.Add(intent);
            }

            if (context.UserData.TryGetValue<string>(PresentationDialog.StyleValue, out string styleInfo))
            {
                AdaptiveTextBlock style = new AdaptiveTextBlock()
                {
                    Text = $"**Style:**\n\n {styleInfo}",
                    Wrap = true,
                };
            }

            if (context.UserData.TryGetValue<string>(PresentationDialog.StyleValue, out string visualInfo))
            {
                AdaptiveTextBlock visuals = new AdaptiveTextBlock()
                {
                    Text = $"**Visuals:**\n\n {visualInfo}",
                    Wrap = true,
                };
            }

            if (context.UserData.TryGetValue<string>(PresentationDialog.ExtraInfo, out string extraInfo))
            {
                AdaptiveTextBlock comments = new AdaptiveTextBlock()
                {
                    Text = $"**Comments:**\n\n {extraInfo}",
                    Wrap = true,
                };
                list.Add(comments);
            }

            AdaptiveTextBlock lastCall = new AdaptiveTextBlock()
            {
                Text = $"Want to change anything, or should we send this job to the designer?",
                Wrap = true,
            };

            list.Add(lastCall);
            card.Body.AddRange(list);

            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Looks good, send the job",
                Data = "Looks good, send the job"
            });
            return card;
        }


        /* V2 mockups */
        public static AdaptiveCard V2PresentationIntro()
        {
            string iconCheckmark = "✅";
            string iconCross = "❌";

            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.V2Introduction,
                Wrap = true
            };

            AdaptiveTextBlock whatWeDoText = new AdaptiveTextBlock("**What we do:**");
            AdaptiveFactSet whatWeDo = new AdaptiveFactSet();
            PresentationDialogStrings.V2WhatWeDo.ForEach((string option) =>
            {
                whatWeDo.Facts.Add(new AdaptiveFact(iconCheckmark, option));
            });

            AdaptiveTextBlock whatWeDontDoText = new AdaptiveTextBlock("**What we DON'T do:**");
            AdaptiveFactSet whatWeDontDo = new AdaptiveFactSet();
            PresentationDialogStrings.V2WhatWeDontDo.ForEach((string option) =>
            {
                whatWeDontDo.Facts.Add(new AdaptiveFact(iconCross, option));
            });

            AdaptiveTextBlock letsBegin = new AdaptiveTextBlock(PresentationDialogStrings.V2Start);


            card.Body.Add(description);
            card.Body.Add(whatWeDoText);
            card.Body.Add(whatWeDo);
            card.Body.Add(whatWeDontDoText);
            card.Body.Add(whatWeDontDo);
            card.Body.Add(letsBegin);

            card.Actions = new List<AdaptiveAction>()
            {
                AdaptiveCardHelper.CreateSubmitAction(PresentationDialogStrings.V2LetsBegin),
                AdaptiveCardHelper.CreateSubmitAction(PresentationDialogStrings.V2ShowExamples)
            };
            return card;
        }

        public static AdaptiveCard V2PresentationPurpose()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = PresentationDialogStrings.V2PurposeDescription,
                Spacing = AdaptiveSpacing.ExtraLarge,
                Wrap = true
            };

            AdaptiveColumnSet options = new AdaptiveColumnSet()
            {
                Columns = {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.V2NewProject,
                        PresentationDialogStrings.V2NewProjectDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.V2ProgressReport,
                        PresentationDialogStrings.V2ProgressReportDesc
                    )
                }
            };
            AdaptiveColumnSet options2 = new AdaptiveColumnSet()
            {
                Columns = {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.V2Educate,
                        PresentationDialogStrings.V2EducateDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        PresentationDialogStrings.V2Cleanup,
                        PresentationDialogStrings.V2CleanupDec
                    )
                }
            };

            card.Body.Add(description);
            card.Body.Add(options);
            card.Body.Add(options2);
            return card;
        }

        public static IMessageActivity V2ShowExamples(IDialogContext context)
        {
            var responseMessage = context.MakeMessage();
            responseMessage.Text = PresentationDialogStrings.V2ExampleInfo;

            responseMessage.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = V2StyleExampleCard("Light, Modern, Photos", "https://www.microsoft.com/", new List<string>()
                    {
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_corporate_1.png"),
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_corporate_1.png"),
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_corporate_1.png")
                    })
                },
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = V2StyleExampleCard("Dark, Corporate, Photos", "https://www.microsoft.com/", new List<string>()
                    {
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_modern_1.png"),
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_modern_1.png"),
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_modern_1.png")
                    })
                },
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = V2StyleExampleCard("Colorful, Abstract, Shapes", "https://www.microsoft.com/", new List<string>()
                    {
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_abstract_1.png"),
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_abstract_1.png"),
                        PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_abstract_1.png")
                    })
                },
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = V2CustomDesignCard()
                }
            };

            return responseMessage;
        }

        private static AdaptiveCard V2StyleExampleCard(string style = "", string templateUrl = "", List<string> imageUrls = null)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock styleTextBlock = new AdaptiveTextBlock($"**Styles:** {style} ([preview]({templateUrl}))");
            AdaptiveColumnSet imageSet = new AdaptiveColumnSet();

            imageUrls.ForEach((url) =>
            {
                imageSet.Columns.Add(CreateAdaptiveColumnWithImage(null, url));
            });

            card.Body.Add(styleTextBlock);
            card.Body.Add(imageSet);
            card.Actions.Add(CreateSubmitAction("Make mine like this", style));

            return card;
        }

        private static AdaptiveCard V2CustomDesignCard()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock textBlock = new AdaptiveTextBlock(PresentationDialogStrings.V2SomethingDifferent);
            textBlock.Wrap = true;

            card.Body.Add(textBlock);
            card.Actions.Add(CreateSubmitAction(PresentationDialogStrings.V2LetsBegin));

            return card;
        }

    }
}