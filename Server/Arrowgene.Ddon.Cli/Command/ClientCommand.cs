using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Arrowgene.Ddon.Client;
using Arrowgene.Ddon.Client.Resource;
using Arrowgene.Ddon.Client.Resource.Texture;
using Arrowgene.Ddon.Client.Resource.Texture.Dds;
using Arrowgene.Ddon.Client.Resource.Texture.Tex;
using Arrowgene.Ddon.Shared.Csv;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.Cli.Command
{
    public class ClientCommand : ICommand
    {
        private static readonly ILogger Logger = LogProvider.Logger<Logger>(typeof(ClientCommand));

        public string Key => "client";

        public string Description => "Usage: `client \"E:\\Games\\Dragon's Dogma Online\\nativePC\\rom\"`";


        public CommandResultType Run(CommandParameter parameter)
        {
            if (parameter.ArgumentMap.ContainsKey("extract"))
            {
                string extract = parameter.ArgumentMap["extract"];
                extract = extract.TrimEnd(Path.DirectorySeparatorChar);
                extract = extract.TrimEnd(Path.AltDirectorySeparatorChar);
                DirectoryInfo outDirectory = new DirectoryInfo(extract);
                if (!outDirectory.Exists)
                {
                    Logger.Error($"extract directory does not exists ({outDirectory.FullName})");
                    return CommandResultType.Exit;
                }

                string source = parameter.Arguments[0];
                if (File.Exists(source))
                {
                    FileInfo arcFile = new FileInfo(source);
                    if (arcFile.Extension != ".arc")
                    {
                        Logger.Error($"Source file is not a .arc file. ({source})");
                        return CommandResultType.Exit;
                    }

                    Extract(arcFile, outDirectory);
                    return CommandResultType.Exit;
                }

                if (Directory.Exists(source))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(source);
                    Extract(directoryInfo, outDirectory);
                    return CommandResultType.Exit;
                }

                Logger.Error($"Source file or folder does not exist. ({outDirectory.FullName})");
                return CommandResultType.Exit;
            }

            if (parameter.ArgumentMap.ContainsKey("recommend"))
            {
                if (parameter.Arguments.Count < 1)
                {
                    Logger.Error("Usage: client <romDir> recommend=<outFile>");
                    return CommandResultType.Exit;
                }

                DirectoryInfo romDir = new DirectoryInfo(parameter.Arguments[0]);
                if (!romDir.Exists)
                {
                    Logger.Error($"Rom Path Invalid ({romDir.FullName})");
                    return CommandResultType.Exit;
                }

                ExportRecommendedLevels(romDir, parameter.ArgumentMap["recommend"]);
                return CommandResultType.Exit;
            }

            if (parameter.ArgumentMap.ContainsKey("starterwarps"))
            {
                if (parameter.Arguments.Count < 1)
                {
                    Logger.Error("Usage: client <romDir> starterwarps=<outFile> [maxLevel=20]");
                    return CommandResultType.Exit;
                }

                DirectoryInfo romDir = new DirectoryInfo(parameter.Arguments[0]);
                if (!romDir.Exists)
                {
                    Logger.Error($"Rom Path Invalid ({romDir.FullName})");
                    return CommandResultType.Exit;
                }

                uint maxLevel = 20;
                if (parameter.ArgumentMap.TryGetValue("maxLevel", out string maxLevelStr)
                    && !uint.TryParse(maxLevelStr, out maxLevel))
                {
                    Logger.Error($"Invalid maxLevel: {maxLevelStr}");
                    return CommandResultType.Exit;
                }

                ExportStarterLevelSyncWarpPoints(romDir, parameter.ArgumentMap["starterwarps"], maxLevel);
                return CommandResultType.Exit;
            }

            if (parameter.Arguments[0] == "gmd2Csv" &&
                parameter.ArgumentMap.ContainsKey("gmdCsv") &&
                parameter.ArgumentMap.ContainsKey("romDir") &&
                parameter.ArgumentMap.ContainsKey("romLang"))
            {
                // Merge all .arc files of ROM dir into existing CSVs language column

                string gmdCsvArg = parameter.ArgumentMap["gmdCsv"];
                string romDirArg = parameter.ArgumentMap["romDir"];
                string romLangArg = parameter.ArgumentMap["romLang"];

                if (!Enum.TryParse<GuiMessage.Language>(romLangArg, out GuiMessage.Language romLanguage))
                {
                    Logger.Error($"Provided romLang:{romLangArg} is invalid");
                    return CommandResultType.Exit;
                }

                DirectoryInfo romDir = new DirectoryInfo(romDirArg);
                FileInfo gmdCsvFile = new FileInfo(gmdCsvArg);
                if (!romDir.Exists)
                {
                    Logger.Error($"Provided romDir:{romDirArg} does not exist");
                    return CommandResultType.Exit;
                }

                GmdCsv gmdCsvReader = new GmdCsv();
                List<GmdCsv.Entry> csvEntries = gmdCsvReader.ReadPath(gmdCsvFile.FullName);
                Dictionary<string, GmdCsv.Entry> csvIndex = new Dictionary<string, GmdCsv.Entry>();
                foreach (GmdCsv.Entry csvEntry in csvEntries)
                {
                    string hash = csvEntry.GetUniqueQualifierLanguageAgnostic();
                    if (!csvIndex.ContainsKey(hash))
                    {
                        csvIndex.Add(hash, csvEntry);
                    }
                    else
                    {
                        GmdCsv.Entry existingRomCsvEntry = csvIndex[hash];
                        Logger.Error($"Duplicate entry in CSV, dropping");
                    }
                }

                string[] files = Directory.GetFiles(romDir.FullName, "*.arc", SearchOption.AllDirectories);
                List<GmdCsv.Entry> romCsvEntries = new List<GmdCsv.Entry>();
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo arcFile = new FileInfo(files[i]);
                    romCsvEntries.AddRange(ExtractGmdCsvEntries(arcFile, romLanguage));
                    Logger.Info($"Processing {i}/{files.Length} {arcFile.FullName}");
                }

                List<GmdCsv.Entry> resultCsvEntries = new List<GmdCsv.Entry>();
                foreach (GmdCsv.Entry romCsvEntry in romCsvEntries)
                {
                    string hash = romCsvEntry.GetUniqueQualifierLanguageAgnostic();
                    GmdCsv.Entry resultEntry;
                    if (csvIndex.ContainsKey(hash))
                    {
                        resultEntry = csvIndex[hash];
                        MergeGmdCsvLanguage(romCsvEntry, romLanguage, resultEntry);
                    }
                    else
                    {
                        resultEntry = romCsvEntry;
                    }

                    resultCsvEntries.Add(resultEntry);
                }

                string outPath = Path.Combine(gmdCsvFile.FullName);
                gmdCsvReader.WritePath(resultCsvEntries, outPath);
                Logger.Info($"Done: {outPath}");
                return CommandResultType.Exit;
            }


            if (parameter.Arguments[0] == "packGmd" &&
                parameter.ArgumentMap.ContainsKey("gmdCsv") &&
                parameter.ArgumentMap.ContainsKey("romDir") &&
                parameter.ArgumentMap.ContainsKey("romLang"))
            {
                try
                {
                    GmdCsv gmdCsvReader = new GmdCsv();
                    List<GmdCsv.Entry> gmdCsvEntries = gmdCsvReader.ReadPath(parameter.ArgumentMap["gmdCsv"]);
                    GmdActions.Pack(gmdCsvEntries, parameter.ArgumentMap["romDir"], parameter.ArgumentMap["romLang"]);
                } 
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
                return CommandResultType.Exit;
            }


            if (parameter.Arguments.Count < 1)
            {
                Logger.Error($"To few arguments. {Description}");
                return CommandResultType.Exit;
            }

            FileInfo fileInfo = new FileInfo(parameter.Arguments[0]);
            if (fileInfo.Exists)
            {
                if (".tex".Equals(fileInfo.Extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    TexToDds(fileInfo);
                    return CommandResultType.Exit;
                }

                if (".dds".Equals(fileInfo.Extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    DdsToTex(fileInfo, parameter);
                    return CommandResultType.Exit;
                }

                Logger.Error("No available action for provided file");
                return CommandResultType.Exit;
            }

            // assuming first parameter is rom dir for these actions
            DirectoryInfo romDirectory = new DirectoryInfo(parameter.Arguments[0]);
            if (!romDirectory.Exists)
            {
                Logger.Error("Rom Path Invalid");
                return CommandResultType.Exit;
            }

            if (parameter.ArgumentMap.ContainsKey("dump"))
            {
                DirectoryInfo outDirectory = new DirectoryInfo(parameter.ArgumentMap["dump"]);
                DumpPaths(romDirectory, outDirectory);
                return CommandResultType.Exit;
            }

            if (parameter.ArgumentMap.ContainsKey("export"))
            {
                DirectoryInfo outDirectory = new DirectoryInfo(parameter.ArgumentMap["export"]);
                ExportResourceRepository(romDirectory, outDirectory);
                return CommandResultType.Exit;
            }

            return CommandResultType.Exit;
        }

        /// <summary>
        /// Reads the client's stage list (rStageList) and emits a baked C# lookup table of
        /// StageNo -> RecommendLevel (only stages whose recommended level is non-zero) so the game server
        /// can apply level sync without shipping/parsing client resources at runtime.
        /// Usage: client "&lt;romDir&gt;" recommend="&lt;path-to-StageRecommendedLevelTable.cs&gt;"
        /// </summary>
        public void ExportRecommendedLevels(DirectoryInfo romDirectory, string outPath)
        {
            StageList stageList = ArcArchive.GetResource<StageList>(romDirectory, "base.arc", "scr/stage_list");
            if (stageList == null)
            {
                Logger.Error("Failed to load scr/stage_list from base.arc");
                return;
            }

            SortedDictionary<uint, byte> levels = new SortedDictionary<uint, byte>();
            foreach (StageList.Info info in stageList.StageInfos)
            {
                if (info.RecommendLevel > 0 && !levels.ContainsKey(info.StageNo))
                {
                    levels.Add(info.StageNo, info.RecommendLevel);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Arrowgene.Ddon.Shared.Model");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// AUTO-GENERATED from the client's rStageList resource (base.arc -> scr/stage_list).");
            sb.AppendLine("    /// Do not edit by hand; regenerate via: client \"<romDir>\" recommend=\"<thisFile>\".");
            sb.AppendLine("    /// Maps StageNo to the zone's recommended level. Only stages with a non-zero recommended");
            sb.AppendLine("    /// level are listed; open-world fields and towns have no recommended level and are absent.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class StageRecommendedLevelTable");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly IReadOnlyDictionary<uint, byte> ByStageNo = new Dictionary<uint, byte>");
            sb.AppendLine("        {");
            foreach (var kvp in levels)
            {
                sb.AppendLine($"            {{ {kvp.Key}, {kvp.Value} }},");
            }
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(outPath, sb.ToString());
            Logger.Info($"Total stages: {stageList.StageInfos.Count}, with RecommendLevel>0: {levels.Count}");
            Logger.Info($"Done: {outPath}");
        }

        /// <summary>
        /// Reads the client's warp location list and stage/area data, then emits a baked C# list of
        /// warp point IDs for areas with recommended level &lt;= <paramref name="maxRecommendedLevel"/>.
        /// Includes teleports directly to those dungeons and to/near their containing field areas
        /// (same AreaId), but excludes destinations above the level cap.
        /// Usage: client "&lt;romDir&gt;" starterwarps="&lt;path-to-StarterLevelSyncWarpPointTable.cs&gt;" [maxLevel=20]
        /// </summary>
        public void ExportStarterLevelSyncWarpPoints(DirectoryInfo romDirectory, string outPath, uint maxRecommendedLevel)
        {
            StageList stageList = ArcArchive.GetResource<StageList>(romDirectory, "base.arc", "scr/stage_list");
            AreaStageList areaStageList = ArcArchive.GetResource<AreaStageList>(romDirectory, "base.arc", "scr/area_stage_list");
            WarpLocationList warpLocationList = ArcArchive.GetResource<WarpLocationList>(romDirectory, "ui/gui_cmn.arc", "ui/03_warp/warpLocationList", "wal");
            if (stageList == null || areaStageList == null || warpLocationList == null)
            {
                Logger.Error("Failed to load stage list, area stage list, or warp location list from client ROM");
                return;
            }

            Dictionary<uint, byte> recommendByStageNo = new Dictionary<uint, byte>();
            foreach (StageList.Info info in stageList.StageInfos)
            {
                if (info.RecommendLevel > 0 && !recommendByStageNo.ContainsKey(info.StageNo))
                {
                    recommendByStageNo.Add(info.StageNo, info.RecommendLevel);
                }
            }

            Dictionary<uint, uint> areaIdByStageNo = new Dictionary<uint, uint>();
            foreach (AreaStageList.AreaInfoStage ais in areaStageList.AreaInfoStages)
            {
                if (!areaIdByStageNo.ContainsKey(ais.StageNo))
                {
                    areaIdByStageNo.Add(ais.StageNo, ais.AreaId);
                }
            }

            HashSet<uint> earlyAreaIds = new HashSet<uint>();
            foreach (var kvp in recommendByStageNo)
            {
                if (kvp.Value >= 1 && kvp.Value <= maxRecommendedLevel
                    && areaIdByStageNo.TryGetValue(kvp.Key, out uint areaId))
                {
                    earlyAreaIds.Add(areaId);
                }
            }

            HashSet<uint> eligibleStageNos = new HashSet<uint>();
            foreach (var kvp in recommendByStageNo)
            {
                if (kvp.Value >= 1 && kvp.Value <= maxRecommendedLevel)
                {
                    eligibleStageNos.Add(kvp.Key);
                }
            }

            foreach (AreaStageList.AreaInfoStage ais in areaStageList.AreaInfoStages)
            {
                if (!earlyAreaIds.Contains(ais.AreaId))
                {
                    continue;
                }

                byte rec = recommendByStageNo.GetValueOrDefault(ais.StageNo, (byte)0);
                if (rec == 0 || rec <= maxRecommendedLevel)
                {
                    eligibleStageNos.Add(ais.StageNo);
                }
            }

            SortedSet<uint> warpPointIds = new SortedSet<uint> { 1 }; // White Dragon Temple hub
            foreach (WarpLocationList.Entry entry in warpLocationList.Entries)
            {
                if (eligibleStageNos.Contains(entry.StageNo))
                {
                    warpPointIds.Add(entry.Id);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Arrowgene.Ddon.Shared.Model");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// AUTO-GENERATED from the client's warp location list and stage/area data.");
            sb.AppendLine("    /// Do not edit by hand; regenerate via:");
            sb.AppendLine("    ///   client \"<romDir>\" starterwarps=\"<thisFile>\" [maxLevel=20]");
            sb.AppendLine("    ///");
            sb.AppendLine($"    /// Warp points that travel to level-sync destinations (recommended level 1-{maxRecommendedLevel})");
            sb.AppendLine("    /// or to field areas that contain such content. Higher-level destinations remain locked for exploration.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class StarterLevelSyncWarpPointTable");
            sb.AppendLine("    {");
            sb.AppendLine($"        public const uint MaxRecommendedLevel = {maxRecommendedLevel};");
            sb.AppendLine();
            sb.AppendLine("        public static readonly IReadOnlyList<uint> WarpPointIds = new uint[]");
            sb.AppendLine("        {");
            foreach (uint id in warpPointIds)
            {
                sb.AppendLine($"            {id},");
            }
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(outPath, sb.ToString());
            Logger.Info($"Early field areas: {earlyAreaIds.Count}, eligible stage nos: {eligibleStageNos.Count}, warp points: {warpPointIds.Count}");
            Logger.Info($"Done: {outPath}");
        }

        public void ExportResourceRepository(DirectoryInfo romDirectory, DirectoryInfo outDir)
        {
            ClientResourceRepository repo = new ClientResourceRepository();
            repo.Load(romDirectory);
            string json = JsonSerializer.Serialize(repo);
            string outPath = Path.Combine(outDir.FullName, "repo.json");
            File.WriteAllText(outPath, json);
            Logger.Info($"Done: {outPath}");
        }

        public void DumpPaths(DirectoryInfo romDirectory, DirectoryInfo outDir)
        {
            if (outDir == null)
            {
                Logger.Error("Failed to dump paths. (outDir == null)");
                return;
            }

            if (!outDir.Exists)
            {
                outDir.Create();
                Logger.Info($"Created Dir: {outDir.FullName}");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"Path,ArcPath,Ext,JamCrcStr,Class,JamCrc,Size,SizeCompress,Offset{Environment.NewLine}");
            string[] files = Directory.GetFiles(romDirectory.FullName, "*.arc", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string relativePath = filePath.Substring(romDirectory.FullName.Length);
                ArcArchive archive = new ArcArchive();
                archive.Open(filePath);
                foreach (ArcArchive.ArcFile file in archive.GetFiles())
                {
                    ArcArchive.FileIndex fi = file.Index;
                    sb.Append($"{relativePath},{fi.ArcPath}.{fi.ArcExt.Extension},{fi.ArcExt.Extension},");
                    sb.Append($"{fi.ArcExt.JamCrcStr},{fi.ArcExt.Class},{fi.JamCrc},");
                    sb.Append($"{fi.Size},{fi.CompressedSize},{fi.Offset}{Environment.NewLine}");
                }

                Logger.Info($"Processing {i}/{files.Length} {filePath}");
            }

            string outPath = Path.Combine(outDir.FullName, "dump.csv");
            File.WriteAllText(outPath, sb.ToString());
            Logger.Info($"Done: {outPath}");
        }

        public void Extract(DirectoryInfo source, DirectoryInfo outDir)
        {
            if (outDir == null)
            {
                Logger.Error("Failed to extract. (outDir == null)");
                return;
            }

            if (!outDir.Exists)
            {
                outDir.Create();
                Logger.Info($"Created Output Directory: {outDir.FullName}");
            }

            string[] files = Directory.GetFiles(source.FullName, "*.arc", SearchOption.AllDirectories);

            if (files.Length <= 0)
            {
                Logger.Error($"No *.arc files found in source directory. ({source.FullName})");
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                FileInfo fi = new FileInfo(filePath);
                Extract(fi, outDir);
                Logger.Info($"Processing {i}/{files.Length} {filePath}");
            }
        }

        public void Extract(FileInfo fileInfo, DirectoryInfo outDir)
        {
            ArcArchive archive = new ArcArchive();
            archive.Open(fileInfo.FullName);
            foreach (ArcArchive.ArcFile af in archive.GetFiles())
            {
                ArcArchive.FileIndex fi = af.Index;
                string outDirectory = Path.Combine(outDir.FullName, fileInfo.Name, fi.Directory);
                if (!Directory.Exists(outDirectory))
                {
                    Directory.CreateDirectory(outDirectory);
                }

                string outPath = Path.Combine(outDirectory, fi.Name);
                File.WriteAllBytes(outPath, af.Data);

                Logger.Info($"Written: {outPath}");
            }
        }

        public void TexToDds(FileInfo fileInfo)
        {
            TexTexture texTexture = new TexTexture();
            texTexture.Open(fileInfo.FullName);
            DdsTexture ddsTexture = TexConvert.ToDdsTexture(texTexture);
            string outPath = Path.Combine(Path.GetDirectoryName(fileInfo.FullName), Path.GetFileNameWithoutExtension(fileInfo.FullName)+".dds");
            ddsTexture.Save(outPath);
            Logger.Info($"Written: {outPath}");

            // Dump original TEX header to .txt file
            string headerPath = Path.Combine(Path.GetDirectoryName(fileInfo.FullName), Path.GetFileNameWithoutExtension(fileInfo.FullName)+".txt");
            string headerDump = TexConvert.DumpTexHeader(texTexture.Header);
            File.WriteAllText(headerPath, headerDump);
            Logger.Info($"Written TEX headers: {headerPath}");
        }

        public void DdsToTex(FileInfo fileInfo, CommandParameter parameter)
        {
            // Read original TEX header from .txt file
            FileInfo headerDumpFile = new FileInfo(Path.Combine(Path.GetDirectoryName(fileInfo.FullName), Path.GetFileNameWithoutExtension(fileInfo.FullName)+".txt"));
            if (!headerDumpFile.Exists)
            {
                Logger.Error($"Original TEX headers file not found: {fileInfo.FullName}");
                return;
            }
            string texHeaderDump = File.ReadAllText(headerDumpFile.FullName);
            TexHeader originalTexHeader = TexConvert.ReadTexHeaderDump(texHeaderDump);

            DdsTexture ddsTexture = new DdsTexture();
            ddsTexture.Open(fileInfo.FullName);
            TexTexture texTexture = TexConvert.ToTexTexture(ddsTexture, originalTexHeader);
            string outPath = Path.Combine(Path.GetDirectoryName(fileInfo.FullName), Path.GetFileNameWithoutExtension(fileInfo.FullName)+".tex");
            texTexture.Save(outPath);
            Logger.Info($"Written: {outPath}");
        }

        private void MergeGmdCsvLanguage(GmdCsv.Entry entrySrc, GuiMessage.Language langSrc, GmdCsv.Entry entryDst)
        {
            switch (langSrc)
            {
                case GuiMessage.Language.Japanese:
                    entryDst.MsgJp = entrySrc.MsgJp;
                    break;
                case GuiMessage.Language.English:
                    entryDst.MsgEn = entrySrc.MsgEn;
                    break;
                default:
                    Logger.Error($"Language {langSrc} not supported");
                    break;
            }
        }

        private List<GmdCsv.Entry> ExtractGmdCsvEntries(FileInfo arcFile, GuiMessage.Language language)
        {
            List<GmdCsv.Entry> csvEntries = new List<GmdCsv.Entry>();

            if (!arcFile.Exists || arcFile.Extension != ".arc")
            {
                Logger.Error($"Source file not exists or is not a .arc file. ({arcFile.FullName})");
                return csvEntries;
            }

            List<ArcArchive.ArcFile> gmdFiles;
            using (FileStream fs = new FileStream(arcFile.FullName, FileMode.Open))
            {
                List<ArcArchive.FileIndex> gmdIndices = ArcArchive.IndexProbeStream(fs,
                    ArcArchive.Search().ByExtension("gmd")
                );
                gmdFiles = ArcArchive.ReadFileStream(fs, gmdIndices);
            }

            foreach (ArcArchive.ArcFile gmdFile in gmdFiles)
            {
                GuiMessage gmd = new GuiMessage();
                gmd.Open(gmdFile.Data);
                foreach (GuiMessage.Entry gmdEntry in gmd.Entries)
                {
                    GmdCsv.Entry csvEntry = new GmdCsv.Entry();
                    csvEntry.Index = gmdEntry.Index;
                    csvEntry.Key = gmdEntry.Key;

                    if (language == GuiMessage.Language.Japanese)
                    {
                        csvEntry.MsgJp = gmdEntry.Msg;
                    }
                    else if (language == GuiMessage.Language.English)
                    {
                        csvEntry.MsgEn = gmdEntry.Msg;
                    }
                    else
                    {
                        Logger.Error($"Language {language} not supported");
                    }

                    csvEntry.GmdPath = gmdFile.Index.Path;
                    csvEntry.ArcName = arcFile.Name;
                    csvEntry.ReadIndex = gmdEntry.ReadIndex;
                    string search = "nativePC\\rom";
                    int romIdx = arcFile.FullName.IndexOf(search, StringComparison.InvariantCultureIgnoreCase);
                    if (romIdx == -1)
                    {
                        search = "nativePC/rom";
                        romIdx = arcFile.FullName.IndexOf(search, StringComparison.InvariantCultureIgnoreCase);
                    }

                    if (romIdx >= 0)
                    {
                        csvEntry.ArcPath = arcFile.FullName.Substring(romIdx + search.Length);
                    }

                    csvEntries.Add(csvEntry);
                }
            }

            return csvEntries;
        }

        public void Shutdown()
        {
        }
    }
}
