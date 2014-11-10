using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace NotificationCenterWebApp.NotificationHandlers
{
    /// <summary>
    /// Intercepts the http response to append the required html content to show off the notifications
    /// </summary>
    public class ToastNotificationsHttpModule : IHttpModule
    {
        /// <summary>
        /// The MEF composition container;
        /// </summary>
        static CompositionContainer container;

        /// <summary>
        /// Helper <see cref="Object"/> to perform a singleton implementation.
        /// </summary>
        private static readonly object LockObj = new object();


        /// <summary>
        /// Holds the collection of <see cref="INotificationHandler"/> that will handle the pushed notifications.
        /// </summary>
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<Lazy<ICodeInjector>> codeInjectors;

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module that implements System.Web.IHttpModule.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastNotificationsHttpModule"/> class.
        /// </summary>
        public ToastNotificationsHttpModule()
        {
            if (container == null)
            {
                lock (LockObj)
                {
                    if (container == null)
                    {
                        var catalog = new DirectoryCatalog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"));
                        container = new CompositionContainer(catalog);
                    }
                }
            }
            container.ComposeParts(this);
        }

        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="application">An System.Web.HttpApplication that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
        public void Init(HttpApplication application)
        {
            application.BeginRequest += application_BeginRequest;
            application.PostReleaseRequestState += PostReleaseRequestState;

        }

        /// <summary>
        /// Occurs as the first event in the HTTP pipeline chain of execution when ASP.NET responds to a request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void application_BeginRequest(object sender, EventArgs e)
        {
            var bundles = System.Web.Optimization.BundleTable.Bundles.GetRegisteredBundles();
            var bundleCSS = bundles.Where(b => b.Path.Contains("~/Content/css")).FirstOrDefault();
            var bundleJS = bundles.Where(b => b.Path.Contains("~/bundles/jquery")).FirstOrDefault();

            if (bundleCSS != null && bundleJS != null)
            {
                bundleCSS.Include("~/Content/toastr.css");
                bundleJS.Include("~/Scripts/toastr.js");
            }
        }

        /// <summary>
        /// Occurs when ASP.NET has completed executing all request event handlers and the request state data has been stored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PostReleaseRequestState(object sender, EventArgs e)
        {
            HttpApplication application = sender as HttpApplication;
            InjectHttpResponseBody(application.Context, codeInjectors);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="codeInjectors"></param>
        public void InjectHttpResponseBody(HttpContext context, IEnumerable<Lazy<ICodeInjector>> codeInjectors)
        {
            try
            {
                var response = context.Response;
                response.Filter = new ToastFilter(response.Filter, response.ContentEncoding, codeInjectors);
            }
            catch (Exception exception)
            {
                //TODO: add logging here
            }
        }
    }
}