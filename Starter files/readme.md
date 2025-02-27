﻿# Securing ASP.NET Core 6 with OAuth2 and OpenID Connect

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

## 4.1 - Configuring IndentityServer to log in with the Authorization flow

PKCE Protection is enabled by default.

Add a client with the required grant types, in this case the 'code' flow. This flow delivers to the browser via URI redirection, so you must specify a valid URI that the client is able to receive tokens on.

/signin-oidc - this can be configured on the web client, but this is the default value.

We also need to configure the scopes that can be requested by the client.

And lastly a client secret for client authentication to allow the client application to execute an authenticated call to the token endpoint.

## 4.2 - Logging in with the Authorization Code Flow

There's a bit of configuration required at level of our web application as well. 

First of all, we'll need support for OpenID Connect. For that, open the NuGet dialog, and browse Microsoft.AspNetCore.Authentication.OpenIdConnect. 

There we go, let's install it, and so far for that. 

Let's open the Program class at the level of our ImageGallery.Client. There's two things we need to do here. We need something to take care of the client‑side part of the OpenID Connect flow, and we need somewhere, some place we can use to store the user's identity. 

We start by calling into builder.Services.AddAuthentication. We'll use that to configure the authentication middleware. On the options, we set the DefaultScheme to cookies, that's a constant value that can easily be accessed via CookieAuthenticationDefaults.AuthenticationScheme. We do need to import the Microsoft.AspNetCore.Authentication.Cookies namespace for that. You can choose this value, but it should correspond to the logical name for a particular authentication scheme. In our case, we're quite okay with cookies. By setting this value, we can sign into this scheme, we can sign out of it, and we can read scheme‑related information, and so on, simply by referring to this cookies scheme name. This is not strictly necessary in our case, by the way, but I prefer to be explicit about this so we can get a better understanding of what's going on. Moreover, if you're hosting different applications on the same server, you'll want to ensure that these have a different scheme name in order to avoid them interfering with each other. We also need to set the DefaultChallengeScheme to a value, and we're going to set it to OpenIDConnectDefaults.AuthenticationScheme. 

Let me import the necessary namespace, this gives it OpenIDConnect as value, and this will have to match the scheme we use to configure the OpenIDConnect, as we'll see soon. Then we call in to AddCookie. We pass through an authentication scheme here, our CookieAuthenticationDefaults.AuthenticationScheme. This configures the cookie handler, and it enables our application to use cookie‑based authentication or our default scheme. What this means is that once an identity token is validated and transformed into a claims identity, it will be stored in an encrypted cookie, and that cookie is then used on subsequent requests to the web app to check whether or not we are making an authenticated request. 

In other words, it's the cookie that is checked by our web app because we configured it like this. 

The next call we want to make is a call into AddOpenIDConnect to register and configure the OpenID Connect handler. This enables our application to support the OpenID Connect Authentication workflow. In our case, that will be the code flow. In other words, it is this handler that will handle creating the authorization requests, token requests, and other requests, and it will ensure that identity token validation happens. Let's see what we need to set on this. 

We register these services for the OpenID Connect scheme. This will ensure that when a part of our application requires authentication, OpenID Connect will be triggered as default, as we set the DefaultChallengeScheme to OpenIDConnect as well. The next thing we see here is that the SignInScheme is set to Cookies, and that matches the default scheme name for authentication. This ensures that the successful result of authentication will be stored in the cookie matching this scheme. We have a few other things to configure. 

The Authority should be set to the address of our identity provider because that is the authority responsible for the identity provided part of the OpenID Connect flows. The middleware will use this value to read the metadata on the discovery endpoint so it knows where to find the different endpoints and other information. 

Then there's the ClientId, which should match the ClientId at the level of the identity provider, and we also pass through the client's secret, which matches the secret we configured at the level of the identity provider. This ensures that our client can do an authenticated call to the token endpoint. 

Up next is the ResponseType. We want to set that to code. ResponseType corresponds to a grant or flow. By setting it to code, we signify that we want to use the code grant. For that one, PKCE protection is required, and the middleware automatically enables this when code is the ResponseType. We will look into that later. 

