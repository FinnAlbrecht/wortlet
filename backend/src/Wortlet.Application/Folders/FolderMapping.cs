using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Folders;

internal static class FolderMapping
{
    public static FolderSummaryDto ToDto(this FolderSummary s) =>
        new(s.Id, s.LanguageId, s.Name, s.Description, s.IsSeed, s.ChapterCount, s.WordCount, s.WeakCount, s.CreatedAt);

    public static FolderDetailDto ToDetail(this Folder f, Language language) => new(
        f.Id,
        f.LanguageId,
        language.Name,
        f.Name,
        f.Description,
        f.IsSeed,
        f.OldLabel,
        f.NewLabel,
        VocabularyText.Format(f.Chapters),
        f.Chapters.Select(c => new ChapterDto(
            c.Name,
            c.Pages,
            c.IsNew,
            c.Words.Select(w => new WordDto(
                w.Key,
                AnswerEvaluator.BaseKey(w.Key),
                w.Answers,
                AnswerEvaluator.Solution(w.Answers),
                w.Hint)).ToList())).ToList(),
        f.WeakWordList().Select(w => w.Key).ToList());
}
