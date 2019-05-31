using System;

namespace wlpm
{
    public class PackageException: Exception
    {
        public PackageException()
        {
        }

        public PackageException(string message): base(message)
        {
        }
    }
}