Up next are the scopes we want our application to request. Seeing we support the openid and profile scopes, those are the scopes we want to request. We can do that by calling into Scope.Add, passing through the scope name. The thing is, though, we don't have to. By default, openid and profile scopes are requested by the middleware, so let me comment it out, but leave it there for reference. 

And then there's the CallbackPath. Remember that redirect URI? We set it to our host followed by the port, followed by a slash, followed by signin‑oidc at IDP level. We did that in the Config class. We also need to configure this at this site, as it's part of the validation process of the request. But, signin‑oidc is the default value used by this middleware, so, we don't have to set this manually either. Again, I will leave it here for reference, so you know how to change it in case you want to choose another endpoint. 

The last thing I'd like to do is set options.SaveTokens to true. This allows the middleware to save the tokens it receives from the identity provider, so, we can use them afterwards. 

Now let's scroll down a bit. Here, we can configure the request pipeline. 

What we want to ensure is that the authentication middleware is effectively used, and for that, we call into app.UseAuthentication. It's important that we add this to the request pipeline, as we want the request to be blocked for unauthenticated users. A great place to put this is between UseRouting, so, the middleware potentially has access to the route data, and MapControllerRoute, so, the middleware can effectively block access to the endpoint. 

That's it for configuring the request pipeline. 

What we have to do now is ensure that a part of our application cannot be accessed without an authenticated user. More specifically, our GalleryController. So let's open that, and let's decorate it with the Authorize attribute. That's defined in Microsoft.AspNetCore.Authorization, so we need to import that namespace. Lastly, to make our lives a bit easier during the demos, let's add a bit of helper code to log the identity token and resulting user object. That'll make it easy for us to see what's going on. So let me scroll down a bit, and here near the bottom, let me paste that in. 

To get to save token, we call into GetTokenAsync on the HTTP context. To get access to that method, we do need to import the Microsoft.AspNetCore.Authentication namespace. The token we want to get in this case is an identity token, so, we pass through OpenIDConnectParameterNames.IdToken. That simply refers to a string, id_token. 

So we have the identity token, and we log that, and next to the identity token we also log all the user's claims. We can find those claims in the user object, which is exposed by controller base. So, on each controller you have access to the user object, which contains the claims that are the result of the complete OpenID Connect flow. Now let me import System.Text so StringBuilder resolves as well. By the way, if you're wondering where that token is coming from, the middleware, by default, stores it inside the properties section of the application cookie, as long as you set save tokens to true when configuring the middleware, which is what we did. 

Now let's ensure that this information gets logged to our output window, and to enable that, we simply execute it whenever the index method is called. There we go. Let's set a breakpoint at the index action, and let's right‑click our solution again, and go to the Startup Project, and let's make sure that we start up all the projects we need. We need multiple startup projects, we need the client, we need our identity provider, and we need the API, and that should do it. Let's click OK, and let's build and run. 

So, what actually happened here is that the middleware received an authorization code from the authorization endpoint. It used that and the client ID and secret to call the token endpoint and received an identity token, which it validated, and afterwards created a claims identity from it. That is then stored in an authentication ticket, which is stored in encrypted cookie. 

Mind you, all of this can be done manually as well. We can manually create an authentication request, we can parse the token, we can validate it, and so on, but the OpenID Connect middleware makes this a lot easier for us. 

Copy the token from the console into jwt.io 

So here's our identity token, and we indeed find a subclaim here, which matches the identifier in our Claims list. Note that there is no profile‑related info in here, like my given name or family name, so we're not completely there yet. We will look into why this is the case, later on in the module. The good news was that we're logged into our web client. 

One small variation I'd like to point out is the optional inclusion of a consent screen. 

Here we can set RequireConsent to true. By default, this value is false. Let's see what gives. We're navigating to the web client, which redirects to the identity provider, I log in again, and we see a consent screen here. Anyway, here we can check whether or not we want to allow the client application access to some of our information, and this should look quite familiar because if you've ever clicked a Login with Facebook button on a website, you should have seen something familiar. However, mostly for internal applications or applications that are completely under your control, this screen is not shown. I'm going to leave it on, though, because it makes it a bit easier to see what's going on once we start requesting additional scopes. Let's allow this, and we hit our Index action again, meaning we're authenticated.

