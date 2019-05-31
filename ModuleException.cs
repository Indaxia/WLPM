using System;

namespace wlpm
{
    public class ModuleException: Exception
    {
        public ModuleException()
        {
        }

        public ModuleException(string message): base(message)
        {
        }
    }
}