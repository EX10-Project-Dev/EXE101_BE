namespace DAL.Constants;

public static class AppRoles
{
    public const string Customer = "customer";
    public const string Merchant = "merchant";
    public const string Admin = "admin";

    public static readonly string[] All = [Customer, Merchant, Admin];
}
