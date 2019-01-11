using System;
using System.Collections.Generic;
using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace PPTExpertConnect.Helpers
{
    public class AdaptiveCardHelper
    { 
        public static AdaptiveColumn CreateAdaptiveColumnWithText(string ctaHeader, string ctaText="")
        {
            AdaptiveColumn column = new AdaptiveColumn();

            AdaptiveSubmitAction action = CreateSubmitAction(ctaHeader, ctaHeader);

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

            if (string.IsNullOrEmpty(ctaText))
            {
                column.Items.Add(imageContainer);
                return column;
            }

            AdaptiveSubmitAction action = CreateSubmitAction(ctaText, ctaText);

            AdaptiveContainer ctaContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText(ctaText, string.Empty);
            ctaContainer.SelectAction = action;
            ctaContainer.Style = AdaptiveContainerStyle.Emphasis;

            AdaptiveContainer emptyContainer = AdaptiveCardHelper.CreateAdaptiveContainerWithText();

            if (imageInsideAction)
            {
                if (imageBelowText)
                {
                    ctaContainer.Items.Add(imageContainer);
                    ctaContainer.Items.Add(emptyContainer);
                    column.Items.Add(ctaContainer);
                }
                else
                {
                    ctaContainer.Items.Insert(0, emptyContainer);
                    ctaContainer.Items.Insert(1, imageContainer);
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

        public static AdaptiveColumn CreateAdaptiveColumnWithImagePreviewBelow(string ctaText, string imageUrl)
        {
            AdaptiveColumn column = CreateAdaptiveColumnWithImage(ctaText, imageUrl, true, true);

            AdaptiveTextBlock previewContainer = new AdaptiveTextBlock($"[Preview]({ imageUrl})"){
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                Spacing = AdaptiveSpacing.None
            };

            column.Items.Add(previewContainer);
            return column;
        }

        public static (AdaptiveColumn column, AdaptiveAction preview) CreateAdaptiveColumnWithImagePreview(string ctaText, string imageUrl)
        {
            AdaptiveColumn column = CreateAdaptiveColumnWithImage(ctaText, imageUrl, true, true);

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

            AdaptiveAction preview = new AdaptiveShowCardAction()
            {
                Title = $"Preview",
                Card = previewCard,
                AdditionalProperties = new Dictionary<string, object>()
            };
            preview.AdditionalProperties.Add("ShowCardActionMode", "popup");

            //column.Items.Add(previewContainer);
            return (column, preview);
        }

        public static AdaptiveContainer CreateAdaptiveContainerWithText(string ctaHeader = "", string ctaText = "")
        {
            AdaptiveContainer container = new AdaptiveContainer();

            if (ctaHeader != string.Empty)
            {
                AdaptiveTextBlock headerBlock = new AdaptiveTextBlock()
                {
                    Text = $"{ctaHeader}",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Bolder,
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
                Url = new Uri(imageUrl, UriKind.RelativeOrAbsolute),
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center
            };

            return new AdaptiveContainer()
            {
                Items = { image }
            };
        }

        /// <summary>
        ///  Creates a clickble action for the card Action. 
        /// </summary>
        /// <param name="displayText">Text to display on the button</param>
        /// <param name="messageBack">Text to be returned to bot, if null the displayText will be used</param>
        /// <returns></returns>
        public static AdaptiveSubmitAction CreateSubmitAction(string displayText, string messageBack = "")
        {
            if (string.IsNullOrEmpty(displayText))
            {
                throw new SystemException("Incorrect parameters entered");
            }

            string dPText = displayText;
            string mBText = !string.IsNullOrEmpty(messageBack) ? messageBack : displayText;

            return new AdaptiveSubmitAction()
            {
                Title = dPText ?? mBText,
                Data = new ResponseObject()
                {
                    msteams = new CardAction()
                    {
                        Type = ActionTypes.MessageBack,
                        Text = mBText,
                        DisplayText = mBText
                    }
                }
            };
            //return new AdaptiveSubmitAction()
            //{
            //    Title = dPText,
            //    //                Data = dPText
            //    Data = new CardAction(
            //        ActionTypes.ImBack, mBText, null, mBText)
            //};
        }

        public class ResponseObject
        {
            public CardAction msteams = null;
        }
    }
}
