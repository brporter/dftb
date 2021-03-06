using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace dftbsvc
{
    public static class JwtBearerConfiguration
    {
        const string InvalidTokenError = "invalid_token";
        const string InvalidTokenErrorDescription = "Invalid access token.";
        const string TokenExpiredHeader = "x-token-expired";
        const string TokenExpiredMessageFormat = "Token expired on {0:o}";

        public static AuthenticationBuilder AddJwtBearerConfiguration(this AuthenticationBuilder builder, string authority, string audience)
        {
            return builder.AddJwtBearer(options => {
                options.Authority = authority;
                options.Audience = audience;

                options.TokenValidationParameters = new TokenValidationParameters() {
                    ClockSkew = new System.TimeSpan(0, 0, 30),
                    ValidAudience = audience
                };

                options.Events = new JwtBearerEvents()
                {
                    OnChallenge = context => {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        if (string.IsNullOrWhiteSpace(context.Error))
                        {
                            context.Error = InvalidTokenError;
                        }

                        if (string.IsNullOrWhiteSpace(context.ErrorDescription))
                        {
                            context.ErrorDescription = InvalidTokenErrorDescription;
                        }

                        var ste = context.AuthenticateFailure as SecurityTokenExpiredException;

                        if (ste != null)
                        {
                            context.Response.Headers.Add(TokenExpiredHeader, ste.Expires.ToString("o"));
                            context.ErrorDescription = string.Format(TokenExpiredMessageFormat, ste.Expires);
                        }

                        return context.Response.WriteAsJsonAsync(new {
                            error = context.Error,
                            error_description = context.ErrorDescription
                        });
                    }
                };
            });
        }
    }
}