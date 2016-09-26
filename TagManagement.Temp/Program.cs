using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagManagement.Temp
{
   class Program
   {
      static void Main(string[] args)
      {
         var input = File.ReadAllLines(@"C:\Users\Nick\Desktop\temp.csv");
         var outputPath = string.Format(@"D:\temp\output_{0}.txt", DateTime.Now.ToFileTimeUtc());
         File.Create(outputPath).Close();
         var resultList = new List<Item>();
         foreach(var line in input)
         {
            var splitted = line.Split(',');
            var newItem = new Item { ID = int.Parse(splitted[0]), Value = splitted[1] };
            resultList.Add(newItem);
         }

         var groupedList = resultList.GroupBy(x => x.ID, x => x.Value, (key, g) => new { ID = key, Items = g.ToList() });
         var file = new StreamWriter(outputPath, true);
         
         foreach (var group in groupedList){
            var itemString = string.Join(" ", group.Items.Select(x => x.Replace("\"","\"\"")).ToArray());
            file.WriteLine("{0},\"{1}\"", group.ID, itemString);
         }

         file.Flush();
         file.Close();

         Console.ReadKey();
      }
   }

   class Item
   {
      public int ID { get; set; }
      public string Value { get; set; }
   }
}
