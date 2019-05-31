using System;
using System.Diagnostics;
using System.Globalization;

namespace wlpm
{
    class Program
    {
        static void Main(string[] args)
        {
            new Application(Environment.CurrentDirectory, args);
        }
    }
}
