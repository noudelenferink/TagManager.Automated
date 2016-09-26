using System.Collections.Generic;
using TagManagement.Api.Models;

namespace TagManagement.Api.Processors
{
   public class ResultGroup
   {
      public int GroupId { get; set; }
      public List<ProcessedTag> Results { get; set; }
   }
}