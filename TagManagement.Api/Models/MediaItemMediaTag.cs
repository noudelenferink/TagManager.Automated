namespace TagManagement.Api.Models
{
   public class MediaItemMediaTag
   {
      [PetaPoco.Column("MediaItemId")]
      public long MediaItemID { get; set; }
      [PetaPoco.Column("MediaTagId")]
      public long MediaTagID { get; set; }
   }
}