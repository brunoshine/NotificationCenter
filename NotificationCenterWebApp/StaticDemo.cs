using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NotificationCenterWebApp
{
    public static class StaticDemo
    {
        public static string MESSAGE;

        public static void SetMessage(string message)
        {
            MESSAGE = message;
        }
    }
}