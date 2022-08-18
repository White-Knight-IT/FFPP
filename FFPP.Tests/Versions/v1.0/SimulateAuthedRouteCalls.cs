using FFPP.Api.v10;

namespace FFPP.Tests.v10;

public class SimulateAuthedRouteCalls
{
    [Fact]
    public async void TestCurrentRouteVersion()
    {
        var output = await Routes.CurrentRouteVersion();

        Assert.Equal(output, new Routes.CurrentApiRoute());
    }
}