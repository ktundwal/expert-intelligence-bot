using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;
using PPTExpertConnect.Models;
using static  PPTExpertConnect.Helpers.AdaptiveCardHelper;

namespace PPTExpertConnect.Helpers
{
    public class CardBuilder
    {
        private readonly AppSettings _appSettings;

        public CardBuilder(AppSettings settings)
        {
            _appSettings = settings;
        }

        public AdaptiveCard IntroductionCard()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveContainer titleContainer = new AdaptiveContainer();
            AdaptiveTextBlock heading = new AdaptiveTextBlock()
            {
                Text = Constants.WhoIsBot,
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Large,
                Wrap = true
            };
            AdaptiveTextBlock botDescription = new AdaptiveTextBlock()
            {
                Text = Constants.BotDescription,
                Wrap = true
            };
            AdaptiveTextBlock optionDescription = new AdaptiveTextBlock()
            {
                Text = Constants.StartingOptionDescription,
                Wrap = true
            };

            titleContainer.Items.Add(heading);
            titleContainer.Items.Add(botDescription);
            titleContainer.Items.Add(optionDescription);

            card.Body.Add(titleContainer);

            AdaptiveColumnSet options = new AdaptiveColumnSet();

            options.Columns.Add(AdaptiveCardHelper.CreateAdaptiveColumnWithImage(
                Constants.WebResearch, Constants.WebResearchUrl
            ));
            options.Columns.Add(AdaptiveCardHelper.CreateAdaptiveColumnWithImage(
                Constants.PresentationDesign, Constants.PresentationDesignUrl
            ));

            card.Body.Add(options);

            return card;
        }

        public AdaptiveCard AnythingElseCard()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock lastQuestion = new AdaptiveTextBlock()
            {
                Text = Constants.LastQuestion,
                Wrap = true,
            };

            AdaptiveTextBlock tellUs = new AdaptiveTextBlock
            {
                Spacing = AdaptiveSpacing.Small,
                Text = Constants.LetUsKnow,
                Wrap = true
            };

            card.Body.Add(lastQuestion);
            card.Body.Add(tellUs);

            return card;
        }

        public AdaptiveCard ConfirmationCard(string driveItemUrl)
        {
            string okayAdded = Constants.AddedEverythingToFile;

            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock title = new AdaptiveTextBlock()
            {
                Text = $"Got it. Here's a [link]({driveItemUrl}) to an online PowerPoint template file.",
                Wrap = true,
            };

            AdaptiveTextBlock header = new AdaptiveTextBlock()
            {
                Spacing = AdaptiveSpacing.Medium,
                Text = $"In the file, you'll find prompts that ask you for...",
                Wrap = true,
            };

            AdaptiveTextBlock information = new AdaptiveTextBlock()
            {
                Spacing = AdaptiveSpacing.Large,
                Text = "\n" +
                       "\n- The images you’d like to use" +
                       "\n- Any logos, icons or other assets for the designer" +
                       "\n- Any text or outline on the pages to give the designer an idea of the structure you’d like" +
                       "\n- Instructions for key slides the designer should focus on" +
                       "\n- Links or screenshots of examples we can use as a reference" +
                       "\n- Where you’ll be presenting (conference room on projector, online meeting, etc), it may help the designer",
                Wrap = true,
            };

            AdaptiveTextBlock lastInfo = new AdaptiveTextBlock()
            {
                Spacing = AdaptiveSpacing.Large,
                Text = "Let us know when you've added these to the file, and the designer will work on it.",
                Wrap = true,
            };

            AdaptiveTextBlock lastDesc = new AdaptiveTextBlock()
            {
                Spacing = AdaptiveSpacing.Medium,
                Text = "Please don’t include any sensitive information you don’t want the freelancer to see.",
                Wrap = true,
            };

            AdaptiveSubmitAction action = CreateSubmitAction(okayAdded, okayAdded);
            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText(okayAdded);
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;
            ctaContainer.Spacing = AdaptiveSpacing.Large;

            card.Body.AddRange(new AdaptiveElement[] {title, header, information, lastInfo, lastDesc, ctaContainer});
            return card;
        }

        public AdaptiveCard SummaryCard(UserInfo userInfo)
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock title = new AdaptiveTextBlock()
            {
                Text = $"All right, here’s a summary of your PowerPoint design order.",
                Wrap = true,
            };

            List<AdaptiveElement> list = new List<AdaptiveElement> { title };

            AddToSummary("Intent", userInfo.Purpose, list);
            AddToSummary("Style", userInfo.Style, list);
            AddToSummary("Color", userInfo.Color, list);

            var visualInfo = !string.IsNullOrEmpty(userInfo.Visuals)
                ? userInfo.Visuals + ", " + userInfo.Images
                : userInfo.Images;

            AddToSummary("Visuals", visualInfo, list);
            AddToSummary("Comments", userInfo.Extra, list);
            AddToSummary(
                string.Empty,
                "Want to change anything, or should we send this job to the designer ? ",
                list);
            
            card.Body.AddRange(list);

            AdaptiveColumnSet optionSet = new AdaptiveColumnSet()
            {
                Columns =
                {
                    CreateAdaptiveColumnWithText("Looks good, send the job")
                }
            };
            optionSet.Spacing = AdaptiveSpacing.Padding;

