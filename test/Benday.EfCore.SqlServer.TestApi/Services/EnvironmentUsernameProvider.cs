using Benday.EfCore.ServiceLayers;

namespace Benday.EfCore.SqlServer.TestApi.Services;

/// <summary>
/// Simple <see cref="IUsernameProvider"/> for the worked example. A real app
/// would back this with HttpContext.User; here it returns the OS username.
/// </summary>
public class EnvironmentUsernameProvider : IUsernameProvider
{
    /// <inheritdoc />
    public string Username => Environment.UserName;
}
