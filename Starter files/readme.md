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

## 4.1 - Configuring IndentityServer to log in with the Authorization flow

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

## 4.3 - 
