using HotChocolate;

namespace property_service.Enums;

public enum PropertyTypeEnum
{
    [GraphQLName("Apartment")] Apartment,
    [GraphQLName("House")] House,
    [GraphQLName("Villa")] Villa,
    [GraphQLName("Studio")] Studio,
    [GraphQLName("Room")] Room,
    [GraphQLName("Cottage")] Cottage,
    [GraphQLName("Bungalow")] Bungalow,
    [GraphQLName("Chalet")] Chalet,
    [GraphQLName("Duplex")] Duplex,
    [GraphQLName("Penthouse")] Penthouse,
    [GraphQLName("Townhouse")] Townhouse,
    [GraphQLName("Farmhouse")] Farmhouse,
    [GraphQLName("Loft")] Loft,
    [GraphQLName("MobileHome")] MobileHome
}