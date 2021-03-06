﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ElsmartKIDGenerator
{
    internal class AuthorizationHeaderHandler : DelegatingHandler
    {
        public AuthorizationHeaderHandler()
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IEnumerable<string> apiKeyHeaderValues = null;
            if (request.Headers.TryGetValues("elsmartkidApiKey", out apiKeyHeaderValues))
            {
                var apiKeyHeaderValue = apiKeyHeaderValues.First();
                
                if(apiKeyHeaderValue == "12345")
                {
                    setPrincipal();
                }
            }
            else if (!request.GetQueryNameValuePairs().Equals(null))
            {
                var keys = request.GetQueryNameValuePairs();
                foreach (var item in keys)
                {
                    if (item.Key.Equals("elsmartkidApiKey") && item.Value.Equals("12345"))
                    {
                        setPrincipal();
                    }
                }
            }

            return base.SendAsync(request, cancellationToken);
        }

        private void setPrincipal()
        {
            var usernameClaim = new Claim(ClaimTypes.Name, "validatedUser");
            var identity = new ClaimsIdentity(new[] { usernameClaim }, "ApiKey");
            var principal = new ClaimsPrincipal(identity);

            Thread.CurrentPrincipal = principal;
            HttpContext.Current.User = principal;
        }
    }
}