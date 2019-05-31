using System;
using System.Threading.Tasks;

namespace wlpm.Repository.Provider
{
    public class Bitbucket: RepositoryProviderInterface
    {
        public bool IdentifyURL(string repositoryUrl)
        {
            return repositoryUrl.StartsWith("https://bitbucket.org/");
        }
        public string GetZipFileUrl(string repositoryUrl, string version)
        {
            var uri = new Uri(repositoryUrl);
            var path = uri.AbsolutePath.Split('/');
            if(path.Length < 3) {
                throw new RepositoryException("Wrong repository URL: " + repositoryUrl);
            }
            var user = path[1];
            var repo = path[2];
            if(version == "*" || version.Length == 0) {
                version = "HEAD";
            }
            return "https://bitbucket.org/" + user + "/" + repo + "/get/" + version + ".zip";
        }
    }
}