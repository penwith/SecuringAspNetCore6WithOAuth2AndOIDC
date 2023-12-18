# Securing ASP.NET Core 6 with OAuth2 and OpenID Connect

## 3.1 - Setting Up Identity Server

```
> dotnet new isempty -n Marvin.IDP
```

Set up using Duende.IdentityServer v6.1.1

This is available on UKHO ProGet, but it is not available on UKHO ProGetTrusted, which suggests that no-one else at the UKHO is using this identity server. To use it ourselves we would need to get it approved and we would have to pay for a license.

In OIDC and OAuth there is no out of the box encryption of tokens - it relies on TLS.

Running the IDP project auto-generates a key that it can then use to signe tokens. This would be replaced with a certificate when moving into production.

TODO : Check out key replacement in final chapter.

## 3.2 - Adding a User Interface

ASP.NET Core Razor pages, views and models

```
> dotnet new isui
```

This adds required pages to the Marvin.IDP project.

Need to add configuration in HostingExtensions.

Solution did not build as the Account Create index page was referencing a non-existant CreateUser method. Have commented this out for now.

## 3.3 - Adding Users to Test With

SubjectId is the users unique identifier and must be unique at the level of the IDP.

Claims are related to Scopes, so there is setup required in the Config class add profile scope:

```
new IdentityResources.Profile()
```

This enables support for this scope across the full identity provider. See IdentityModel.JwtClaimTypes
