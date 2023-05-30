using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Tests.TestApp.Controllers;

[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet("test1")]
    public ActionResult<string> Authorize()
    {
        throw new ValidationException();
    }
    
}