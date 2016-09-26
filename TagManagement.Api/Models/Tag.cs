namespace TagManagement.Api.Models
{
   public class Tag
   {
      public int TagID { get; set; }
      public int MediaItemID { get; set; }
      public string TagValue { get; set; }
   }
}