using HotChocolate;

namespace property_service.Enums;

public enum AmenityTypeEnum
{
    [GraphQLName("Pool")] Pool,
    [GraphQLName("Kitchen")] Kitchen,
    [GraphQLName("Gym")] Gym,
    [GraphQLName("Parking")] Parking,
    [GraphQLName("Wifi")] Wifi
}