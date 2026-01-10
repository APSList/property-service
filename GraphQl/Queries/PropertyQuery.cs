using HotChocolate.Authorization;
using System.Security.Claims;
using property_service.Interfaces;
using property_service.Models.PropertyModels;

namespace property_service.GraphQl.Queries;

public class PropertyQuery
{
    [UseFiltering]
    [UseSorting]
    [Authorize(Policy = "OrgRequired")]
    public async Task<IQueryable<Property>> GetProperties(
        ClaimsPrincipal user,
        [Service] IPropertyService service)
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

        var list = await service.GetPropertiesAsync();

        return list.AsQueryable();
    }
}
