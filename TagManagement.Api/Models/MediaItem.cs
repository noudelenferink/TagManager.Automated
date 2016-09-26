namespace TagManagement.Api.Models
{
   public class MediaItem
   {
      [PetaPoco.Column("MediaItemID")]
      public int ID { get; set; }
      public string Title { get; set; }
      public string Description { get; set; }
      public int ExternalID { get; set; }
      [PetaPoco.Ignore]
      public int NumTags { get; set; }
   }
}