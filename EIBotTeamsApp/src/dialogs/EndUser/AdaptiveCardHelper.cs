using System;
using AdaptiveCards;
using Microsoft.Bot.Connector;

namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public class AdaptiveCardHelper
    {
        public static AdaptiveColumn CreateAdaptiveColumnWithText(string ctaHeader, string ctaText)
        {
            AdaptiveColumn column = new AdaptiveColumn();
            AdaptiveSubmitAction action = new AdaptiveSubmitAction()
            {
                Title = ctaHeader,
                Data = new ResponseObject()
                {
                    msteams = new CardAction(ActionTypes.MessageBack, null, null, null, ctaHeader, ctaHeader)
                }
            };

            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText(ctaHeader, ctaText);
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;

            column.Items.Add(ctaContainer);
            return column;
        }

        public static AdaptiveColumn CreateAdaptiveColumnWithImage(string ctaText, string imageUrl, bool imageInsideAction = false, bool imageBelowText = false)
        {
            AdaptiveColumn column = new AdaptiveColumn();

            AdaptiveContainer imageContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithImage(imageUrl);

            if (ctaText == string.Empty)
            {
                column.Items.Add(imageContainer);
                return column;
            }

            var data = new ResponseObject()
            {
                msteams = new CardAction()
                {
                    Type = ActionTypes.MessageBack,
                    Text = ctaText,
                    DisplayText = ctaText
                }
            };

            AdaptiveSubmitAction action = new AdaptiveSubmitAction()
            {
                Title = ctaText,
                Data = data
            };
            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText(string.Empty, ctaText);
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;

            if (imageInsideAction)
            {
                if (imageBelowText)
                {
                    ctaContainer.Items.Add(imageContainer);
                    column.Items.Add(ctaContainer);
                }
                else
                {
                    ctaContainer.Items.Insert(0, imageContainer);
                    column.Items.Add(ctaContainer);
                }
            }
            else
            {
                column.Items.Add(imageContainer);
                column.Items.Add(ctaContainer);
            }

            return column;
        }

        public static AdaptiveColumn CreateAdaptiveColumnWithImagePreview(string ctaText, string imageUrl)
        {
            AdaptiveColumn column = CreateAdaptiveColumnWithImage(ctaText, imageUrl, true, true);
            AdaptiveContainer previewContainer = CreateAdaptiveContainerWithText(string.Empty, "Preview");

            AdaptiveCard previewCard = new AdaptiveCard();
            AdaptiveColumnSet previewImages = new AdaptiveColumnSet()
            {
                Columns =
                {
                    CreateAdaptiveColumnWithImage(string.Empty, imageUrl),
                    CreateAdaptiveColumnWithImage(string.Empty, imageUrl),
                    CreateAdaptiveColumnWithImage(string.Empty, imageUrl),
                }
            };

            previewCard.Body.Add(previewImages);

            previewContainer.SelectAction = new AdaptiveShowCardAction()
            {
                Title = $"Preview {ctaText}",
                Card = previewCard
            };
            
            column.Items.Add(previewContainer);
            return column;
        }

        public static AdaptiveContainer CreateAdaptiveContainerWithText(string ctaHeader = "", string ctaText = "")
        {
            AdaptiveContainer container = new AdaptiveContainer();

            if (ctaHeader != string.Empty)
            {
                AdaptiveTextBlock headerBlock = new AdaptiveTextBlock()
                {
                    Text = $"**{ctaHeader}**",
                    Size = AdaptiveTextSize.Large,
                    //Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Large,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                };
                container.Items.Add(headerBlock);
            }

            if (ctaText != string.Empty)
            {
                AdaptiveTextBlock textBlock = new AdaptiveTextBlock()
                {
                    Text = ctaText,
                    Size = AdaptiveTextSize.Small,
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                };
                container.Items.Add(textBlock);
            }

            return container;
        }

        public static AdaptiveContainer CreateAdaptiveContainerWithImage(string imageUrl)
        {
            AdaptiveImage image = new AdaptiveImage()
            {
                Url = new Uri(imageUrl, UriKind.Absolute)
            };

            return new AdaptiveContainer()
            {
                Items = { image }
            };
        }

        public class ResponseObject
        {
            public CardAction msteams = null;
        }
    }
}