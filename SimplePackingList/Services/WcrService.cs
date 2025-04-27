using Microsoft.Windows.AI.Generative;
using System;
using System.Threading.Tasks;

namespace SimplePackingList.Services;

public class WcrService
{
    public async Task<string> TextResponseWithProgress(string prompt, IProgress<string> progress)
    {
        using LanguageModel languageModel = await LanguageModel.CreateAsync();
        TextRewriter textSummarizer = new(languageModel);

        Windows.Foundation.IAsyncOperationWithProgress<LanguageModelResponseResult, string> textSummarizerResponseWithProgress = textSummarizer.RewriteAsync(prompt);

        string progressText = string.Empty;
        textSummarizerResponseWithProgress.Progress = (_, generationProgress) =>
        {
            progress.Report(generationProgress);
        };
        LanguageModelResponseResult response = await textSummarizerResponseWithProgress;
        return response.Text;
    }
}
