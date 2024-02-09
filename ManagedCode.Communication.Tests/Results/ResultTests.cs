using Xunit;
using System;
using FluentAssertions;
using ManagedCode.Communication.Tests.Results;

namespace ManagedCode.Communication.Tests
{
    public class ResultTests
    {
        [Fact]
        public void Equals_ReturnsTrue_WhenResultsAreIdentical()
        {
            var error = new Error { Message = "Error", ErrorCode = "E001" };
            var result1 = Result.Fail(error);
            var result2 = Result.Fail(error);

            Assert.True(result1.Equals(result2));
        }

        [Fact]
        public void Equals_ReturnsFalse_WhenResultsAreDifferent()
        {
            var error1 = new Error { Message = "Error1", ErrorCode = "E001" };
            var error2 = new Error { Message = "Error2", ErrorCode = "E002" };
            var result1 = Result.Fail(error1);
            var result2 = Result.Fail(error2);

            Assert.False(result1.Equals(result2));
        }

        [Fact]
        public void GetHashCode_ReturnsSameHashCode_WhenResultsAreIdentical()
        {
            var error = new Error { Message = "Error", ErrorCode = "E001" };
            var result1 = Result.Fail(error);
            var result2 = Result.Fail(error);

            Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
        }

        [Fact]
        public void OperatorEquals_ReturnsTrue_WhenResultIsSuccessAndBooleanIsTrue()
        {
            var result = Result.Succeed();

            Assert.True(result == true);
        }

        [Fact]
        public void OperatorNotEquals_ReturnsTrue_WhenResultIsSuccessAndBooleanIsFalse()
        {
            var result = Result.Succeed();

            Assert.True(result != false);
        }

        [Fact]
        public void ImplicitOperatorBool_ReturnsTrue_WhenResultIsSuccess()
        {
            var result = Result.Succeed();

            bool isSuccess = result;

            Assert.True(isSuccess);
        }

        [Fact]
        public void ImplicitOperatorException_ReturnsException_WhenResultIsFailure()
        {
            var exception = new Exception("Error");
            var result = Result.Fail(Error.FromException(exception));

            Exception resultException = result;

            Assert.Equal(exception, resultException);
        }

        [Fact]
        public void ImplicitOperatorResultFromError_ReturnsFailure_WhenErrorIsProvided()
        {
            var error = new Error { Message = "Error", ErrorCode = "E001" };

            Result result = error;

            Assert.True(result.IsFailed);
        }

        [Fact]
        public void ImplicitOperatorResultFromErrors_ReturnsFailure_WhenErrorsAreProvided()
        {
            var errors = new Error[] { new Error { Message = "Error1", ErrorCode = "E001" }, new Error { Message = "Error2", ErrorCode = "E002" } };
            
            Result result = errors;

            Assert.True(result.IsFailed);
        }

        [Fact]
        public void ImplicitOperatorResultFromException_ReturnsFailure_WhenExceptionIsProvided()
        {
            var exception = new Exception("Error");

            Result result = exception;

            Assert.True(result.IsFailed);
        }
        
          [Fact]
        public void Succeed_ShouldSetIsSuccessToTrue()
        {
            var result = Result<int>.Succeed(5);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Fail_ShouldSetIsSuccessToFalse()
        {
            var result = Result<int>.Fail();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void Fail_WithErrorCode_ShouldSetErrorCodeCorrectly()
        {
            var result = Result<int>.Fail("TestError");
            result.GetError().Value.Message.Should().Be("TestError");
        }

        [Fact]
        public void AddError_ShouldAddErrorToList()
        {
            var result = Result<int>.Succeed(5);
            result.AddError(Error.Create("TestError"));
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Message.Should().Be("TestError");
        }

        [Fact]
        public void ThrowIfFail_ShouldThrowException_WhenErrorsExist()
        {
            var result = Result<int>.Fail("TestError");
            Assert.Throws<Exception>(() => result.ThrowIfFail());
        }

        [Fact]
        public void ThrowIfFail_ShouldNotThrowException_WhenNoErrorsExist()
        {
            var result = Result<int>.Succeed(5);
            result.Invoking(r => r.ThrowIfFail()).Should().NotThrow();
        }

        [Fact]
        public void IsErrorCode_ShouldReturnTrue_WhenErrorCodeMatches()
        {
            var result = Result<int>.Fail("TestError",MyTestEnum.Option2);
            result.IsErrorCode(MyTestEnum.Option2).Should().BeTrue();
        }

        [Fact]
        public void IsErrorCode_ShouldReturnFalse_WhenErrorCodeDoesNotMatch()
        {
            var result = Result<int>.Fail(MyTestEnum.Option2, "TestError");
            result.IsErrorCode(MyTestEnum.Option1).Should().BeFalse();
        }
        
        [Fact]
        public void IsNotErrorCode_ShouldReturnTrue_WhenErrorCodeDoesNotMatch()
        {
            var result = Result<int>.Fail(MyTestEnum.Option2, "TestError");
            result.IsNotErrorCode(MyTestEnum.Option1).Should().BeTrue();
        }

        [Fact]
        public void AddInvalidMessage_ShouldAddMessageToInvalidObject()
        {
            var result = Result<int>.Succeed(5);
            result.AddInvalidMessage("TestKey", "TestValue");
            result.InvalidObject.Should().ContainKey("TestKey");
            result.InvalidObject["TestKey"].Should().Be("TestValue");
        }
        
        
        [Fact]
        public void Fail_WithException_ShouldSetExceptionCorrectly()
        {
            var exception = new Exception("TestException");
            var result = Result<int>.Fail(exception);
            result.GetError().Value.Exception().Message.Should().Be("TestException");
        }

        [Fact]
        public void Fail_WithValue_ShouldSetValueCorrectly()
        {
            var result = Result<int>.Fail(5);
            result.Value.Should().Be(5);
        }
        
        [Fact]
        public void From_ShouldReturnSuccessResult_WhenFuncDoesNotThrow()
        {
            var result = Result<int>.From(() => 5);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(5);
        }

        [Fact]
        public void From_ShouldReturnFailedResult_WhenFuncThrows()
        {
            var result = Result<int>.From(() =>
            {
                throw new Exception("TestException");
                return 5;
            });
            result.IsFailed.Should().BeTrue();
            result.GetError().Value.Exception().Message.Should().Be("TestException");
        }

        [Fact]
        public void Invalid_ShouldReturnFailedResult_WithInvalidObject()
        {
            var result = Result<int>.Invalid("TestKey", "TestValue");
            result.IsInvalid.Should().BeTrue();
            result.InvalidObject.Should().ContainKey("TestKey");
            result.InvalidObject["TestKey"].Should().Be("TestValue");
        }

        [Fact]
        public void Succeed_ShouldReturnSuccessResult_WithValue()
        {
            var result = Result<int>.Succeed(5);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(5);
        }

        [Fact]
        public void OperatorEquals_ShouldReturnTrue_WhenResultIsSuccessAndBooleanIsTrue()
        {
            var result = Result<int>.Succeed(5);
            (result == true).Should().BeTrue();
        }

        [Fact]
        public void OperatorNotEquals_ShouldReturnTrue_WhenResultIsSuccessAndBooleanIsFalse()
        {
            var result = Result<int>.Succeed(5);
            (result != false).Should().BeTrue();
        }
        
    }
}