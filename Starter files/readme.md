# Securing ASP.NET Core 6 with OAuth2 and OpenID Connect

## Setting Up Identity Server

Set up using Duende.IdentityServer v6.1.1

This is available on UKHO ProGet, but it is not available on UKHO ProGetTrusted, which suggests that no-one else at the UKHO is using this identity server. To use it ourselves we would need to get it approved and we would have to pay for a license.

In OIDC and OAuth there is no out of the box encryption of tokens - it relies on TLS.

Running the IDP project auto-generates a key that it can then use to signe tokens. This would be replaced with a certificate when moving into production.

TODO : Check out key replacement in final chapter.
