using System;

namespace wlpm.Repository
{
    public class RepositoryException: Exception
    {
        public RepositoryException()
        {
        }

        public RepositoryException(string message): base(message)
        {
        }
    }
}