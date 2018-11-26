using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Teams.TemplateBotCSharp;

namespace Microsoft.Office.EIBot.Service.dialogs.examples.moderate
{
    [Serializable]
    public class DeadlinePrompt : Prompt<IEnumerable<DateTime>, DateTime>
    {
        private const string Option1 = "2 days from now";
        private const string Option2 = "4 days from now";
        private const string Option3 = "7 days from now";

        public static readonly string DeliveryPromptMessage = "Got it. When do you need this research to be completed? We'll need at least 48 hrs.\n\nHere are some valid options. You can type 1, 2 or 3 or some other time:\n\n" +
                                                              $"1. {Option1}\n\n" +
                                                              $"2. {Option2}\n\n" +
                                                              $"3. {Option3}";

        public const string PastValueErrorMessage =
            "You have requested $moment$.\n\nI'm sorry, but I need at least 48 hours to complete this work.\n\nWhat other time suits you best?";
        private readonly int _minHours;
        private readonly string _culture;

        public DeadlinePrompt(int minHours, string culture) : base(new PromptOptions<DateTime>(DeliveryPromptMessage, attempts: 2))
        {
            _culture = culture;
            _minHours = minHours;
        }

        protected override bool TryParse(IMessageActivity message, out IEnumerable<DateTime> result)
        {
            var properties = new Dictionary<string, string> { { "from", message.From.Name }, { "textFromUser", message.Text } };
            string textToParse = ConvertOptionToTimeString(message);

            try
            {
                var extraction = ValidateAndExtract(textToParse, _culture);
                if (!extraction.IsValid)
                {
                    promptOptions.DefaultRetry = extraction.ErrorMessage;
                }

                result = extraction.Values;

                properties.Add("result", string.Join(", ", result.Select(r => r.ToString(_culture))));
                properties.Add("IsValid", extraction.IsValid.ToString());
                var eventName = extraction.IsValid ? "DeadlinePrompt.TryParse.Success" : "DeadlinePrompt.TryParse.Fail";
                WebApiConfig.TelemetryClient.TrackEvent(eventName, properties);
                return extraction.IsValid;
            }
            catch (System.Exception e)
            {
                WebApiConfig.TelemetryClient.TrackException(e, properties);

                // don't throw, just return false
                promptOptions.DefaultRetry = $"Sorry, I couldn't understand {message.Text}. Please try again.";
                result = new DateTime[0];
                return false;
            }
        }

        private static string ConvertOptionToTimeString(IMessageActivity message)
        {
            string textToParse;
            var cleanedText = message.Text.Trim().Replace("'", "");
            switch (cleanedText)
            {
                case "1":
                    textToParse = Option1;
                    break;
                case "2":
                    textToParse = Option2;
                    break;
                case "3":
                    textToParse = Option3;
                    break;
                default:
                    textToParse = message.Text;
                    break;
            }

            return textToParse;
        }

        private Extraction ValidateAndExtract(string input, string culture)
        {
            // Get DateTime for the specified culture
            var results = DateTimeRecognizer.RecognizeDateTime(input, culture);

            // Check there are valid results
            if (results.Count > 0 && results.First().TypeName.StartsWith("datetimeV2"))
            {
                // The DateTime model can return several resolution types (https://github.com/Microsoft/Recognizers-Text/blob/master/.NET/Microsoft.Recognizers.Text.DateTime/Constants.cs#L7-L14)
                // We only care for those with a date, date and time, or date time period:
                // date, daterange, datetime, datetimerange

                var first = results.First();
                var resolutionValues = (IList<Dictionary<string, string>>)first.Resolution["values"];

                var subType = first.TypeName.Split('.').Last();
                if (subType.Contains("date") && !subType.Contains("range"))
                {
                    // a date (or date & time) or multiple
                    var moment = resolutionValues.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();
                    if (IsFuture(moment))
                    {
                        // a future moment, valid!
                        return new Extraction
                        {
                            IsValid = true,
                            Values = new[] { moment }
                        };
                    }

                    // a past moment
                    return new Extraction
                    {
                        IsValid = false,
                        Values = new[] { moment },
                        ErrorMessage = PastValueErrorMessage.Replace("$moment$", MomentOrRangeToString(moment))
                    };
                }
                else if (subType.Contains("date") && subType.Contains("range"))
                {
                    // range
                    var from = DateTime.Parse(resolutionValues.First()["start"]);
                    var to = DateTime.Parse(resolutionValues.First()["end"]);
                    if (/*IsFuture(from) && */IsFuture(to))
                    {
                        // future
                        return new Extraction
                        {
                            IsValid = true,
                            Values = new[] { from, to }
                        };
                    }

                    var values = new[] { from, to };
                    return new Extraction
                    {
                        IsValid = false,
                        Values = values,
                        ErrorMessage = PastValueErrorMessage.Replace("$moment$", MomentOrRangeToString(values))
                    };
                }
            }

            return new Extraction
            {
                IsValid = false,
                Values = Enumerable.Empty<DateTime>(),
                ErrorMessage = $"I'm sorry, '{input}' doesn't seem to be a valid delivery date and time"
            };
        }

        private bool IsFuture(DateTime date)
        {
            // at least one hour
            return date > DateTime.UtcNow.AddHours(_minHours);
        }

        private string MomentOrRangeToString(IEnumerable<DateTime> moments, string momentPrefix = "on ")
        {
            if (moments.Count() == 1)
            {
                return MomentOrRangeToString(moments.First(), momentPrefix);
            }

            return "from " + string.Join(" to ", moments.Select(m => MomentOrRangeToString(m, "")));
        }

        private string MomentOrRangeToString(DateTime moment, string momentPrefix = "on ")
        {
            return momentPrefix + moment.ToString();
        }

        private class Extraction
        {
            public bool IsValid { get; set; }

            public IEnumerable<DateTime> Values { get; set; }

            public string ErrorMessage { get; set; }
        }
    }
}