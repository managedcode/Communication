using System.Threading.Tasks;
using Orleans;

namespace ManagedCode.Communication.Tests.TestClusterApp.Grains.Abstractions;

public interface IFilteredGrain : IGrainWithStringKey
{
    ValueTask<Result<int>> GetNumber();
}