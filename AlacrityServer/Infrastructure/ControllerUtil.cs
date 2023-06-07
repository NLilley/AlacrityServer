using AlacrityCore.Utils;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Infrastructure;

public static class ControllerUtil
{
    public static int GetClientId(this ControllerBase controller)
        => GetClientId(controller.HttpContext);
    

    public static int GetClientId(this HttpContext context)
    {
        var authenticated = context.Session.GetInt32(SessionUtil.IsAuthenticatedKey);
        if (authenticated != 1)
            throw new InvalidOperationException("Cannot get clientId as client is not authenticated");

        var clientId = context.Session.GetInt32(SessionUtil.ClientIdString);
        if (clientId is null | clientId <= 0)
            throw new InvalidOperationException($"Cannot get clientId as clientId is invalid {clientId}");

        return clientId.Value;
    }
}
