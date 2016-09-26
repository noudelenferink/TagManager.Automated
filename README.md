# TagManager.Automated

Automated version of the TagManager. Contains an algortithm that processes all raw tags in a media catalog and outputs the resulting tag sets to an .txt-file.

- Excludes stop words
- Groups by unique value
- Select the tags with the most occurences, else the longest tag
- Checks the spelling of the working set
- Stems the tags in the working set
- Filters the remaining set to unique tags
- Finds relevant tags in the rest of the repostitory by looking at the occurences of the remaining tags at other items.
 - Looks at tags that have a maximum occurence of 25% in the rest of the catalog
 - Select the tags that, when they occur, 
  - Have an occurence rating of at least 50% with at tag from the working set.
  - Have a higher occurence rating together with the tag, than compared to the total set
  - Have a higher occurence together than when they are assigned at their own.