## 4.3 - Logging out of Our Web Application

Remember to log out of the identity provider as well as locally

Should add a redirect, so...

## 4.4 - Redirecting After Logging Out

host:port/signout-callback-oidc is the default so you don't have to register this on the client, but you do need to register it on the IDP. Add the following to the Client config on the IDP:

```
PostLogoutRedirectUris =
{
    "https://localhost:7184/signout-callback-oidc"
}
```
This will give you a link back to the client app. To automatically redirect update the LogoutOptions class:

```
public static bool AutomaticRedirectAfterSignOut = true;
```

## 4.5 - Returning Additional Claims From the UserInfo Endpoint

By default, IdentityServer does not include identity claims, save for the user identifier, in the identity token. 

We could allow this by setting the AlwaysIncludeUserClaimsInIdToken property to true when configuring a client, but we don't want to do that. 

There's two main reasons for this. 

One, in some flows or variations of flows the identity token can be returned from the authorization endpoint directly. If a set of claims are included in that token, it becomes bigger, potentially resulting in URI length limits. These are dependent on the browser, and that is not a good place to be. Most modern browsers don't have issues with long URIs anymore, but older browsers still do. So, it still is something to keep in mind. Moreover, you don't know in advance which hubs, proxies, caches, and/or servers your requests and responses will pass through between your client app and your identity provider. It's not unrealistic that one of those won't play nice with long URIs. 

And two, identity tokens are sometimes passed around. For example, when ending a session or in some federation‑related scenario. That increases the chance of an attacker getting ahold of it; therefore, the less information that's in there, the better. Not including claims in the identity token that aren't required is thus the prefered approach. But how do we get access to those claims then? 

Well, we learned about the token endpoint and the authorization endpoint at level of the IDP, but there's another one, the UserInfo endpoint. 

This is an endpoint the client application can call to get additional information on the user. Calling this endpoint requires an access token with scopes that relate to the claims that have to be returned. So, if we want profile information from the UserInfo endpoint, the access token must contain the profile scope. 

It is a bit too early to dive into access tokens in this part of the course, so for now it's sufficient to know that our OpenID Connect middleware needs one to call that UserInfo endpoint. 

Other tokens can be returned from this endpoint as well, access tokens and refresh tokens, for example. So, in reality, with our current flow an access token is returned next to that identity token. It is delivered to the client, so the middleware now has access to it. In the next step, after having validated the identity token, the middleware sends a request to the UserInfo endpoint, passing through the access token. At that level, the access token is validated and the UserInfo endpoint returns the user's claims that are related to the scopes in the access token. 

Typically, those user claims are then added to the claims identity, so there's easy access to them throughout the application. 

In the client we set GetClaimsFromUserInfoEndpoint to true:

```
options.GetClaimsFromUserInfoEndpoint = true;
```

This does not set the claims in the token, so is this an alternative to requiring an access token? Can our API just take an Indentoty token and hit the user info endpoint for the claim? Probably, but this is probably not best practice.

## 5.1 - Claims Transformation: Keeping the Original Claim Types

Default claim types don't make much sense any more. Keep your original claim types by clearing the default inbound claim type map.

## 5.2 - Claims Transformation: Manipulating the Claims Collection

Remove claims that you don't need. Add ones that you do.

```
options.ClaimActions.Remove("aud"); // Call remove to ADD this to the collection!!!
options.ClaimActions.DeleteClaim("sid");
options.ClaimActions.DeleteClaim("idp");
```

## 5.3 - Role-based Authorization: Ensuring the Role Is Included

We extended the in-memory user and gave them a new claim

```
new Claim("role", "FreeUser")

```

Create a new identity scope

```
public static IEnumerable<IdentityResource> IdentityResources =>
    new IdentityResource[]
    { 
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("role", "Your role(s)")
    };
```

