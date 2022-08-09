using SlackAPI;
using System.Text.Json;

namespace SlackApiBot.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception that's thrown when there's an error in the response from the Slack API.
    /// </summary>
    public class SlackApiException : Exception
    {
        /// <summary>
        /// The response error message returned from the Slack API.
        /// </summary>
        public string ResponseErrorMessage { get; private set; }

        /// <summary>
        /// The response object returned from the Slack API.
        /// </summary>
        public Response Response { get; private set; }

        public SlackApiException(string message, string responseError, Response response) : base(message)
        {
            ResponseErrorMessage = responseError;
            Response = response;
        }

        public override string ToString()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true, // Must be true in order to serialize fields that are not declared as properties with getters and setters.
                WriteIndented = true
            };

            return $"{base.ToString()}\nResponse Error Message: {ResponseErrorMessage}\nResponse: {JsonSerializer.Serialize(Response, jsonOptions)}";
        }
    }
}
