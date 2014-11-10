using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(NotificationCenterWebApp.Startup))]
namespace NotificationCenterWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
