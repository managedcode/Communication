using Xunit;
using System;

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
    }
}