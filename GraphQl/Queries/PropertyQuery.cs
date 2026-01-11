using HotChocolate.Authorization;
using System.Security.Claims;
using property_service.Models.PropertyModels;
using property_service.Database;

namespace property_service.GraphQl.Queries;

public class PropertyQuery
{
    [UseFiltering]
    [UseSorting]
    [Authorize(Policy = "OrgRequired")]
    public IQueryable<Property> GetProperties(
        ClaimsPrincipal user,
        [Service] PropertyDbContext db)
    {
        var orgIdRaw = user.FindFirst("organization_id")?.Value;

        if (string.IsNullOrWhiteSpace(orgIdRaw) || !int.TryParse(orgIdRaw, out var orgId))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Missing or invalid organization_id in token.")
                    .SetCode("ORG_ID_MISSING")
                    .Build());
        }

        return db.Properties.Where(x => x.OrganizationId == orgId);
    }
}
