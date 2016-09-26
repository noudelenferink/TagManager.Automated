namespace TagManagement.Api.Models
{
   [PetaPoco.PrimaryKey("MediaTagId")]
   public class MediaTag
   {
      [PetaPoco.Column("MediaTagId")]
      public long ID { get; set; }

      [PetaPoco.Column("MediaTagTypeId")]
      public int? Type { get; set; }

      [PetaPoco.Column("MediaTagValue")]
      public string Value { get; set; }
   }
}