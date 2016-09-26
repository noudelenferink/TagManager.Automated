namespace TagManagement.Api.Processors
{
   using NHunspell;
   using System;
   using System.Collections.Generic;
   using System.Configuration;
   using System.Linq;
   using System.Web;
   using Models;
   using System.IO;
   using System.Xml.Linq;

   public class TagProcessor
   {
      
      private List<MediaTag> mediaTagRepository;
      private List<Tag> tagRepository;
      private HunSpellAdapter hunSpellAdapter;
      private List<MediaItem> mediaItemRepository;
      private List<Tuple<string,int>> tagCountRepository;
      private XDocument wordnetXmlDoc;

      public List<string> DutchStopwords
      {
         get; private set;
      }

      public TagProcessor()
      {
         this.hunSpellAdapter = new HunSpellAdapter();

         var stopwordsFilePath = @"D:\\Projects\\TagManagement\\TagManagement.Api\\" + @"Tools\\stop-words_dutch_1_nl.txt";
         this.DutchStopwords = File.ReadAllLines(stopwordsFilePath).ToList();
         this.DutchStopwords.AddRange(new List<string> { "man", "bijt", "hond" });

         var wordnetPath = @"D:\\Projects\\TagManagement\\TagManagement.Api\\" + @"Tools\\wordnet.xml";
         this.wordnetXmlDoc = XDocument.Load(wordnetPath);

         var db = new PetaPoco.Database("mysql");
         this.mediaItemRepository = db.Fetch<MediaItem>("WHERE ExternalID IS NOT NULL");
         this.mediaTagRepository = db.Fetch<MediaTag>("WHERE 1 = 1");
         this.tagRepository = db.Fetch<Tag>("WHERE 1 = 1").Select(t => new Tag { TagID = t.TagID, MediaItemID = t.MediaItemID, TagValue = t.TagValue.ToLower() }).ToList();
         tagCountRepository = this.tagRepository.Select(t => new { MediaItemID = t.MediaItemID, TagValue = t.TagValue.ToLower() }).Distinct().GroupBy(g => g.TagValue, (key, group) => new Tuple<string, int> (key, group.Count())).ToList();
      }

      public List<Tag> GetTagRepository()
      {
         var counter = 0;
         var db = new PetaPoco.Database("mysql");
         var rep = db.Fetch<Tag>("WHERE 1 = 1").Select(t => new Tag { TagID = t.TagID, MediaItemID = t.MediaItemID, TagValue = t.TagValue.ToLower() }).ToList();
         var clone = rep.ToList();
         foreach (var tag in clone)
         {
            if (!hunSpellAdapter.GetSpellCheck(tag.TagValue))
            {
               var sugg = hunSpellAdapter.GetSuggestions(tag.TagValue);
               if (sugg.Any())
               {
                  rep.Remove(tag);
                  rep.Add(new Tag { TagID = tag.TagID, MediaItemID = tag.MediaItemID, TagValue = sugg.First().ToLower() });
               }
            }

            var tagStem = hunSpellAdapter.GetStem(tag.TagValue);
            if (tagStem.Any())
            {
               rep.Remove(tag);
               rep.Add(new Tag  { TagID = tag.TagID, MediaItemID = tag.MediaItemID, TagValue = tagStem.First().ToLower() });
            }
            counter++;
            Console.WriteLine((double)counter / clone.Count);
         }

         return rep.Distinct().ToList();
      }

      public List<ResultGroup> ProcessTags(List<string> tagList)
      {
         var counter = 1;
         var groupedList = tagList.Where(t => !DutchStopwords.Contains(t)).GroupBy(t => t.ToLower(), (key, group) => new { Value = key, Occurences = group.Count() }).ToList();
         var processingList = groupedList.Select(t => new ProcessedTag { ID = counter++, Value = t.Value, Occurences = t.Occurences }).ToList();
         //foreach (var tag in processingList)
         //{
         //   this.ProcessTag(tag, processingList);
         //}


         var groupTest = (from r in processingList
                          //where hunSpellAdapter.GetSpellCheck(r.Value)
                          let identifier = r.DuplicateTagID ?? r.RelatedTagID ?? r.ID
                          group r by identifier into groupedResults
                          select new ResultGroup
                          {
                             GroupId = groupedResults.Key,
                             Results = groupedResults.ToList()
                          }).ToList();

         return groupTest;
      }

      private List<ProcessedTag> ProcessTag(ProcessedTag tagToCheck, List<ProcessedTag> tagList)
      {
         var returnList = new List<ProcessedTag>();
         //var stemmed = this.GetStem(tagToCheck.Value);
         //if (stemmed.Any())
         //{
         //   tagToCheck.Value = stemmed.Last();
         //}
         var tagSynonyms = GetTagSynonyms(tagToCheck);
         CheckRepositoryForDuplicates(tagToCheck);
         
         var result = (from t in tagList
                       where t.ID != tagToCheck.ID
                       where !t.DuplicateTagID.HasValue
                       where !t.RelatedTagID.HasValue
                       let tagWords = GetTagWords(t.Value)
                       let foundDuplicateTagId = !tagToCheck.IsDuplicateReference && t.Value.ToLower() == tagToCheck.Value.ToLower() ? t.ID : (int?)null
                       let foundPartialDuplicateTagId = !tagToCheck.IsRelatedReference && tagWords.Any(a => GetTagWords(tagToCheck.Value).Select(wpr => wpr.ToLower()).Contains(a.ToLower())) ? t.ID : (int?)null
                       let foundSynonymTagId = !tagToCheck.IsRelatedReference && tagWords.Any(w => tagSynonyms.Contains(w.ToLower())) ? t.ID : (int?)null
                       let sortOrderValue = Convert.ToInt32(foundDuplicateTagId.HasValue) + Convert.ToInt32(foundPartialDuplicateTagId.HasValue) + Convert.ToInt32(foundSynonymTagId.HasValue)
                       orderby sortOrderValue descending
                       select new
                       {
                          DuplicateTagId = foundDuplicateTagId,
                          RelatedTagId = foundPartialDuplicateTagId ?? foundSynonymTagId
                       }).FirstOrDefault();

         if (result != null)
         {
            if (result.DuplicateTagId.HasValue)
            {
               tagList.Find(t => t.ID == tagToCheck.ID).DuplicateTagID = result.DuplicateTagId;
               tagList.Find(t => t.ID == result.DuplicateTagId).IsDuplicateReference = true;
            }

            if (result.RelatedTagId.HasValue)
            {
               tagList.Find(t => t.ID == tagToCheck.ID).RelatedTagID = result.RelatedTagId;
               tagList.Find(t => t.ID == result.RelatedTagId).IsRelatedReference = true;
            }
         }

         return tagList;
      }

      private List<string> GetTagSynonyms(ProcessedTag tag)
      {
         var result = new List<string>();
         var tagWords = GetTagWords(tag.Value);
         var searchWords = new List<string>();
         foreach (var word in tagWords)
         {
            if (!hunSpellAdapter.GetSpellCheck(word))
            {
               var suggestions = hunSpellAdapter.GetSuggestions(word);
               if (suggestions.Any())
               {
                  tag.Value = suggestions.First();
               }

            }
            else
            {
               if (tagWords.Count == 1)
               {
                  tag.IsSpelledCorrect = true;
               }
               searchWords.Add(word);
            }
         }
         searchWords.ForEach(w => result.AddRange(hunSpellAdapter.GetSynonyms(w)));
         return result;
      }

      private List<string> GetTagWords(string tag)
      {
         var wordList = new List<string>();
         var splitted = tag.Split(' ');
         Array.ForEach(splitted, wordList.Add);
         return wordList;
         //return splitted.Select(w => new TagWord { Word = w, WordIndex = Array.IndexOf(splitted, w) }).ToList();

      }

      public List<MediaItem> GetMediaItems(bool showAll)
      {
         var db = new PetaPoco.Database("mysql");
         var sql = "SELECT mi.MediaItemID, mi.Title, mi.Description, mi.ExternalID, COUNT(1) AS NumTags FROM Tag t JOIN MediaItem mi ON mi.MediaItemID = t.MediaItemID";
         if (!showAll)
         {
            sql += " WHERE mi.ExternalID IS NOT NULL";
         }

         sql += " GROUP BY t.MediaItemID";
         var results = db.Fetch<MediaItem>(sql);

         return results;
      }

      public List<Tag> GetMediaItemTags(int mediaItemID)
      {
         var db = new PetaPoco.Database("mysql");
         var results = db.Fetch<Tag>("SELECT TagID AS ID, TagValue AS Value FROM Tag WHERE MediaItemID = @0", mediaItemID);
         return results;
      }

      private void CheckRepositoryForDuplicates(ProcessedTag tag)
      {
         tag.MediaTags = this.mediaTagRepository.Where(mt => mt.Value == tag.Value).ToList();
      }

      public List<string> ProcessAgain(int mediaItemID)
      {
         var tags = this.tagRepository.Where(t => t.MediaItemID == mediaItemID).ToList();

         var result = new List<ResultGroup>();
         var tagList = tags.Select(t => t.TagValue).ToList();
         result = ProcessTags(tagList);

         var bestTagsFromResultGroups = new List<ProcessedTag>();
         foreach (var group in result)
         {
            if (group.Results.Count > 1)
            {
               // If in the group of results, a single result has a media tag select that one.


               //if(group.Results.Where(r => r.MediaTags.Any()).Count() == 1)
               //{
               //   var tag = group.Results.Where(r => r.MediaTags.Any()).First();
               //   test.Add(new {
               //      ID = tag.MediaTags.First().ID,
               //      Value = tag.Value,
               //   });

               //   continue;
               //}

               //else if(group.Results.Count(r => r.IsSpelledCorrect) == 1)
               //{
               //   var tag = group.Results.Where(r => r.IsSpelledCorrect).Single();
               //   test.Add(new
               //   {
               //      Value = tag.Value
               //   });

               //   continue;
               //}

               if (group.Results.Where(r => r.Occurences == group.Results.Max(r2 => r2.Occurences)).Count() == 1)
               {
                  var tag = group.Results.Where(r => r.Occurences == group.Results.Max(r2 => r2.Occurences)).Single();
                  bestTagsFromResultGroups.Add(tag);
                  continue;
               }

               else if (group.Results.Where(r => r.Value.Length == group.Results.Max(r2 => r2.Value.Length)).Count() == 1)
               {
                  var tag = group.Results.Where(r => r.Value.Length == group.Results.Max(r2 => r2.Value.Length)).Single();
                  bestTagsFromResultGroups.Add(tag);
                  continue;
               }
            }
            else
            {
               bestTagsFromResultGroups.Add(group.Results[0]);
            }
         }

         var resultList = bestTagsFromResultGroups.Select(t => t.Value).ToList();
         var resultListClone = resultList.ToList();

         foreach (var tag in resultListClone)
         {
            if (!hunSpellAdapter.GetSpellCheck(tag))
            {
               var sugg = hunSpellAdapter.GetSuggestions(tag);
               if (sugg.Any())
               {
                  resultList.Remove(tag);
                  resultList.Add(sugg.First().ToLower());
               }
            }

            var tagStem = hunSpellAdapter.GetStem(tag);
            if (tagStem.Any()) {
               resultList.Remove(tag);
               resultList.Add(tagStem.First().ToLower());
            }
         }

         resultList = resultList.Distinct().ToList();
         var additionalTags = new List<string>();
         Console.WriteLine();
         var counter = 0;
         foreach (var t in resultList)
         {
            counter++;
            Console.Write("\r{0:000}%", ((double)counter/resultListClone.Count) * 100);
            additionalTags.AddRange(GetRelevantTagsInCollection(mediaItemID, t, additionalTags));
         }

         var x = additionalTags.GroupBy(r => r, (key, group) => new { Value = key, Count = group.Count() }).ToList();

         return x.Select(r => r.Value).Concat(resultList).OrderBy(r => r).ToList(); 
      }

      private List<string> GetRelevantTagsInCollection(int mediaItemID, string tag, List<string> currentResults)
      {
         var resultObject = new List<string>();

         var db = new PetaPoco.Database("mysql");
         var tagCount = this.tagCountRepository.Where(t => t.Item1 == tag).Select(t => t.Item2).SingleOrDefault();
         if (tagCount > 1)
         {
            var wnEntry = this.GetWordnetResult(tag);
            if (wnEntry == null || wnEntry.Type == "noun")
            {
               var otherMediaItemIDs = tagRepository.Where(t => t.MediaItemID != mediaItemID && t.TagValue == tag).Select(t => t.MediaItemID).Distinct().ToList();
               if (otherMediaItemIDs.Any() && otherMediaItemIDs.Count <= Math.Ceiling(0.25 * this.mediaItemRepository.Count))
               {
                  //var otherTagsInMediaItem = tagRepository.Where(t => t.MediaItemID == mediaItemID && t.TagValue != tag).Select(t => t.TagValue).Distinct().ToList();

                  //var sql = new PetaPoco.Sql("SELECT distinct MediaItemID, t.TagValue, t2.NumOccurences FROM Tag t JOIN (SELECT TagValue, COUNT(DISTINCT MediaItemID) AS NumOccurences FROM Tag GROUP BY TagValue) t2 ON t.TagValue = t2.TagValue WHERE t.MediaItemID IN (@ids) HAVING t2.NumOccurences > 1", new { ids = otherMediaItemIDs });
                  //var otherTagsInOtherMediaItems = db.Fetch<dynamic>(sql);

                  //var otherTagsInOtherMediaItems = this.tagRepository.Where(t => otherMediaItemIDs.Contains(t.MediaItemID) && t.TagValue != tag).Select(t => new { MediaItemID = t.MediaItemID, Value = t.TagValue }).Distinct().ToList();

                  //var i = (from t in otherTagsInOtherMediaItems
                  //         group t by t.Value into tGroup
                  //         let tagInCountRepository = this.tagCountRepository.Where(t => t.Item1 == tGroup.Key).Single()
                  //         where tGroup.Count() > 1
                  //         where tagInCountRepository.Item2 <= Math.Ceiling(0.25 * this.mediaItemRepository.Count)
                  //         where !DutchStopwords.Contains(tGroup.Key)
                  //         where !currentResults.Contains(tGroup.Key)
                  //         //where !otherTagsInMediaItem.Contains(tGroup.Key)
                  //         select new
                  //         {
                  //            TagValue = tGroup.Key as string,
                  //            OccurencesWithSearchValue = tGroup.Count(),
                  //            OccurencesInDataSet = tagInCountRepository.Item2
                  //         }).ToList();

                  var test = this.tagRepository.Select(t => new { t.MediaItemID, t.TagValue }).Distinct()
                     .Where(t => otherMediaItemIDs.Contains(t.MediaItemID) && t.TagValue != tag)
                     .GroupBy(t => t.TagValue, t => t.MediaItemID, (key, group) => new { TagValue = key, OccurencesWithSearchValue = group.Count() })
                     .Where(t => !DutchStopwords.Contains(t.TagValue))
                     //.Where(t => !currentResults.Contains(t.TagValue))
                     .Where(t => t.OccurencesWithSearchValue > 1)
                     .ToList();

                  var i2 = (from t in test
                            join tc in tagCountRepository on t.TagValue equals tc.Item1
                            where t.TagValue == tc.Item1
                            where tc.Item2 <= Math.Ceiling(0.25 * this.mediaItemRepository.Count)
                            select new
                            {
                               TagValue = t.TagValue as string,
                               OccurencesWithSearchValue = t.OccurencesWithSearchValue,
                               OccurencesInDataSet = tc.Item2
                            }).ToList();



                  //var j = i.Select(t => new { Value = t.TagValue, OccurencesWithSearchValue = t.OccurencesWithSearchValue, OccurencesInDataSet = this.tagRepository.Where(tr => tr.TagValue == t.TagValue).Select(tr => new { ItemID = tr.MediaItemID, Value = tr.TagValue }).Distinct().Count() }).ToList();

                  resultObject = i2
                     .Where(t => (double)t.OccurencesWithSearchValue / (double)t.OccurencesInDataSet > 0.5)
                     .Where(t => (double)t.OccurencesWithSearchValue / (double)t.OccurencesInDataSet >= (double)t.OccurencesWithSearchValue / (double)tagCount)
                     //.Where(t => t.OccurencesWithSearchValue / ((double)tagCount - t.OccurencesWithSearchValue) > 0.25)
                     .Where(t => (double)t.OccurencesWithSearchValue / ((double)(tagCount - t.OccurencesWithSearchValue) + (t.OccurencesInDataSet - t.OccurencesWithSearchValue) + t.OccurencesWithSearchValue) > 0.25)
                     //.Where(t => ((double)(tagCount - t.OccurencesWithSearchValue) + (t.OccurencesInDataSet - t.OccurencesWithSearchValue) + t.OccurencesWithSearchValue) / ((double)tagCount + t.OccurencesInDataSet) > 0.75)
                     .Select(t => t.TagValue).ToList();
               }
            }
         }

         return resultObject;
      }

      public WordnetResult GetWordnetResult(string word)
      {
         var lexialEntry = (from le in wordnetXmlDoc.Descendants("LexicalEntry")
                            where le.Descendants("WordForm").Any(x => (string)x.Attribute("writtenForm") == word)
                            select le).FirstOrDefault();

         if (lexialEntry == null)
         {
            return null;
         }

         return new WordnetResult
         {
            Stem = ((string)lexialEntry.Descendants("Lemma").Single().Attribute("writtenForm")).ToLower(),
            Type = ((string)lexialEntry.Attribute("partOfSpeech")).ToLower()
         };
      }
   }
}