using Common;
using Entities.DTO;
using Entities.Logging;
using Entities.Types;
using System.IO.Compression;

namespace Simulysis.Helpers
{
    public class MdlExtendedReader : SlxReader
    {
        private KeyValuePair<string, string> SplitLine(string line)
        {
            string[] pair = line.Split(new char[] { ' ', '\t' }, 2);

            return new KeyValuePair<string, string>(pair[0], pair[1].Trim().Trim('"'));
        }

        private string GetVersionName(string[] lines, ref long i)
        {
            while (true)
            {
                string parsedLn = lines[i++].Trim().ToLower();

                if (parsedLn.StartsWith("version"))
                {
                    var pair = SplitLine(parsedLn);
                    return  pair.Value;
                }

                if (parsedLn.Equals("system {"))
                {
                    throw new Exception("Mdl file does not contains version");
                }
            }
        }

        public static bool IsMdlExtendedFile(string fileName)
        {
            using (TextReader reader = new StreamReader(fileName))
            {
                string? line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("__MWOPC_PACKAGE_BEGIN__"))
                    {
                        return true;
                    }

                    if (line.Contains("system {", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return false;
            }
        }

        public override long Read(
            string filePath,
            long projectId,
            string projectFullPath,
            string systemLevel,
            string description,
            List<FileContent> fileContentList,
            IEnumerable<string> filePaths
        )
        {
            fileName = Path.GetFileName(filePath);
            Loggers.SVP.Info($"{fileName}: Reading {fileName}");

            var extractFolder = Directory.CreateDirectory(Path.Combine(projectFullPath, Constants.SLX_EXTRACT_FOLDER));
            var desFolder = Directory.CreateDirectory(Path.Combine(extractFolder.FullName, Path.GetFileNameWithoutExtension(fileName)));

            var lines = File.ReadAllLines(filePath);

            TextWriter currentFileWriter = null;

            foreach (var line in lines)
            {
                if (line.Contains("__MWOPC_PART_BEGIN__"))
                {
                    if (currentFileWriter != null)
                    {
                        currentFileWriter.Close();
                    }

                    var parts = SplitLine(line);
                    var path = parts.Value;

                    if (path.StartsWith('/') || path.StartsWith('\\'))
                    {
                        path = path.Substring(1);
                    }

                    path = Path.Combine(desFolder.FullName, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    currentFileWriter = new StreamWriter(path);
                }
                else
                {
                    if (currentFileWriter != null)
                    {
                        currentFileWriter.WriteLine(line);
                    }
                }
            }

            if (currentFileWriter != null)
            {
                currentFileWriter.Close();
            }

            long i = 0;
            return ReadCore(filePath, projectId, projectFullPath, systemLevel, description, desFolder.FullName, fileContentList, filePaths, GetVersionName(lines, ref i));
        }
    }
}
