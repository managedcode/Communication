namespace ManagedCode.Communication.AspNetCore.Configuration;

/// <summary>
/// Configuration options for Communication library in ASP.NET Core applications
/// </summary>
public class CommunicationOptions
{
    /// <summary>
    /// Gets or sets whether to show detailed error information in responses
    /// </summary>
    public bool ShowErrorDetails { get; set; } = false;
}