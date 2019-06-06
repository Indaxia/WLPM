using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace wlpm
{
    public enum DependencyType
    {
        File,
        Package
    }

    public class PackageDependency
    {
        public string Resource { 
            get{ return _resource; }
            set{
                _resource = value.Trim();
                _id = generateId(_resource, _version);
            }
        }
        public DependencyType Type { get; set; }
        public string Version { 
            get{ return _version; }
            set{
                if((value.Contains('*') && value.Length > 1) || value.Contains('^') || value.Contains('+')) {
                    throw new PackageException("Package '" + Resource + "' contains unsupported version syntax, use * or fully qualified version tag");
                }
                _version = value;
                _id = generateId(_resource, _version);
            }
        }
        public bool TopOrder { get; set; }
        public string id { get{ return _id; } }
        public List<string> Sources;

        private string _version;
        private string _resource;
        private string _id;

        public PackageDependency()
        {
            Type = DependencyType.Package;
            Version = "*";
            TopOrder = false;
            Resource = "";
            Sources = new List<string>();
        }

        public PackageDependency(DependencyType type, string resource, string version = "*", bool topOrder = false)
        {
            Type = type;
            Version = version;
            TopOrder = topOrder;
            Resource = resource;
            Sources = new List<string>();
        }

        public PackageDependency(PackageDependency other)
        {
            Type = other.Type;
            Version = other.Version;
            TopOrder = other.TopOrder;
            Resource = other.Resource;
            Sources = other.Sources; // TODO: check if it works
        }

        public PackageDependency(JProperty d, string packageName)
        {
            Type = DependencyType.Package;
            Version = "*";
            TopOrder = false;
            Resource = "";
            Sources = new List<string>();
            if(d.Value.Type == JTokenType.String) {
                Version = (string)d.Value;
                Resource = d.Name;
                Type = DependencyType.Package;
            } else if(d.Value.Type == JTokenType.Object) {
                JToken v = d.Value;
                Resource = v["resource"] == null ? d.Name : (string)v["resource"]; // from lock state
                if(v["version"] != null) {
                    Version = (string)v["version"];
                }
                if(v["type"] != null) {
                    switch((string)v["type"]) {
                        case "file":
                            Type = DependencyType.File;
                            break;
                        case "package":
                            Type = DependencyType.Package;
                            break;
                        default:
                            throw new PackageException("Cannot parse \"" + packageName + "\" package. The value of the property 'dependencies." + Resource + ".type' must be 'file' or 'package'");
                    }
                }
                if(v["topOrder"] != null) {
                    TopOrder = (bool)v["topOrder"];
                }
                if(v["sources"] != null && v["sources"].Type == JTokenType.Array) {
                    foreach(string src in v["sources"]) {
                        Sources.Add(src);
                    }
                }
            } else {
                throw new PackageException("Cannot parse \"" + packageName + "\" package. The value of the property 'dependencies." + d.Name + "' must be a string or object");
            }
        }

        public bool sameAs(PackageDependency another)
        {
            return Resource == another.Resource 
                && Type == another.Type 
                && Version == another.Version 
                && TopOrder == another.TopOrder;
        }

        public JProperty ToJson(bool asState = false)
        {
            if(asState || Type != DependencyType.Package || TopOrder) {
                var resultObj = new JObject(
                    new JProperty("type", Type == DependencyType.Package ? "package" : "file"),
                    new JProperty("topOrder", TopOrder)
                );
                if(Type == DependencyType.Package) {
                    resultObj.Add(new JProperty("version", Version));
                }
                if(asState) {
                    resultObj.Add(new JProperty("resource", Resource));
                    JArray sourcesArr = new JArray();
                    foreach(var s in Sources) {
                        sourcesArr.Add(s);
                    }
                    resultObj.Add(new JProperty("sources", sourcesArr));
                }
                return new JProperty(asState ? id : Resource, resultObj);
            }
            return new JProperty(Resource, (string)Version);
        }

        public static string generateId(string resourceUrl, string version)
        {
            resourceUrl += "--" + version;

            StringBuilder sb = new StringBuilder (resourceUrl.Replace('.', '_'));

            sb
                .Replace("https", "")
                .Replace("http","")
                .Replace("://", "")
                .Replace('/', '.')
                .Replace('>', '_')
                .Replace('<', '_')
                .Replace(':', '_')
                .Replace('\\', '_')
                .Replace('|', '_')
                .Replace('?', '_')
                .Replace('*', '_')
                .Replace('\t', '_')
                .Replace(' ', '_')
                .Replace('\0', '_')
            ;
            string result = sb.ToString();


            if(result.EndsWith('.')) {
                result += "_";
            }
            return result;
        }
    }
}