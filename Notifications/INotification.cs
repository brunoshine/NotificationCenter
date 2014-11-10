
namespace Notifications
{
    /// <summary>
    /// Represents a notification that can be handled by the <see cref="NotificationCenter"/>
    /// </summary>
    public interface INotification
    {
        /// <summary>
        /// Gets or sets the type of notification
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Gets or sets the message of the notification
        /// </summary>
        string Message { get; set; }
    }
}
