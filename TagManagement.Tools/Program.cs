using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TagManagement.Api.Controllers;
using TagManagement.Api.Models;
using TagManagement.Api.Processors;

namespace TagManagement.Tools
{
   class Program
   {
      private static string filepath;

      static void Main()
      {
         
         DoSomething();
         Class1.FormatSourceDataFile(@"D:\temp\");
         Console.WriteLine();
         Console.Write("Press any key to exit..");
         Console.ReadKey();
      }

      static void DoSomething()
      {
         filepath = string.Format(@"D:\temp\output_{0}.txt", DateTime.Now.ToFileTimeUtc());
         var newFile = File.Create(filepath);
         newFile.Close();
         var processor = new TagProcessor();

         var mediaItemIdList = processor.GetMediaItems(false).Select(m => new { Key = m.ID, Value = m.ExternalID }).ToList();
         //mediaItemIdList.Clear();
         //mediaItemIdList.Add(new { Key = 597, Value = 18704 });
         var watch = new Stopwatch();
         watch.Start();
         
         foreach (var mediaItemID in mediaItemIdList)
         {
            Console.Write("{0:0000000000}\t", watch.ElapsedMilliseconds / 1000);
            Console.Write(mediaItemID.Key); 
            Console.Write("\t");
            var result = processor.ProcessAgain(mediaItemID.Key);
            var file = new StreamWriter(filepath, true);
            file.WriteLine(string.Format("#{0}", mediaItemID.Value));
            File.WriteAllText(string.Format(@"D:\temp\{0}.txt", mediaItemID.Value), String.Join(", \n", result.ToArray()));
            Console.WriteLine(" - {0}",result.Count());
            file.Close();
            //GetProcessResult(item.Key, item.Value).Wait();
         }

         
      }

      static async Task<Dictionary<int,int>> GetMediaItemIdList()
      {
         var resultValue = new Dictionary<int, int>();
         using (var client = new HttpClient())
         {
            client.Timeout = new TimeSpan(0,5,0);
            var response = await client.GetAsync("http://localhost:9001/api/mediaitems");
            if (response.IsSuccessStatusCode)
            {
               var x = await response.Content.ReadAsStringAsync();
               var resultData = JsonConvert.DeserializeObject<List<dynamic>>(x).ToList();
               resultValue = resultData.Select(r => new { ID = (int)r.ID, ExternalID = (int)r.ExternalID }).ToDictionary(r => r.ID, r=> r.ExternalID);
            }
         }

         return resultValue;
      }

      static async Task<List<string>> GetProcessResult(int mediaItemID, int externalID)
      {
         var file = new StreamWriter(filepath, true);
         file.WriteLine(string.Format("#{0}", externalID));
         var resultValue = new List<string>();
         using (var client = new HttpClient())
         {
            var response = await client.GetAsync(string.Format("http://localhost:9001/api/process/{0}", mediaItemID));
            if (response.IsSuccessStatusCode)
            {

               file.WriteLine();
               var resultString = await response.Content.ReadAsStringAsync();
               resultValue = JsonConvert.DeserializeObject<List<string>>(resultString).ToList();
               File.WriteAllText(string.Format(@"D:\temp\{0}.txt", mediaItemID), resultString);
               Console.WriteLine(resultValue.Count());
            }
         }
         file.Flush();
         file.Close();

         return resultValue;
      }
   }
}
