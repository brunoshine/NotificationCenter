
namespace NotificationCenterWebApp.NotificationHandlers
{
    /// <summary>
    /// Represents an object that can inject html code on a web page
    /// </summary>
    public interface ICodeInjector
    {

        /// <summary>
        /// Gets the html code to inject on the web page to show the toast notifications
        /// </summary>
        /// <returns>A <see cref="string"/> representing the html to inject on the web page</returns>
        string GetCode();
    }
}
