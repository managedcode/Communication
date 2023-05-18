using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Tests.TestApp.Controllers;

public class TestController : ControllerBase
{
    [HttpGet("authorize")]
    public ActionResult<string> Authorize()
    {
        return "Authorize";
    }
    
    [HttpGet("anonymous")]
    public ActionResult<string> Anonymous()
    {
        return "Anonymous";
    }
    
    [HttpGet("admin")]
    public ActionResult<string> Admin()
    {
        return "admin";
    }
    
    [HttpGet("moderator")]
    public ActionResult<string> Moderator()
    {
        return "moderator";
    }
    
    [HttpGet("common")]
    public ActionResult<string> Common()
    {
        return "common";
    }
}