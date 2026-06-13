using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrugiProjekatSisProg
{
    internal class Algoritam
    {
        public async static Task<string> StartAsync(string fileName)
        {
            try
            {
                string rootPath = Path.Combine(
                     Directory.GetCurrentDirectory(),
                     "..", "..", "..", "root");

                string[] files = Directory.GetFiles(
                    rootPath,
                    fileName,
                    SearchOption.AllDirectories);

                if (files.Length == 0)
                {
                    return ("Greška: fajl ne postoji. " + rootPath);

                }

                string filePath = files[0];
                string content = await File.ReadAllTextAsync(filePath);

                if (string.IsNullOrWhiteSpace(content))
                {
                    return ("Fajl je prazan.");

                }

                int count = content.Count(char.IsPunctuation);
                /*  foreach (char c in content)
                  {
                      if (char.IsPunctuation(c))
                      {
                          Console.WriteLine($"'{c}' -> {(int)c}");
                      }
                  }*/

                return $"Broj znakova interpunkcije: {count}";

            }
            catch (Exception ex)
            {
                return (ex.Message);

            }
        }
    }
}
