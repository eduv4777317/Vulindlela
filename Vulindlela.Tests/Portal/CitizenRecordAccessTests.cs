using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Vulindlela.Portal.Mvc.Security;
using Vulindlela.Tests.Booking;

namespace Vulindlela.Tests.Portal
{
    public class CitizenRecordAccessTests
    {
        private readonly IAuthorizationService _authorization;

        public CitizenRecordAccessTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.CanViewCitizenRecord, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new CitizenRecordAccessRequirement());
                });
            });
            services.AddSingleton<IAuthorizationHandler, CitizenRecordAccessHandler>();
            _authorization = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        [Fact]
        public async Task Citizen_CanViewTheirOwnRecord()
        {
            var thandi = SignedIn(Roles.Citizen, TestCitizens.Thandi);

            var result = await _authorization.AuthorizeAsync(thandi, TestCitizens.Thandi, Policies.CanViewCitizenRecord);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task Citizen_CannotViewAnotherCitizensRecord()
        {
            var thandi = SignedIn(Roles.Citizen, TestCitizens.Thandi);

            var result = await _authorization.AuthorizeAsync(thandi, TestCitizens.Sipho, Policies.CanViewCitizenRecord);

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task Official_CanViewAnyCitizensRecord()
        {
            var official = SignedIn(Roles.Official, null);

            var thandi = await _authorization.AuthorizeAsync(official, TestCitizens.Thandi, Policies.CanViewCitizenRecord);
            var sipho = await _authorization.AuthorizeAsync(official, TestCitizens.Sipho, Policies.CanViewCitizenRecord);

            Assert.True(thandi.Succeeded);
            Assert.True(sipho.Succeeded);
        }

        [Fact]
        public async Task CitizenWithoutAnIdOnTheSession_IsRefused()
        {
            var noId = SignedIn(Roles.Citizen, null);

            var result = await _authorization.AuthorizeAsync(noId, TestCitizens.Thandi, Policies.CanViewCitizenRecord);

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task SignedOutVisitor_IsRefused()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            var result = await _authorization.AuthorizeAsync(anonymous, TestCitizens.Thandi, Policies.CanViewCitizenRecord);

            Assert.False(result.Succeeded);
        }

        private static ClaimsPrincipal SignedIn(string role, string? citizenId)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "Test user"), new(ClaimTypes.Role, role) };
            if (citizenId is not null)
            {
                claims.Add(new Claim(PortalClaims.CitizenId, citizenId));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
    }
}
