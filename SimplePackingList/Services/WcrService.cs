using Microsoft.Windows.AI.Text;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SimplePackingList.Services;

public class WcrService
{
    public async Task<string> TextResponseWithProgress(string prompt, IProgress<string> progress)
    {
        string baseContext = """
            You are a helpful assistant. You will be provided with a recent weather report. Do not mention the date or the weather report. You will help the user by describing the best kinds clothes to wear given the weather. Do not include any other information or context. Keep responses short. Start each line with hyphen and put each recommendation on its own line.
            """;

        using LanguageModel languageModel = await LanguageModel.CreateAsync();
        LanguageModelOptions languageModelOptions = new()
        {
            Temperature = 0.5f
        };

        LanguageModelContext contextModel = languageModel.CreateContext(baseContext);

        IAsyncOperationWithProgress<LanguageModelResponseResult, string> responseAndProgress = languageModel.GenerateResponseAsync(contextModel, prompt, languageModelOptions);

        string progressText = string.Empty;
        responseAndProgress.Progress = (_, generationProgress) =>
        {
            progress.Report(generationProgress);
        };
        LanguageModelResponseResult response = await responseAndProgress;
        return response.Text;
    }
}
