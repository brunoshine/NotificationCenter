using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifications
{
    /// <summary>
    /// Represents a class that can handle notifications published by the <see cref="NotificationCenter"/>
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>
        /// Gets the list of notification categories that this handler supports.
        /// </summary>
        IEnumerable<string> Categories { get; }

        /// <summary>
        /// Pushes a new message
        /// </summary>
        /// <param name="notification">A instance of the <see cref="INotification"/> that represents the message</param>
        /// <returns>true is the message was pushed with success; otherwise false;</returns>
        Task<bool> PushNotificationAsync(INotification notification);
    }
}
