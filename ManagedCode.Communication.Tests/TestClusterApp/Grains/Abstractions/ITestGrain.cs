using System.Threading.Tasks;
using Orleans;

namespace ManagedCode.Communication.Tests.TestClusterApp.Grains.Abstractions;

public interface ITestGrain : IGrainWithStringKey
{
    ValueTask<Result> GetFailedResult();
}