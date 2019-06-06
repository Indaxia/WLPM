using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wlpm
{
    public class Package
    {
        public const string DefaultTarget = "war3map.lua";
        public const string DefaultSourceExtensions = "*.lua";

        public string Title { get; set; }
        public string Author { get; set; }  
        public string License { get; set; }
        public List<PackageDependency> Dependencies { get; set; }
        public List<string> Sources { get; set; }
        public string Target { get; set; }
        public string AfterBuild { get; set; }
        public bool InsertModuleLoader { get; set; }
        public List<string> AllowHosts { get; set; }
        public string SourceExtensions { get; set; }
        
        public Package()
        {
            initDefaults();
        } 

        public Package(string jsonStr)   
        {
            initDefaults();
            fromJson(jsonStr);
        }

        private void initDefaults()
        {
            Title = "project";
            Author = "";
            License = "";
            Dependencies = new List<PackageDependency>();
            Sources = new List<string>();
            Target = DefaultTarget;
            AfterBuild = "";
            InsertModuleLoader = true;
            SourceExtensions = DefaultSourceExtensions;
            AllowHosts = new List<string>();
        }

        private void fromJson(string jsonStr)
        {
            JObject json = JObject.Parse(jsonStr);

            if(json["title"] != null) {
                Title = (string)json["title"];
            }
            if(json["author"] != null) {
                Author = (string)json["author"];
            }
            if(json["license"] != null) {
                License = (string)json["license"];
            }
            if(json["target"] != null) {
                Target = (string)json["target"];
            }
            if(json["afterBuild"] != null) {
                AfterBuild = (string)json["afterBuild"];
            }
            if(json["sourceExtensions"] != null) {
                SourceExtensions = (string)json["sourceExtensions"];
            }
            if(json["insertModuleLoader"] != null) {
                InsertModuleLoader = (bool)json["insertModuleLoader"];
            }
            if(json["dependencies"] != null) {
                if(json["dependencies"].Type != JTokenType.Object) {
                    throw new PackageException("Cannot parse \"" + Title + "\" package. The value of the property 'dependencies' must be an object, if exists");
                }
                foreach(JProperty d in json["dependencies"]) {
                    Dependencies.Add(new PackageDependency(d, Title));
                }
            }
            if(json["allowHosts"] != null) {
                if(json["allowHosts"].Type != JTokenType.Array) {
                    throw new PackageException("Cannot parse \"" + Title + "\" package. The value of the property 'allowHosts' must be an array, if exists");
                }
                foreach(string h in json["allowHosts"]) {
                    AllowHosts.Add(h);
                }
            }
            if(json["sources"] != null && json["sources"].Type == JTokenType.Array) {
                foreach(string src in json["sources"]) {
                    Sources.Add(src);
                }
            }
        }

        public JObject ToJson()
        {
            var result = new JObject();
            if(Title.Length > 0) {
                result.Add(new JProperty("title", Title));
            }
            if(Author.Length > 0) {
                result.Add(new JProperty("author", Author));
            }
            if(License.Length > 0) {
                result.Add(new JProperty("license", License));
            }
            var deps = new JObject();
            foreach(PackageDependency dep in Dependencies) {
                deps.Add(dep.ToJson());
            }
            result.Add(new JProperty("dependencies", deps));
            if(Sources.Count > 0) {
                result.Add(new JProperty("sources", new JArray(Sources.ToArray())));
            }
            if(Target.Length > 0 && Target != DefaultTarget) {
                result.Add(new JProperty("target", Target));
            }
            if(AfterBuild.Length > 0) {
                result.Add(new JProperty("afterBuild", AfterBuild));
            }
            if(!InsertModuleLoader) {
                result.Add(new JProperty("insertModuleLoader", false));
            }
            if(SourceExtensions.Length > 0 && SourceExtensions != DefaultSourceExtensions) {
                result.Add(new JProperty("sourceExtensions", SourceExtensions));
            }
            if(AllowHosts.Count > 0) {
                result.Add(new JProperty("allowHosts", AllowHosts.ToArray()));
            }
            return result;
        }

        public static string getDefaultConfiguration()
        {
            return "{\n  \"title\": \"project\",\n  \"dependencies\": {}\n}";
        }
    }
}