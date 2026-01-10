using HotChocolate.Authorization;
using property_service.Interfaces;
using property_service.Models.PropertyModels;

namespace property_service.GraphQl.Queries;

public class PropertyQuery
{
    [Authorize(Policy = "OrgRequired")]
    public async Task<IEnumerable<Property>> GetProperties(
        [Service] IPropertyService service)
    {
        return await service.GetPropertiesAsync();
    }
}
