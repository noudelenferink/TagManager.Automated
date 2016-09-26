namespace TagManagement.Api.Models
{
   using Newtonsoft.Json;
   using Newtonsoft.Json.Converters;
   using System.Collections.Generic;

   public class ProcessedTag
   {
      public int ID { get; set; }
      public string Value { get; set; }
      public List<MediaTag> MediaTags { get; set; }
      public int? DuplicateTagID { get; set; }
      public int? SynonymTagID { get; set; }
      public int? RelatedTagID { get; set; }
      public bool IsDuplicateReference { get; set; }
      public bool IsRelatedReference { get; set; }
      public bool IsSpelledCorrect { get; set; }
      public int Occurences { get; internal set; }
   }
}