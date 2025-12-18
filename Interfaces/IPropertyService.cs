using property_service.Models.PropertyModels;

namespace property_service.Interfaces;

public interface IPropertyService
{
    Task<List<Property>> GetPropertiesAsync(PropertyFilter filter);
    Task<Property?> GetPropertyByIdAsync(int id);
    Task<int> InsertPropertyAsync(PropertyCreateRequestDTO dto);
    Task<int?> UpdatePropertyAsync(int id, PropertyCreateRequestDTO dto);
    Task<int?> DeletePropertyByIdAsync(int id);
}
