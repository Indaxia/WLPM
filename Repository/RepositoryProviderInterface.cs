using System.Threading.Tasks;

namespace wlpm.Repository
{
    public interface RepositoryProviderInterface
    {
        bool IdentifyURL(string repositoryUrl);
        string GetZipFileUrl(string repositoryUrl, string version);
    }
}