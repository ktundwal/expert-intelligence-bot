using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using System.Linq;
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

            card.Body.Add(last);

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

            card.Actions.Add(CreateSubmitAction("Okay I've added everything to the file", "Okay I've added everything to the file"));
            
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

            if (context.UserData.TryGetValue<string>(UserData.Purpose, out string purposeInfo))
            {
                AdaptiveTextBlock intent = new AdaptiveTextBlock()
                {
                    Text = $"**Intent:**\n\n {purposeInfo}",
                    Wrap = true,
                };
                list.Add(intent);
            }

            if (context.UserData.TryGetValue<string>(UserData.Style, out string styleInfo))
            {
                AdaptiveTextBlock style = new AdaptiveTextBlock()
                {
                    Text = $"**Style:**\n\n {styleInfo}",
                    Wrap = true,
                };
                list.Add(style);
            }

            if (context.UserData.TryGetValue<string>(UserData.Visuals, out string visualInfo))
            {
                AdaptiveTextBlock visuals = new AdaptiveTextBlock()
                {
                    Text = $"**Visuals:**\n\n {visualInfo}",
                    Wrap = true,
                };
            }

            if (context.UserData.TryGetValue<string>(UserData.Extra, out string extraInfo))
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

            card.Actions.Add(CreateSubmitAction("Looks good, send the job","Looks good"));
            if (!string.IsNullOrEmpty(purposeInfo)) // This is to understand we went into a customizable branch, and we didn't pick from the examples.
            {
                card.Actions.Add(CreateSubmitAction("I want to change something"));
            }
            return card;
        }


        /* V2 mockups */
        public static AdaptiveCard PresentationIntro()
        {
            const string iconCheckmark = "✅";
            const string iconCross = "❌";

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

        public static AdaptiveCard V2IllustrationsCard()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = "Which of these variations do you like best?",
                Wrap = true,
            };
            
            var styleA = AdaptiveCardHelper.CreateAdaptiveColumnWithImage("Photos", PresentationDialogStrings.GetImageUrl(@"StyleOptions/image_select_photos_1.png"), true, true);
            var styleB = AdaptiveCardHelper.CreateAdaptiveColumnWithImage("Illustrations", PresentationDialogStrings.GetImageUrl(@"StyleOptions/image_select_illustrations_1.png"), true, true);
            var styleC = AdaptiveCardHelper.CreateAdaptiveColumnWithImage("Shapes", PresentationDialogStrings.GetImageUrl(@"StyleOptions/image_select_typographic_1.png"), true, true);

            AdaptiveColumnSet styleOptions = new AdaptiveColumnSet()
            {
                Columns = {
                    styleA,
                    styleB,
                    styleC,
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText("None of these" , "Let me describe what I want.")
                }
            };

            card.Body.Add(description);
            card.Body.Add(styleOptions);

            return card;
        }

        public static AdaptiveCard V2VsoTicketCard(int projectNumber, string inviteUrl)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock projectTextBlock = new AdaptiveTextBlock($"All set! **Your project number is {projectNumber}.**") { Wrap = true };
            AdaptiveTextBlock slaTextBlock = new AdaptiveTextBlock(
                "You'll hear back from us in 2 business days. The freelancer will send you their work, and you can give feedback for revisions.")
                { Wrap = true };

            AdaptiveTextBlock invitationTextBlock = new AdaptiveTextBlock("Invite your colleagues to try this service for free by sending them this link: \n" +
                                                                          $"[{inviteUrl}]({inviteUrl})")
                { Wrap = true };

            AdaptiveImage cloudImage = new AdaptiveImage()
            {
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                AltText = "ProjectSentImage",
                UrlString = System.Configuration.ConfigurationManager.AppSettings["BaseUri"] + @"public/assets/ppt/presentation_design_icon.png"
            };

            card.Body.Add(projectTextBlock);
            card.Body.Add(slaTextBlock);
            card.Body.Add(cloudImage);
            card.Body.Add(invitationTextBlock);
            return card;
        }

        public static AdaptiveCard V2PresentationResponse(string user)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock responseTextBlock = new AdaptiveTextBlock($"Hi {user}, your presentation is ready for review. Let us know if you have any comments or add them right in the PowerPoint file.") { Wrap = true };
            AdaptiveTextBlock slaTextBlock = new AdaptiveTextBlock("If we don't hear back from in 48 hours, we'll assume you're all set and we'll close this project.") { Wrap = true };

            card.Body.Add(responseTextBlock);
            card.Body.Add(slaTextBlock);

            card.Actions.Add(CreateSubmitAction("This is complete"));
            card.Actions.Add(CreateSubmitAction("I want a free revision"));
            return card;
        }

        public static AdaptiveCard V2AllOptionsToChange(IDialogContext context)
        {
            AdaptiveCard card = new AdaptiveCard();

            return null;
        }

        public static AdaptiveCard V2Ratings()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock ratingTextBlock = new AdaptiveTextBlock("Thanks for letting us help you, we hope your presentation goes well! Please rate your experience.") { Wrap = true };
            AdaptiveColumnSet stars = new AdaptiveColumnSet();

            foreach (var star in Enumerable.Range(1, 5))
            {
                stars.Columns.Add(CreateAdaptiveColumnWithImage(star.ToString(), PresentationDialogStrings.GetImageUrl(@"star_rating_graphic_large.png")));
            }

            card.Body.Add(ratingTextBlock);
            card.Body.Add(stars);
            return card;
        }

        public static AdaptiveCard V2Learning(string text, string articleLink, string articleImage, string articleTitle)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock learningBlock = new AdaptiveTextBlock(text) { Wrap = true };

            AdaptiveColumnSet learningSet = new AdaptiveColumnSet();
            learningSet.Columns.Add(CreateAdaptiveColumnWithImage("", articleImage ?? PresentationDialogStrings.GetImageUrl(@"StyleOptions/style_select_dark_modern_2.png")));
            AdaptiveColumn learningDescriptions = new AdaptiveColumn()
            {
                Width =  "2",
                Items = new List < AdaptiveElement >
                {
                    new AdaptiveTextBlock("Course") {IsSubtle = true, Wrap = true},
                    new AdaptiveTextBlock($"**[{articleTitle}]({articleLink})**")  {Wrap = true}
                }
            };
            learningSet.Columns.Add(learningDescriptions);

            card.Body.Add(learningBlock);
            card.Body.Add(learningSet);

            return card;
        }

        public static AdaptiveCard V2Feedback(bool toAddress, bool badRating)
        {
            return new AdaptiveCard()
            {
                Body = new List<AdaptiveElement>
                {
                    toAddress ? new AdaptiveTextBlock("Got it. We will address your feedback and get back to you shortly.") {Wrap = true} : 
                    badRating ? new AdaptiveTextBlock("Dang, really? Please tell us why you gave us this rating, so we can improve.")  {Wrap = true} : 
                    new AdaptiveTextBlock("Any other feedback to help us improve the process?")  {Wrap = true}
                }
            };
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