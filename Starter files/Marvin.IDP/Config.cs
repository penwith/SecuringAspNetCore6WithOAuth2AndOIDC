using Duende.IdentityServer.Models;

namespace Marvin.IDP;

public static class Config
{
    // Identity resources are claims related to the user
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        { 
            new IdentityResources.OpenId()
        };

    // map to apis
    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
            { };

    // configure the client apps here
    public static IEnumerable<Client> Clients =>
        new Client[] 
            { };
}