Give it 
- a name that matches the new claim name
- a display name (to be displayed on consent screen)
- a list of claims that must be returned an application asks for this role scope.

To enable the client application to request the role scope, we have to explicitly allow this, so we add roles to the AllowedScopes list on the IDP:

```
public static IEnumerable<Client> Clients =>
    new Client[]
    {
        new Client()
        {
            ClientName = "Image Gallery",
            ClientId = "imagegalleryclient",
            AllowedGrantTypes = GrantTypes.Code,
            RedirectUris =
            {
                "https://localhost:7184/signin-oidc"
            },
            PostLogoutRedirectUris =
            {
                "https://localhost:7184/signout-callback-oidc"
            },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "roles"
            },
            ClientSecrets =
            {
                new Secret("secret".Sha256())
            },
            RequireConsent = true
        }
    };
```

The client app the requests the role scope and adds a mapping for the role. As the user might be in multiple roles we should not MapUniqueJsonKey, but insead use MapJsonKey:

```
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = "https://localhost:5001";
        options.ClientId = "imagegalleryclient";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        //options.Scope.Add("openid");
        //options.Scope.Add("profile");
        //options.CallbackPath = new PathString("signin-oidc");
        // SignedOutCallbackPath: default = host:port/signout-callback-oidc.
        // Must match with the post logout redirect URI at IDP client config if
        // you want to automatically return to the application after logging out
        // of IdentityServer.
        // To change, set SignedOutCallbackPath
        // eg: options.SignedOutCallbackPath = new PathString("pathaftersignout");
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.ClaimActions.Remove("aud"); // Call remove to ADD this to the collection!!!
        options.ClaimActions.DeleteClaim("sid");
        options.ClaimActions.DeleteClaim("idp");
        options.Scope.Add("roles");
        options.ClaimActions.MapJsonKey("role", "role");
    });
```

## 5.4 - Role-based Authorization: Using the Role in Your Views

Tell the framework where to find the user's role. TokenValidationParameters allows us to specify the name claim and role claim types as well as detailing how to validate the token:

```
options.TokenValidationParameters = new TokenValidationParameters
{
    NameClaimType = "given_name",
    RoleClaimType = "role"
};
```

## 5.5 - Role-based Authorization: Using the Role in Your Controllers

Authorize access to a specific action depending on the role:

```
[Authorize(Roles = "PayingUser")]
public IActionResult AddImage()
{
    return View();
}
```

Separate multiple roles with a comma. Add the attribute to the Post metho also:

```
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "PayingUser")]
public async Task<IActionResult> AddImage(AddImageViewModel addImageViewModel)
```

## 5.6 - Creating an Access Denied Page

Add routing in the for the access denied page:

```
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.AccessDeniedPath = "/Authentication/AccessDenied";
})
```

## Chapter 6 - Understanding Authorization with OAuth2 and OpenID Connect

### Learning How OAuth2 Works

OAuth 2 is intended for authorization or delegated authorization to be exact. 

In other words, authorizing access to resources like an API. 

In those scenarios, a client application would request an access token from an authorization server. Let's assume that we're talking about letting a user decide on access. In other words, not just machine-to-machine communication.

A user is involved in this example. A client application starts the flow. This redirects the user to the authorization server. 

Here, as we know by now, users prove who they are, for example, by providing a username and password. 

From this moment on, the authorization server knows who the user is. It can create an access token and sign it.  

The access token is eventually returned from the token endpoint and it represents consent given by the user to the client application to access data the user owns. 

For example, resource is exposed by the API, client application stores this token, and sends it as a bearer token on each request to the API. 

** At the level of the API, the access token is validated and access to the resources can be granted or refused.**

That's the high-level picture, but I've mentioned a few times already that OpenID Connect is a superior protocol. It is an extension of OAuth 2. 

Even if only access tokens are required, you are often using OpenID Connect without even realizing it. 

It standardized additional claims and verification methods even for flows that don't require an identity token. So from that point of view, you could say that you're using OpenID Connect for both authentication and authorization, even though you will still encounter people who prefer to talk about OAuth 2 when only authorization is required. 

