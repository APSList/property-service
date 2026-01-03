namespace property_service.Enums;

public enum PropertyStatusEnum
{
    [GraphQLName("Available")] Available,
    [GraphQLName("SoldOut")] SoldOut,
    [GraphQLName("Closed")] Closed
}