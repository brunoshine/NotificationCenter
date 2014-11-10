
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Notifications
{
    /// <summary>
    /// Class to notify the user of events that happen in the background.
    /// </summary>
    public sealed class NotificationCenter
    {
        /// <summary>
        /// The singleton instance
        /// </summary>
        private static readonly NotificationCenter instance = new NotificationCenter();

        /// <summary>
        /// The MEF composition container;
        /// </summary>
        static CompositionContainer container;

        /// <summary>
        /// Holds the collection of <see cref="INotificationHandler"/> that will handle the pushed notifications.
        /// </summary>
        [ImportMany(AllowRecomposition=true)]
        public IEnumerable<Lazy<INotificationHandler>> notificationHandlers;

        /// <summary>
        /// Creates a new instance of <see cref="NotificationCenter"/>.
        /// </summary>
        private NotificationCenter()
        {
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory));
            catalog.Catalogs.Add(new DirectoryCatalog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")));
            container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        /// <summary>
        /// Gets an instance of the NotificationCenter
        /// </summary>
        /// <returns>An instance of <see cref="NotificationCenter"/></returns>
        public static NotificationCenter GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Posts a notification to be handled
        /// </summary>
        /// <typeparam name="T">The type of the notification object.</typeparam>
        /// <param name="category">The category of the notification.</param>
        /// <param name="notification">The notification object to send.</param>
        /// <returns></returns>
        public async Task<bool> PostNotificationAsync<T>(string category, T notification) where T : INotification
        {
            try
            {
                var subs = notificationHandlers.Where(x => x.Value.Categories.Any(c => c.Equals(category, StringComparison.InvariantCultureIgnoreCase))).ToList();
                foreach (var item in subs)
                {
                    try
                    {
                        var result = await item.Value.PushNotificationAsync(notification);
                    }
                    catch (Exception ex)
                    {
                        //TODO: Add loogging
                        // Do not throw so that the foreach can continue the loop
                    }
                }
                return true;
            }
            catch (CompositionException compositionEx)
            {
                //TODO: Add loogging
                return false;
            }
            catch (Exception ex)
            {
                //TODO: Add loogging
                return false;
            }
        }
    }

}
