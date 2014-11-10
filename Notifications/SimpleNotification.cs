﻿
namespace Notifications
{
    /// <summary>
    /// Represents a notification that can be handled by the <see cref="NotificationCenter"/>
    /// </summary>
    public class SimpleNotification : INotification
    {
        /// <summary>
        /// Gets or sets the type of notification
        /// </summary>
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the message of the notification
        /// </summary>
        public string Message
        {
            get;
            set;
        }
    }
}
