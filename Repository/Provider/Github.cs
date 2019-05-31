using System;
using System.Threading.Tasks;

namespace wlpm.Repository.Provider
{
    public class Github: RepositoryProviderInterface
    {
        public bool IdentifyURL(string repositoryUrl)
        {
            return repositoryUrl.StartsWith("https://github.com/") || repositoryUrl.StartsWith("https://raw.githubusercontent.com/");
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
                version = "master";
            }
            return "https://github.com/" + user + "/" + repo + "/zipball/" + version;
        }

        
    }
}