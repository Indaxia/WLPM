using System;
using System.Diagnostics;
using System.Globalization;

namespace wlpm
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            new Application(Environment.CurrentDirectory, args, fvi.ProductVersion);
        }
    }
}
