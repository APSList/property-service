using property_service.Database;
using property_service.Models.PropertyModels;

namespace property_service.GraphQl.Queries;

public class PropertyQuery
{
    // GET properties (z GraphQL filterji & sortingom)
    [UseFiltering]
    [UseSorting]
    public IQueryable<Property> GetProperties(
        [Service] PropertyDbContext db)
    {
        return db.Properties;
    }

    // GET property by id
    public async Task<Property?> GetPropertyById(
        int id,
        [Service] PropertyDbContext db)
    {
        return await db.Properties.FindAsync(id);
    }
}
