namespace OpenProfileServer.Models.Enums;

public enum AccountType
{
    /// <summary>
    /// Represents a real human user.
    /// </summary>
    Personal = 1,
    
    /// <summary>
    /// Represents a company, NGO, or community group.
    /// </summary>
    Organization = 2,
    
    /// <summary>
    /// Represents a bot or an automated integration.
    /// </summary>
    Application = 3,
    
    /// <summary>
    /// Reserved for internal system operations.
    /// </summary>
    System = 4,
}