using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TagManagement.Api.Models;
using TagManagement.Api.Processors;

namespace TagManagement.Api.Controllers
{
   [RoutePrefix("api")]
   [EnableCors(origins: "http://localhost:9000", headers: "*", methods: "*")]
   public class WebApiController : ApiController
   {
      private HunSpellAdapter hunspellAdapter;

      public List<string> DutchStopwords { get; private set; }

      public WebApiController()
      {
         this.hunspellAdapter = new HunSpellAdapter();
         var stopwordsFilePath = HttpContext.Current.Request.PhysicalApplicationPath + @"Tools\\stop-words_dutch_1_nl.txt";
         this.DutchStopwords = File.ReadAllLines(stopwordsFilePath).ToList();
         this.DutchStopwords.AddRange(new List<string> { "man", "bijt", "hond" });
      }

      [Route("mediaitems")]
      public List<MediaItem> GetMediaItems(bool showAll = false)
      {
         var tagProcessor = new TagProcessor();
         return tagProcessor.GetMediaItems(showAll);
      }

      [Route("mediaitems/{mediaItemID}/tags")]
      public List<Tag> GetMediaItemTags(int mediaItemID)
      {
         var tagProcessor = new TagProcessor();
         return tagProcessor.GetMediaItemTags(mediaItemID);
      }

      [HttpPost]
      [Route("process")]
      public IHttpActionResult ProcessTagList([FromBody] List<string> tagList)
      {
         var result = new List<ResultGroup>();
         var processor = new TagProcessor();
         result = processor.ProcessTags(tagList);
         return Ok(result);
      }

      [HttpGet]
      [Route("process/{mediaItemID}")]
      public IHttpActionResult ProcessMediaItemTags(int mediaItemID)
      {
         
         var processor = new TagProcessor();

         var resultList = processor.ProcessAgain(mediaItemID);

         return Ok(resultList);
      }

      [HttpPost]
      [Route("mediaitems/{mediaItemID}/media-tags")]
      public void SaveMediaTags(int mediaItemID, [FromBody] List<MediaTag> mediaTagList)
      {
         var db = new PetaPoco.Database("mysql");
         foreach (var mediaTag in mediaTagList)
         {
            if (mediaTag.ID == default(int))
            {
               var newMediatag = new MediaTag
               {
                  Value = mediaTag.Value
               };

               if (mediaTag.Type != (int)MediaTagType.None)
               {
                  newMediatag.Type = (int)mediaTag.Type;
               }
               var newMediaTagID = db.Insert(newMediatag);
               mediaTag.ID = long.Parse(newMediaTagID.ToString());
            }

            db.Insert(new MediaItemMediaTag { MediaTagID = mediaTag.ID, MediaItemID = mediaItemID });
         }
      }

      [HttpPost]
      [Route("hunspell/stem")]
      public IHttpActionResult GetStem([FromBody] string word)
      {
         var result = hunspellAdapter.GetStem(word);
         return Ok(result);
      }

      [HttpPost]
      [Route("hunspell/suggest")]
      public IHttpActionResult GetSuggestions([FromBody] string word)
      {
         var result = hunspellAdapter.GetSuggestions(word);
         return Ok(result);
      }

      [HttpPost]
      [Route("hunspell/synonyms")]
      public IHttpActionResult GetSynonyms([FromBody] string word)
      {
         var result = hunspellAdapter.GetSynonyms(word);
         return Ok(result);
      }

      [HttpPost]
      [Route("hunspell/analyze")]
      public IHttpActionResult GetAnalysis([FromBody] string word)
      {
         var result = hunspellAdapter.GetAnalysis(word);
         return Ok(result);
      }

      [HttpPost]
      [Route("hunspell/spell")]
      public IHttpActionResult GetSpellCheck([FromBody] string word)
      {
         var result = hunspellAdapter.GetSpellCheck(word);
         return Ok(result);
      }

      [HttpPost]
      [Route("hunspell/lookup")]
      public IHttpActionResult GetLookup([FromBody] string word)
      {
         var result = hunspellAdapter.GetLookup(word);
         return Ok(result);
      }

      [HttpPost]
      [Route("hunspell/test")]
      public IHttpActionResult TestMethod([FromBody] string word)
      {
         var db = new PetaPoco.Database("mysql");
         var items = db.Fetch<int>("SELECT DISTINCT MediaItemID FROM Tag WHERE TagValue = @0", word).ToList();

         var sql = PetaPoco.Sql.Builder.Select("MediaItemID, TagValue").From("Tag").Where("MediaItemID IN (@items) AND TagValue != @word", new { items = items, word = word }).GroupBy("MediaItemID, TagValue");
         var otherTagsAtItems = db.Query<dynamic>(sql);
         var groupedByValue = from tag in otherTagsAtItems
                              group tag by tag.TagValue into groupValue
                              select new
                              {
                                 Value = groupValue.Key,
                                 Count = groupValue.Count()
                              };

         var resultList = groupedByValue.Where(g => g.Count == items.Count && g.Count > 1).OrderByDescending(g => g.Count).ToList();
         return Ok(resultList);
      }

      [HttpPost]
      [Route("test")]
      public IHttpActionResult Aap([FromBody] string word)
      {
         var resultValue = new List<dynamic>();
         var db = new PetaPoco.Database("mysql");
         // Get other media tags where tag occurs
         var sql = PetaPoco.Sql.Builder.Select("DISTINCT MediaItemID").From("Tag").Where("TagValue = @0", word);
         var itemsWithTag = db.Query<int>(sql).ToList();

         var sql2 = PetaPoco.Sql.Builder.Select("TagValue").From("Tag").Where("MediaItemID IN (@items)", new { items = itemsWithTag }).GroupBy("TagValue").OrderBy("TagValue");
         var tagsInItems = db.Query<string>(sql2).ToList();

         foreach (var tag in tagsInItems)
         {
            var sql3 = PetaPoco.Sql.Builder.Select("TagValue, COUNT(DISTINCT MediaItemID").From("Tag").Where("TagValue = @0", tag);
            var tagOccurences = db.Fetch<dynamic>("SELECT TagValue, COUNT(DISTINCT MediaItemID) FROM Tag WHERE TagValue = @0 GROUP BY TagValue", tag);
            resultValue.Add(tagOccurences);
         }

         return this.Ok(resultValue);
      }
   }
}