### Using OpenID Connect for Authentication and Authorization

So we have a client application that requires authentication at client level and authorization to talk to an API. 

The client application creates an authentication request. This redirects the user to the identity provider. 

As you notice, we're again talking about an identity provider, instead of an authorization server. 

In this case, the identity provider includes the functionality of an authorization server, as is often the case. 

Here, users prove who they are, for example, by providing a username and password. 

The IDP now knows who the user is, and what happens next depends on the flow that's being used as we know by now. 

Important is that, eventually, the client application will receive both an identity and an access token.

The identity token is validated and used as proof of identity. 

With this, we sign into the client app. 

The access token is stored by the client application. 

On each request to the API, it is sent as a bearer token. 

There is a limited form of validation going on that uses the access token like creating a hash from the token to check if it matches the at_hash value of the identity token. 

** Yet, the actual place for validation of the access token is the API.**

So at the level of the API, the access token is validated, and access to resources can potentially be granted. 

### OAuth2 and OpenID Connect Flows

The OIDC authorization code flow and implicit flow are extensions to the two flows from the OAuth 2 standard. 

So these still exist. 

The three variations of the hybrid flow only exist in OpenID Connect. There was no and is no hybrid flow in OAuth 2.

But as we know, hybrid and implicit are no longer best practice flows. If we can, we don't use them anymore. 

Next to that, OAuth 2 also supports 2 other flows, those are flows that don't exist in OpenID Connect. 

One of these is the **resource owner password credentials flow.**

This is the only flow that allows an in-application login screen. The user isn't redirected to the identity provider to provide credentials. Instead, typically, a username and password field are included in your client application. 

So this is something a lot of people are actually looking for because they want to have those input fields for username and password in their client application, but it was only added to OAuth 2 for legacy reasons, and it **should only be used by trusted applications.**

As we learned, these days we really don't want to use in-app login screens anymore. 

Checking credentials and so on should not be the responsibility of a client app. 

Moreover, this is not a redirection-based flow, which will make integrating with other identity providers through federation impossible. 

It makes single sign-on scenarios harder and so on. Most of these are a requirement for enterprise applications. 

As it doesn't exist in OpenID Connect, using it would mean there is no identity token, and we can also not link an access token to such a verifiable identity token, so this one should be avoided. 

The other flow that is supported is the **client credentials flow**. As the name implies, this flow only uses client authentication. 

In other words, typically, the client ID and the client secret. 

These are then exchanged for an access token. 

For that reason, it should only be used by confidential applications. 

It's also not part of OpenID Connect as OpenID Connect includes a user authentication step, and in this flow, there is no user. 

So this flow can be very useful **when you require machine-to-machine communication without user involvement.**

## 7.1 - Securing Access to Your API

Add support for an additional scope. We will require an access token with that scope in it before we allow access to the API:

```
public static IEnumerable<ApiResource> ApiResources =>
    new ApiResource[]
    {
        new ApiResource("imagegalleryapi", "Image Gallery API")
    };
```

and

```
public static IEnumerable<Client> Clients =>
    new Client[]
    {
        new Client()
        {
            .
            .
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "roles",
                "imagegalleryapi"
            },
            .
            .
        }
    };
```

and in the hosting extensions

```
.AddInMemoryApiResources(Config.ApiResources)
```

### So, API Scopes vs API Resources

The scope concept is pretty old. 

It's coming from the OAuth 2 specification, and it simply means the scope of access that is requested by a client. 

So, you could have a read scope, which would allow a client access to read resources at level of the API, a write scope, which would allow access to write certain resources, and so on. 

It's up to us, at the level of the API, to read out these scopes and use them to allow or disallow certain things. 

So that is a simple approach, but it's often not sufficient. 

Resource is another concept that allows for more complex structures. 

You can look at the resource as a physical or logical API. 

In our case, the imagegallery/api is a resource. In more complex systems, you may have multiple physical APIs, which can all be different resources, or you might decide to split up your one physical API into different logical APIs, all with their own resource name, for example, an employee API, a customer API, and so on. 

