using System;
using System.IO;

namespace TagManagement.Tools
{
   public static class Class1
   {
      public static void FormatSourceDataFile(string sourceDir)
      {
         var resultFilePath = sourceDir + string.Format("merged_{0}.csv", DateTime.Now.ToFileTimeUtc());
         var outputFile = File.Create(resultFilePath);
         outputFile.Close();

         var resultFile = new StreamWriter(resultFilePath, true);
         resultFile.WriteLine("id_NUMBER,TITLE,DESCRIPTION");
         foreach (var file in Directory.EnumerateFiles(sourceDir, "*.txt"))
         {
            var fileInfo = new FileInfo(file);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            resultFile.Write(string.Format("{0},{0},", fileName));
            resultFile.Write("\"");
            foreach (var fileLine in File.ReadAllLines(file)) {
               resultFile.Write(string.Format("{0} ", fileLine.TrimEnd().TrimEnd(',')));
            }

            resultFile.WriteLine("\"");
         }

         resultFile.Close();
      }
   }
}
