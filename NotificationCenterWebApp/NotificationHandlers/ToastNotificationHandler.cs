using Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NotificationCenterWebApp.NotificationHandlers
{
    [Export(typeof(INotificationHandler))]
    [Export(typeof(ICodeInjector))]
    public class ToastNotificationHandler : INotificationHandler, ICodeInjector
    {
        /// <summary>
        /// The name to look for on the <see cref="HttpContext"/> that stores an instance of this class.
        /// </summary>
        private readonly string NAME = "NotificationCenterWebApp.NotificationHandlers.ToastNotificationHandler";
        
        /// <summary>
        /// Helper <see cref="Object"/> for implementing thread-safe object creation.
        /// </summary>
        private static object @lock = new object();

        /// <summary>
        /// List of notification categories that this handler supports.
        /// </summary>
        IEnumerable<string> categories = new List<string> { "NOTIFICATION" };


        /// <summary>
        /// Gets the list of notification categories that this handler supports.
        /// </summary>
        public IEnumerable<string> Categories
        {
            get { return categories; }
        }

        /// <summary>
        /// Holds all the notifications by the order they were pushed.
        /// </summary>
        public BlockingCollection<INotification> Notifications {
            get
            {
                return GetInstance();
            }
        }

        /// <summary>
        /// Gets the current instance of the <see cref="BlockingCollection<INotification>"/> stored on the <see cref="HttpContext"/>.
        /// </summary>
        /// <returns></returns>
        private BlockingCollection<INotification> GetInstance()
        {
            var ctx = HttpContext.Current;
            var instance = ctx.Items[NAME] as BlockingCollection<INotification>;
            if(instance == null)
            {
                lock(@lock)
                {
                    if (instance == null)
                    {
                        ctx.Items[NAME] = instance = new BlockingCollection<INotification>();
                    }
                }
            }
            return instance;
        }
        
        /// <summary>
        /// Pushes a new message
        /// </summary>
        /// <param name="notification">A instance of the <see cref="INotification"/> that represents the message</param>
        /// <returns>true is the message was pushed with success; otherwise false;</returns>
        public async System.Threading.Tasks.Task<bool> PushNotificationAsync(INotification notification)
        {
            if (notification != null)
            {
                Notifications.Add(notification);
                return await Task.Run<bool>(() => { return true; });
            }
            return false;
        }

        /// <summary>
        /// Gets the html code to inject on the web page to show the toast notifications
        /// </summary>
        /// <returns>A <see cref="string"/> representing the html to inject on the web page</returns>
        public string GetCode()
        {
            var sb = new StringBuilder("<script>");
            INotification notification;
            var list = Notifications;
            while (list.TryTake(out notification))
	        {
                //TODO: we should ensure that only available toastr notification types are allowed!
                sb.AppendFormat("toastr['{0}'](\"{1}\");", notification.Type.ToLowerInvariant(), notification.Message.Replace("\"", "'")); 
	        }
            return sb.Append("</script>").ToString();
        }
    }
}