using System;
using System.Reflection;
using System.Windows;

namespace Mass_Effect_2_TLK_Tool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /* can be used to test various translations of Mass Effect 2 TLK Tool */
        /* public App()
        {
            Mass_Effect_2_TLK_Tool.Properties.Resources.Culture = new CultureInfo("en-US");
        } */

        public static string GetVersion()
        {
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver.Major + "." + ver.Minor + "." + ver.Build;
        }
    }

    public class EndiannessException : ApplicationException
    {
    }
}
