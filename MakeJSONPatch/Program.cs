using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonDiffPatchDotNet;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using System.Linq;

namespace MakeJSONPatch
{
    public class Program
    {
        public struct Config
        {
            public string Name;
            public string DB_Old;
            public string DB_New;
            public string Indexes;
            public string RepoName;
            public string Branch;
        }
        public static void Main(string[] args)
        {
            ConvertJson(args[0]);
        }
        public static void ConvertJson(string cn)
        {
            var conf = JArray.Parse(File.ReadAllText("config.json")).ToObject<List<Config>>();
            var jsed = new JsonDiffPatch(new Options()
            {
                ArrayDiff = ArrayDiffMode.Efficient,
                TextDiff = TextDiffMode.Efficient,
                DiffBehaviors = DiffBehavior.None,
                ExcludePaths = new List<string>() { "/DBPatchVer" }
            });
            var cfg = conf.Where(x => x.Name == cn).First();
            var DBOld = JObject.Parse(File.ReadAllText(cfg.DB_Old));
            var DBConv = JObject.Parse(File.ReadAllText(cfg.DB_New));
            var patchIndex = JArray.Parse(File.ReadAllText(Path.Combine(cfg.Indexes, "index.json")));
            var Formatter = new JsonDeltaFormatter();
            var diff = Formatter.Format(jsed.Diff(DBOld, DBConv));
            File.WriteAllText(cfg.DB_Old, JsonConvert.SerializeObject(DBConv, Formatting.Indented));
            File.WriteAllText(Path.Combine(cfg.Indexes, $"{patchIndex.Count}.json"), JsonConvert.SerializeObject(diff, Formatting.Indented, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            }));
            patchIndex.Add($"https://raw.githubusercontent.com/{cfg.RepoName}/{cfg.Branch}/{cfg.Name}/{patchIndex.Count}.json");
            DBConv["DBPatchVer"] = patchIndex.Count + 1;
            File.WriteAllText(cfg.DB_New, JsonConvert.SerializeObject(DBConv, Formatting.Indented));
            File.WriteAllText(Path.Combine(cfg.Indexes, "index.json"), JsonConvert.SerializeObject(patchIndex, Formatting.Indented));
        }
    }
}