These resources can have scopes, which can be used for more fine-grained authorization rules inside of the API. 

For example, our imagegallery/api resource can have an imagegallery.read, and imagegallery.write scope. 

So this allows more flexibility. Also, it's not hard to imagine that different client applications that need access to our imagegallery/api are allowed different levels of access inside of that API. 

We could, for example, give our imagegallery web client access to the imagegallery.read and imagegallery.write scopes, but give a yet-to-be-developed mobile imagegallery client only access to the imagegallery.read scope. 

Whenever a scope related to a resource is requested by a client application, ***the access token*** will contain the resource as an audience value, and the scope will be in the scopes list. 

So, if our web client would request imagegallery/api.read, and imagegallery/api.write scopes, we would end up with an access token with imagegallery/api in the audience list, and imagegallery/api.read and imagegallery/api.write in the scopes list. 

If a mobile client requests imagegallery/api.read, we would end up with a token with imagegallery/api in the audience list and imagegallery/api.read in the scopes list. 

And like that, you can use these scopes to build a fine-grained authorization layer for your API. 

For our current demo, we will keep things simple. We'll create one scope, imagegallery/api.fullaccess, linked to our imagegallery/api resource. 

We will only use the audience value for now. In the next module, we will dive deeper into authorization. There, we will extend our example with different scopes and policies that effectively use these scopes.

### Back to the code

Define the scope

```
public static IEnumerable<ApiScope> ApiScopes =>
    new ApiScope[]
    {
        new ApiScope("imagegalleryapi.fullaccess")
    };
```

And then add this scope to the resource:

```
public static IEnumerable<ApiResource> ApiResources =>
    new ApiResource[]
    {
        new ApiResource("imagegalleryapi", "Image Gallery API")
        {
            Scopes = { "imagegalleryapi.fullaccess" }
        }
    };
```

Then ensure the Allowed Scopes list in the client configuration (of the IDP) contains the resource, to enable the client to request it:

```
    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client()
            {
                .
                .
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "roles",
                    "imagegalleryapi.fullaccess"
                },
                .
                .
            }
        }
```

Then in the Web Client, **bearing in mind that we are *already* getting an access token through the back channel**, to ensure we get an access token with this scope included add the scope to the requested list of scopes:

```
builder.Services.AddAuthentication(options =>
    .
    .
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        .
        .
        options.Scope.Add("roles");
        options.Scope.Add("imagegalleryapi.fullaccess");
        .
        .
    });
```

And then to secure the API, that is make it require an access token: 

First add the *Microsoft.AspNetCore.Authentication.JwtBearer* package to your project.

Then in the program class for the API, configure the JWT bearer middleware:

```
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:5001";
        options.Audience = "imagegalleryapi";
    });
```

Authority is the address of our identity provider. The middleare uses this to load metadata so it knows about the endpoints and keys. When this middleware runs for the first time it will read that metadata from the IDP and the cache the result.

This middleware is also responsible for validating the access tokens.

As a valid value for the Audience property, we pass through *imagegalleryapi*. This make sure that imagegalleryapi is checked as an audience value in the token.

We can also check the type header of the token to avoid JWT confusion attacks, which allows attackers to circumvent token signature checking.

```
options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
```

Stating that the only ValidType we want to accept should have at+jwt as type.

Then to ensure that the incoming claims and validation on those claims are mapped and is executed the same way as on our client:

Clear the default inbound claim type map

```
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
```

And when setting up the Token Validation Parameters, state the name and role claim types:

```
options.TokenValidationParameters = new()
{
    NameClaimType = "given_name",
    RoleClaimType = "role",
    ValidTypes = new[] { "at+jwt" }
};
```

Add the authentication middleware to the request pipeline before the middleware request to map controllers, so that we can check if API access is allowed before the request is passed through:

```
app.UseAuthentication();
```

Make sure that the images controller (on the API) actually requires authorization:

```
[Route("api/images")]
[ApiController]
[Authorize]
public class ImagesController : ControllerBase
```

