using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ServerlessDemo.Monitoring
{
    public class MonitoringFunction
    {

        /// <summary>
        /// A simple function that takes a string for URL and pings it. Obviously, while there are monitoring tools that do this, they do cost something. 
        /// Why is this a good choice for a serverless? 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string MonitoringFunctionHandler(string input, ILambdaContext context)
        {
            string result = "";
            bool pingResult = false;
            Ping pingClient = null;

            if (!this.IsValidUrlOrIp(input))
            {
                result = "This is not a valid URL or IP address.";
            }
            else
            {
                try
                {
                    pingClient = new Ping();
                    PingReply reply = pingClient.Send(input);
                    pingResult = reply?.Status == IPStatus.Success;
                }
                catch (PingException)
                {

                }
                finally
                {
                    if (pingClient != null)
                    {
                        pingClient.Dispose();
                    }
                }

                result = pingResult ? "Site responded successfully." : "Could not get a response from this site, it must be down, panic!!";
            }

            return result;
        }

        private bool IsValidUrlOrIp(string input)
        {
            Regex regex = new Regex("^(http:\\/\\/www\\.|https:\\/\\/www\\.|http:\\/\\/|https:\\/\\/)?[a-z0-9]+([\\-\\.]{1}[a-z0-9]+)*\\.[a-z]{2,5}(:[0-9]{1,5})?(\\/.*)?|^((http:\\/\\/www\\.|https:\\/\\/www\\.|http:\\/\\/|https:\\/\\/)?([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");

            return regex.IsMatch(input);
        }
    }
}
