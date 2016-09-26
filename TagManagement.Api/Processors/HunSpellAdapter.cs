using NHunspell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace TagManagement.Api.Processors
{
   public class HunSpellAdapter
   {
      private readonly string AffFilePath = @"D:\\Projects\\TagManagement\\TagManagement.Api\\" + ConfigurationManager.AppSettings["AffFilePath"];

      private readonly string DictionaryFilePath = @"D:\\Projects\\TagManagement\\TagManagement.Api\\" + ConfigurationManager.AppSettings["DictionaryFilePath"];

      private readonly string DatFilePath = @"D:\\Projects\\TagManagement\\TagManagement.Api\\" + ConfigurationManager.AppSettings["DatFilePath"];

      private Hunspell hunSpell;
      private MyThes thesaurus;

      public HunSpellAdapter()
      {
         this.hunSpell = new Hunspell(AffFilePath, DictionaryFilePath);
         this.thesaurus = new MyThes(DatFilePath);
      }

      public List<string> GetStem(string word)
      {
         return hunSpell.Stem(word);
      }

      public List<string> GetSuggestions(string word)
      {
         return hunSpell.Suggest(word);
      }

      public List<string> GetAnalysis(string word)
      {
         return hunSpell.Analyze(word);
      }

      public List<string> GetSynonyms(string word)
      {
         var result = new List<string>();
         var stemmedWordResult = this.GetStem(word);
         if (stemmedWordResult.Any())
         {
            foreach (var stemmedWord in stemmedWordResult)
            {
               if (!string.IsNullOrEmpty(stemmedWord))
               {
                  var thesaurusResult = this.GetLookup(stemmedWord);

                  if (thesaurusResult != null && thesaurusResult.Meanings != null && thesaurusResult.Meanings.Any())
                  {
                     thesaurusResult.Meanings.ForEach(m => m.Synonyms
                        //.Where(s => s.ToLower() != stemmedWord.ToLower())
                        //.Where(s => s.ToLower() != word.ToLower())
                        .ToList()
                        .ForEach(s => result.Add(s.ToLower()))
                     );
                  }
                  result.Add(stemmedWord);
               }
            }
         }

         return result;
      }

      public bool GetSpellCheck(string word)
      {
         return hunSpell.Spell(word);
      }

      public ThesResult GetLookup(string word)
      {
         return thesaurus.Lookup(word, hunSpell);
      }
   }
}