At this point the Client App is no longer allowed access to the API. The give the client access to the API again we need to pass that access token to the API on each request. Check out the next commit.

## 7.2 - Passing an Access Token to Your API

The token should be passed on each request as a Bearer token. There are a number of ways to do this:

We can get the token from our context and manually add it as a bearer token, but we would have to add code to add that access token in every place we use our API.

We can create a custom delegating handler that is responsible for adding the access token on outgoing requests.

There is a popular package - *Identity.AspNetCore* - which contains such a handler. Apparently created by the people who created IdentityServer. 

In the client setup, to register the required services for access token management:

```
builder.Services.AddAccessTokenManagement();
```

Then add a call into AddUserAccessTokenHandler when configuring the API Client, to ensure the access token is passed on each request:

```
builder.Services.AddHttpClient("APIClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ImageGalleryAPIRoot"]);
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
})
    .AddUserAccessTokenHandler();
```

Add some logging for visibility:

```
var accessToken = await HttpContext
    .GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

_logger.LogInformation($"Access token: " +
                    $"\n{accessToken}");
```

## 7.3 - Using Access Token Claims When Getting Resources

How do we know who the current user is?

On the Client, User.Identity (on the controller) is a Claims identity coming from the identity token.

On the API there is a user object as well. It is the same ControllerBase class that we are working with. This time the Claims identity is constructed from the access token.

In the ImagesController.GetImages endpoint:

```
var ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

if (ownerId == null)
{
    throw new Exception("User identifier is missing from token");
}
```

In this demo we are restricting the images returned to those owned by the user, so we pass the id to the repo

```
var imagesFromRepo = await _galleryRepository.GetImagesAsync(ownerId);
```

and use it to filter the results returned from the database

```
public async Task<IEnumerable<Image>> GetImagesAsync(string ownerId)
{
    return await _context.Images
        .Where( (i => i.OwnerId == ownerId))
        .OrderBy(i => i.Title).ToListAsync();
}
```

**We can used access policies to block access to the controllers completely**

## 7.4 - Including Identity Claims in an Access Token

On the IDP we can use a different constructor when creating an api resource to specify a list of claims that have to be returned when requesting the related scope.

```
public static IEnumerable<ApiResource> ApiResources =>
    new ApiResource[]
    {
        new ApiResource("imagegalleryapi", "Image Gallery API", new []{ "role" })
        {
            Scopes = { "imagegalleryapi.fullaccess" }
        }
    };
```

## 7.5 - Protecting the API When Creating a Resource (with Roles)

Add the auth attribute as before

```
[HttpPost()]
[Authorize(Roles = "PayingUser")]
public async Task<ActionResult<Image>> CreateImage([FromBody] ImageForCreation imageForCreation)
```

Get the owner id from claims, as before, and update the entity

```
var ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

if (ownerId == null)
{
    throw new Exception("User identifier is missing from token");
}

imageEntity.OwnerId = ownerId;
```

The owner id is validated and secured. Don't take values like this from your model which can be tampered with.

## 8.1 - Creating an Authorization Policy

## 8.2 - Using an Authorization Policy (Web Client)

## 8.3 - Using an Authorization Policy (API)

## 8.4 - Fine-grained Policies with Scopes

## 8.5 - Creating Custom Requirements and Handlers

## 9.1 - Token Lifetimes and Expiration

## 9.2 - Gaining Long-lived Access

## 9.3 - Working with Reference Tokens

## 9.4 - Revoking Tokens

## 10.1 - Creating a User Database

## 10.2 - Inspecting UI Interaction with IdentityServer

## 10.3 - Inspecting the User Service

## 10.4 - Integrating IdentityServer with a Custom User Database

## 10.5 - Building Your Identity with a Profile Service

## 12.1 - Enabling Windows Authentication on IIS Express

## 12.2 - Integrating Windows Authentication with IdentityServer

## 12.3 - Inspecting Support for Federating with a Third-party Identity Provider

## 12.4 - Registering an Application on Azure AD

## 12.5 - Integrating with Azure AD