//            if (!string.IsNullOrEmpty(userInfo.Purpose))
//            {
            optionSet.Columns.Add(CreateAdaptiveColumnWithText(Constants.ChangeSomething));
//            }

            card.Body.Add(optionSet);
            return card;
        }

        private void AddToSummary(string type, string info, List<AdaptiveElement> list)
        {
            if (!string.IsNullOrEmpty(info))
            {
                if (!string.IsNullOrEmpty(type))
                {
                    AdaptiveTextBlock item = new AdaptiveTextBlock()
                    {
                        Spacing = AdaptiveSpacing.Large,
                        Text = $"**{type}:**",
                        Wrap = true,
                    };
                    list.Add(item);
                }

                AdaptiveTextBlock block = new AdaptiveTextBlock()
                {
                    Spacing = AdaptiveSpacing.Medium,
                    Text = $"{info}",
                    Wrap = true,
                };
                list.Add(block);
            }
        }


        /* V2 mockups */
        public AdaptiveCard PresentationIntro()
        {
            const string iconCheckmark = "✅";
            const string iconCross = "❌";

            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = Constants.V2Introduction,
                Wrap = true
            };

            AdaptiveTextBlock whatWeDoText = new AdaptiveTextBlock(Constants.WhatWeDo);
            AdaptiveFactSet whatWeDo = new AdaptiveFactSet();
            Constants.V2WhatWeDo.ForEach((string option) =>
            {
                whatWeDo.Facts.Add(new AdaptiveFact(iconCheckmark, option));
            });

            AdaptiveTextBlock whatWeDontDoText = new AdaptiveTextBlock(Constants.WhatWeDontDo);
            AdaptiveFactSet whatWeDontDo = new AdaptiveFactSet();
            Constants.V2WhatWeDontDo.ForEach((string option) =>
            {
                whatWeDontDo.Facts.Add(new AdaptiveFact(iconCross, option));
            });

            AdaptiveTextBlock letsBegin = new AdaptiveTextBlock(Constants.V2Start);


            card.Body.Add(description);
            card.Body.Add(whatWeDoText);
            card.Body.Add(whatWeDo);
            card.Body.Add(whatWeDontDoText);
            card.Body.Add(whatWeDontDo);
            card.Body.Add(letsBegin);

            AdaptiveColumnSet optionSet = new AdaptiveColumnSet()
            {
                Columns =
                {
                    CreateAdaptiveColumnWithText(Constants.V2LetsBegin),
                    CreateAdaptiveColumnWithText(Constants.V2ShowExamples)
                }
            };
            optionSet.Spacing = AdaptiveSpacing.Padding;

            card.Body.Add(optionSet);
            return card;
        }

        public AdaptiveCard V2PresentationPurpose()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = Constants.V2PurposeDescription,
                Spacing = AdaptiveSpacing.ExtraLarge,
                Wrap = true
            };

            AdaptiveColumnSet options = new AdaptiveColumnSet()
            {
                Columns =
                {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        Constants.V2NewProject,
                        Constants.V2NewProjectDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        Constants.V2ProgressReport,
                        Constants.V2ProgressReportDesc
                    )
                }
            };
            AdaptiveColumnSet options2 = new AdaptiveColumnSet()
            {
                Columns =
                {
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        Constants.V2Educate,
                        Constants.V2EducateDesc
                    ),
                    AdaptiveCardHelper.CreateAdaptiveColumnWithText(
                        Constants.V2Cleanup,
                        Constants.V2CleanupDec
                    )
                }
            };

            card.Body.Add(description);
            card.Body.Add(options);
            card.Body.Add(options2);
            return card;
        }

        public AdaptiveCard V2ColorVariations()
        {
            AdaptiveCard card = new AdaptiveCard();
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = Constants.Variations,
                Wrap = true
            });

            AdaptiveColumnSet optionSetA = new AdaptiveColumnSet()
            {
                Columns =
                {
                    CreateAdaptiveColumnWithImagePreviewBelow(
                        Constants.ColorDark,
                        _appSettings.GetImageUrlFromLocation(@"template_dark.png")),
                    CreateAdaptiveColumnWithImagePreviewBelow(
                        Constants.ColorLight,
                        _appSettings.GetImageUrlFromLocation(@"template_light.png"))
                }
            };

            AdaptiveColumnSet optionSetB = new AdaptiveColumnSet()
            {
                Columns =
                {
                    CreateAdaptiveColumnWithImagePreviewBelow(
                        Constants.Colorful,
                        _appSettings.GetImageUrlFromLocation(@"template_colorful.png")),
                    CreateAdaptiveColumnWithText(
                        Constants.NoneOfThese,
                        Constants.DescribeWhatIWant)
                }
            };

            card.Body.Add(optionSetA);
            card.Body.Add(optionSetB);
            
            return card;
        }

        public AdaptiveCard V2ShowExamples()
        {
            AdaptiveCard card = new AdaptiveCard();
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = Constants.V2ExampleInfo,
                Wrap = true
            });

            card.Body.Add(V2StyleExampleContainer("Light, Modern, Photos", "https://www.microsoft.com/", new List<string>()
            {
                _appSettings.GetImageUrlFromLocation(@"example_light_1.png"),
                _appSettings.GetImageUrlFromLocation(@"example_light_2.png"),
                _appSettings.GetImageUrlFromLocation(@"example_light_3.png")
            }));

            card.Body.Add(V2StyleExampleContainer("Dark, Corporate, Photos", "https://www.microsoft.com/", new List<string>()
            {
                _appSettings.GetImageUrlFromLocation(@"example_dark_1.png"),
                _appSettings.GetImageUrlFromLocation(@"example_dark_2.png"),
                _appSettings.GetImageUrlFromLocation(@"example_dark_3.png")
            }));

            card.Body.Add(V2StyleExampleContainer("Colorful, Abstract, Shapes", "https://www.microsoft.com/", new List<string>()
            {
                _appSettings.GetImageUrlFromLocation(@"example_colorful_1.png"),
                _appSettings.GetImageUrlFromLocation(@"example_colorful_2.png"),
                _appSettings.GetImageUrlFromLocation(@"example_colorful_3.png")
            }));

            card.Body.Add(V2CustomDesignContainer());
           
            return card;
        }

        public AdaptiveCard V2IllustrationsCard()
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock description = new AdaptiveTextBlock()
            {
                Text = Constants.Variations,
                Wrap = true,
            };

            var styleA = AdaptiveCardHelper.CreateAdaptiveColumnWithImage("Photos",
                _appSettings.GetImageUrlFromLocation(@"graphic_photos.png"), true, true);
            var styleB = AdaptiveCardHelper.CreateAdaptiveColumnWithImage("Illustrations",
                _appSettings.GetImageUrlFromLocation(@"graphic_illustrations.png"), true, true);
            var styleC = AdaptiveCardHelper.CreateAdaptiveColumnWithImage("Shapes",
                _appSettings.GetImageUrlFromLocation(@"graphic_shapes.png"), true, true);

            AdaptiveColumnSet optionSetA = new AdaptiveColumnSet()
            {
                Columns =
                {
                    styleA, styleB
                }
            };

            AdaptiveColumnSet optionSetB = new AdaptiveColumnSet()
            {
                Columns =
                {
                    styleC,
                    CreateAdaptiveColumnWithText(
                        Constants.NoneOfThese,
                        Constants.DescribeWhatIWant)
                },
                Spacing = AdaptiveSpacing.Padding
            };


            card.Body.Add(description);
            card.Body.Add(optionSetA);
            card.Body.Add(optionSetB);

            return card;
        }

        public AdaptiveCard V2ImageOptions()
        {
            AdaptiveCard card = new AdaptiveCard();
            
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = Constants.ImagesDesc,
                Wrap = true
            });

            AdaptiveColumnSet options = new AdaptiveColumnSet
            {
                Spacing = AdaptiveSpacing.ExtraLarge,
                Columns =
                {
                    CreateAdaptiveColumnWithText(Constants.NewImages),
                    CreateAdaptiveColumnWithText(Constants.OwnImages)
                }
            };

            card.Body.Add(options);

            card.Body.Add(new AdaptiveTextBlock
            {
                Spacing = AdaptiveSpacing.ExtraLarge,
                Text = Constants.WhyWeAsking,
                Wrap = true
            });

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = Constants.ImagesDisclaimer,
                Size = AdaptiveTextSize.Small,
                IsSubtle = true,
                Wrap = true,
                Spacing = AdaptiveSpacing.Large
            });

            return card;
        }

        public AdaptiveCard V2VsoTicketCard(int projectNumber, string inviteUrl)
        {
            AdaptiveCard card = new AdaptiveCard();

            AdaptiveTextBlock projectTextBlock = new AdaptiveTextBlock
            {
                Text = $"All set! **Your project number is {projectNumber}.**",
                Wrap = true

            };

            AdaptiveTextBlock slaTextBlock = new AdaptiveTextBlock
            {
                Spacing = AdaptiveSpacing.Large,
                Text = "You'll hear back from us in 2 business days. " +
                       "The freelancer will send you their work, and you can give feedback for revisions.",
                Wrap = true

            };

            AdaptiveImage cloudImage = new AdaptiveImage()
            {
                Spacing = AdaptiveSpacing.Padding,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                AltText = "ProjectSentImage",
                UrlString = _appSettings.GetImageUrlFromLocation("confirmation_job_created.png")
            };

            AdaptiveTextBlock invitationTextBlock = new AdaptiveTextBlock
            {
                Spacing = AdaptiveSpacing.Medium,
                Text = "Invite your colleagues to try this service for free by sending them this link:",
                Wrap = true
            };

            AdaptiveTextBlock invitationTextLink = new AdaptiveTextBlock
            {
                Spacing = AdaptiveSpacing.None,
                Text = $"[{inviteUrl}]({inviteUrl})",
                Wrap = true
            };

            card.Body.Add(projectTextBlock);
            card.Body.Add(slaTextBlock);
            card.Body.Add(cloudImage);
            card.Body.Add(invitationTextBlock);
            card.Body.Add(invitationTextLink);
            return card;
        }

        public AdaptiveCard V2PresentationResponse(string user)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock responseTextBlock =
                new AdaptiveTextBlock( $"Hi {user}, your presentation is ready for review. Let us know if you have any comments or add them right in the PowerPoint file.") {Wrap = true};
            AdaptiveTextBlock slaTextBlock =
                new AdaptiveTextBlock("If we don't hear back from in 48 hours, we'll assume you're all set and we'll close this project.") {Wrap = true};

            card.Body.Add(responseTextBlock);
            card.Body.Add(slaTextBlock);

            AdaptiveColumnSet optionsSet = new AdaptiveColumnSet()
            {
                Columns =
                {
                    CreateAdaptiveColumnWithText(Constants.Complete),
                    CreateAdaptiveColumnWithText(Constants.Revision),
                }
            };
            card.Body.Add(optionsSet);
            return card;
        }

        //public static AdaptiveCard V2AllOptionsToChange(IDialogContext context)
        //{
        //    AdaptiveCard card = new AdaptiveCard();

        //    return null;
        //}

        public AdaptiveCard V2Ratings()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock ratingTextBlock =
                new AdaptiveTextBlock(
                        "Thanks for letting us help you, we hope your presentation goes well! Please rate your experience.")
                    {Wrap = true};
            AdaptiveColumnSet stars = new AdaptiveColumnSet();

            foreach (var star in Enumerable.Range(1, 5))
            {
                stars.Columns.Add(CreateAdaptiveColumnWithImage(star.ToString(),
                    _appSettings.GetImageUrlFromLocation(@"star_rating_graphic_large.png")));
            }

            card.Body.Add(ratingTextBlock);
            card.Body.Add(stars);
            return card;
        }

        public AdaptiveCard V2Learning(string text, string articleLink, string articleImage, string articleTitle)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock learningBlock = new AdaptiveTextBlock(text) {Wrap = true};

            AdaptiveColumnSet learningSet = new AdaptiveColumnSet();
            learningSet.Columns.Add(CreateAdaptiveColumnWithImage("",
                articleImage ?? _appSettings.GetImageUrlFromLocation(@"StyleOptions/style_select_dark_modern_2.png")));
            AdaptiveColumn learningDescriptions = new AdaptiveColumn()
            {
                Width = "2",
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock("Course") {IsSubtle = true, Wrap = true},
                    new AdaptiveTextBlock($"**[{articleTitle}]({articleLink})**") {Wrap = true}
                }
            };
            learningSet.Columns.Add(learningDescriptions);

            card.Body.Add(learningBlock);
            card.Body.Add(learningSet);

            return card;
        }

        public AdaptiveCard V2Feedback(bool toAddress, bool badRating)
        {
            return new AdaptiveCard()
            {
                Body = new List<AdaptiveElement>
                {
                    toAddress ? new AdaptiveTextBlock(
                        "Got it. We will address your feedback and get back to you shortly.") {Wrap = true} :
                    badRating ? new AdaptiveTextBlock(
                        "Dang, really? Please tell us why you gave us this rating, so we can improve.") {Wrap = true} :
                    new AdaptiveTextBlock("Any other feedback to help us improve the process?") {Wrap = true}
                }
            };
        }

        public AdaptiveCard V2AskForRevisionChanges()
        {
            return new AdaptiveCard()
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock("Let us know what you would like to see changed or updated? We will address your feedback and get back to you shortly.") {Wrap = true}
                }
            };
        }

        private AdaptiveCard V2StyleExampleCard(string style = "", string templateUrl = "",
            List<string> imageUrls = null)
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock styleTextBlock = new AdaptiveTextBlock($"**Styles:** {style} ([preview]({templateUrl}))");
            AdaptiveColumnSet imageSet = new AdaptiveColumnSet();

            imageUrls.ForEach((url) => { imageSet.Columns.Add(CreateAdaptiveColumnWithImage(null, url)); });

            card.Body.Add(styleTextBlock);
            card.Body.Add(imageSet);
            card.Actions.Add(CreateSubmitAction("Make mine like this", style));

            return card;
        }
        private AdaptiveContainer V2StyleExampleContainer (string style = "", string templateUrl = "",
            List<string> imageUrls = null)
        {
            AdaptiveContainer card = new AdaptiveContainer()
            {
                //Separator = true,
                Spacing = AdaptiveSpacing.ExtraLarge
            };

            AdaptiveTextBlock styleTextBlock = new AdaptiveTextBlock($"**Styles:** {style} ([preview]({templateUrl}))");
            AdaptiveColumnSet imageSet = new AdaptiveColumnSet();

            imageUrls.ForEach((url) => { imageSet.Columns.Add(CreateAdaptiveColumnWithImage(null, url)); });


            AdaptiveSubmitAction action = CreateSubmitAction("Make mine like this", style);
            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText("Make mine like this");
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;
            ctaContainer.Spacing = AdaptiveSpacing.Large;

            card.Items.Add(styleTextBlock);
            card.Items.Add(imageSet);
            card.Items.Add(ctaContainer);

            return card;
        }

        private AdaptiveCard V2CustomDesignCard()
        {
            AdaptiveCard card = new AdaptiveCard();
            AdaptiveTextBlock textBlock = new AdaptiveTextBlock(Constants.V2SomethingDifferent);
            textBlock.Wrap = true;


            card.Body.Add(textBlock);
            card.Actions.Add(CreateSubmitAction(Constants.V2LetsBegin));

            return card;
        }
        private AdaptiveContainer V2CustomDesignContainer()
        {
            var createBrief = "Create a brief";

            AdaptiveContainer card = new AdaptiveContainer()
            {
                Separator = true,
                Spacing = AdaptiveSpacing.ExtraLarge
            };
            AdaptiveTextBlock textBlock = new AdaptiveTextBlock(Constants.V2SomethingDifferent);
            textBlock.Wrap = true;


            AdaptiveSubmitAction action = CreateSubmitAction(createBrief, Constants.V2LetsBegin);
            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText(createBrief);
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;
            ctaContainer.Spacing = AdaptiveSpacing.Large;

            card.Items.Add(textBlock);
            card.Items.Add(ctaContainer);

            return card;
        }
    }
}