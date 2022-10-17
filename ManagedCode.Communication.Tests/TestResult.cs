using System.Collections.Generic;

namespace ManagedCode.Communication.Tests;

public class TestResult : BaseResult<TestEnumCode>
{
    public TestResult(bool isSuccess) : base(isSuccess)
    {
    }

    public TestResult(Error<TestEnumCode> error) : base(error)
    {
    }

    public TestResult(List<Error<TestEnumCode>> errors) : base(errors)
    {
    }
}