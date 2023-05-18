using ManagedCode.Communication.Tests.TestApp;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Communication.Tests;

[Collection(nameof(TestClusterApplication))]
public class ControllerTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _testApp;

    public ControllerTests(TestClusterApplication testApp, ITestOutputHelper outputHelper)
    {
        _testApp = testApp;
        _outputHelper = outputHelper;
    }
    
}