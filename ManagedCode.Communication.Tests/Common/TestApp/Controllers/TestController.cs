using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Tests.TestApp.Controllers;

[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet("test1")]
    public ActionResult<string> Test1()
    {
        throw new ValidationException("ValidationException");
    }
    
    [HttpGet("test2")]
    public ActionResult<string> Test2()
    {
        throw new InvalidDataException("InvalidDataException");
    }
    
}