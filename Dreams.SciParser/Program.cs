using System.Linq;
using System.Net;
using Mono.Options;

namespace Dreams.SciParser
{
    public class Program
    {
        static async Task Main(string[] args)
        {           
            string storage = @"storage";
            string website = "https://sci-hub.wf";
            string publicationsPath = "doi.txt";
            var OptionSet = new OptionSet
            {
                {"p|publications=", "Список публикаций", c => publicationsPath = c},
                {"s|storage=", "Папка для хранения публикаций", c => storage = c},
                {"w|website=", "website of sci-hub", c => website = c}
            };
            OptionSet.Parse(args);

            if (!Directory.Exists(storage))
            {
                Directory.CreateDirectory(storage);
            }
            if (File.Exists(publicationsPath))
            {
                using (StreamReader sr = new(publicationsPath))
                {
                    string doi;
                    while ((doi = sr.ReadLine()) != null)
                    {
                        var publication = ParsePublication(website, doi);
                        if (publication is not null)
                        {
                            File.WriteAllBytes($"{storage}/{doi.Replace('/', '_')}.pdf", publication);
                        }
                        Thread.Sleep(1000);
                    }
                }

            }
        }

        public static string ParseUri(string line)
        {
            string pattern = "src=";
            int indexFirst = line.IndexOf(pattern);
            int indexLast = line.IndexOf('#', indexFirst);
            return line.Substring(indexFirst + pattern.Length + 1, indexLast - indexFirst - pattern.Length - 1);
        }

        public static byte[]? ParsePublication(string website, string doi)
        {
            WebClient webclient = new();
            using (Stream stream = webclient.OpenRead($"{website}/{doi}"))
            {
                using (StreamReader reader = new(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains(@"id = ""pdf"">"))
                        {
                            string downloadUrl = ParseUri(line);
                            if (!downloadUrl.StartsWith("//"))
                            {
                                downloadUrl = $"{website}{downloadUrl}";
                            }
                            else
                            {
                                downloadUrl = $"https:{downloadUrl}";
                            }
                            try
                            {
                                //webclient.DownloadFile($"{downloadUrl}?download=true", $"{storage}/{doi.Replace('/', '_')}.pdf");
                                var data = webclient.DownloadData($"{downloadUrl}?download=true");
                                Console.WriteLine("Скачано: " + doi);
                                return data;
                            }
                            catch
                            {
                                Console.WriteLine("Ошибка: " + doi);
                            }
                        }
                        if (line.Contains("статья не найдена"))
                        {
                            Console.WriteLine("Не найдена: " + doi);
                            return null;
                        }
                    }
                }
            }
            return null;
        }
    }  
}