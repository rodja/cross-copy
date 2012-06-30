using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace crosscopyiosclient
{
    public class Application
    {
        static void Main (string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            UIApplication.Main (args, null, "AppDelegate");
        }
    }
}
