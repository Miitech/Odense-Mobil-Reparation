namespace Repair.Abstractions.Api;

public static class ApiRoutes
{
    public const string CONTROLLER_TEMPLATE = "api/[controller]";
    public const string CUSTOMER_PREFIX = "/api/Customer";
    public const string ORDER_PREFIX = "/api/Order";
    public const string REPORT_PREFIX = "/api/Report";

    public const string ID = "/{id}";
    public const string CONSTRAINED_ID = "/{id:int:min(1)}";
}
