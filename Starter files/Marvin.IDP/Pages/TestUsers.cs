// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;

namespace Marvin.IDP;

public class TestUsers
{
    public static List<TestUser> Users
    {
        get
        {  
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "b0fc0e95-d82f-4202-805f-c58f246b4c5c",
                    Username = "David",
                    Password = "password",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.GivenName, "David"),
                        new Claim(JwtClaimTypes.FamilyName, "Flagg") 
                    }
                },
                new TestUser
                {
                    SubjectId = "bd4fc564-0705-41ee-af66-4cf5ad201dcd",
                    Username = "Emma",
                    Password = "password",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.GivenName, "Emma"),
                        new Claim(JwtClaimTypes.FamilyName, "Flagg")
                    }
                }
            };
        }
    }
}