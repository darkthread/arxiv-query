using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Chroma;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;

public class SKAgent
{
    string endPoint;
    string apiKey;
    string modelId = "gpt-4o";
    Kernel kernel;
    KernelFunction translator;
    public SKAgent()
    {
        // https://blog.darkthread.net/blog/secure-apikey-for-console-app/
        endPoint = ProtectedEnvironmentVariables.Get("SK_EndPoint", true);
        apiKey = ProtectedEnvironmentVariables.Get("SK_ApiKey", true);
        kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(modelId, endPoint, apiKey)
            .Build();        
        var prompt = @"你是一個資訊專家，請將以下內容翻譯成zh-tw，專有名詞及用詞請使用台灣慣用說法：
````
{{$input}}
```";
        translator = kernel.CreateFunctionFromPrompt(prompt,
            executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 2048 });        
    }

    public async Task<string> Translate(string english)
    {
        var result = await kernel.InvokeAsync(translator, new() { ["input"] = english });
        return result.ToString();
    }


    public async Task ExplainDemo()
    {
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(modelId, endPoint, apiKey)
            .Build();

        var prompt = "請簡要解釋`{{$input}}`，回答請使用zh-tw，長度不可超過200字。";
        var explain = kernel.CreateFunctionFromPrompt(prompt,
            executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 200 });
        Console.WriteLine(Console.InputEncoding.EncodingName); // System.Text.UTF8Encoding
        Console.InputEncoding = Encoding.Default;
        while (true)
        {
            Console.Write("請輸入要解釋的名詞(直接按Enter結束)：");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                break;
            }
            Console.WriteLine($"解釋：{input}");
            Console.WriteLine(await kernel.InvokeAsync(explain,
                new() { ["input"] = input }));
        }
    }

    public async Task ChatBotDemo()
    {
        var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(modelId, endPoint, apiKey)
        .Build();
        // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/04-kernel-arguments-chat.ipynb
        var prompt = @"
ChatBot 可與 User 討論任何話題，它可提供明確指引，無答案時可回覆「我不知道」。

{{$history}}
User: {{$input}}
ChatBot:";
        var explain = kernel.CreateFunctionFromPrompt(prompt,
            executionSettings: new OpenAIPromptExecutionSettings
            {
                MaxTokens = 2000,
                Temperature = 0.7, // 隨機性 (預設 1)
                TopP = 0.5, // 多樣性 (預設 1)
            });
        // 一般不建議同時調整 Temperature 和 TopP，因為兩者都是調整隨機性的參數，
        // 同時調整會讓結果更難預測。官方範例寫法應用來在於激發一些有趣的對話。
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        var history = new StringBuilder();
        Console.WriteLine("您好，我是 ChatBot，今天想聊什麼？");
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                break;
            }
            Console.WriteLine("Input = " + input);
            var answer = await kernel.InvokeAsync(explain,
                new() { ["input"] = input, ["history"] = history.ToString() });
            history.AppendLine($"User: {input}");
            history.AppendLine($"ChatBot: {answer}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(answer);
        }
        Console.ResetColor();
    }


    //https://www.assemblyai.com/blog/ask-dotnetrocks-questions-semantic-kernel/


    public async Task EmbeddingDemo()
    {
        //https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Connectors/Connectors.Memory.Chroma
        const string chromaUrl = "http://localhost:8000";

        var memory = new MemoryBuilder()
            .WithChromaMemoryStore(chromaUrl)
            // For OpenAI
            //.WithOpenAITextEmbeddingGeneration("text-embedding-ada-002", apiKey)
            // For Azure OpenAI
            .WithAzureOpenAITextEmbeddingGeneration("embedding", endPoint, apiKey)
            .Build();

        const string memColName = "aboutMe";
        await memory.SaveInformationAsync(memColName, id: "info1", text: "My name is Andrea");
        await memory.SaveInformationAsync(memColName, id: "info2", text: "I currently work as a tourist operator");
        await memory.SaveInformationAsync(memColName, id: "info3", text: "I currently live in Seattle and have been living there since 2005");
        await memory.SaveInformationAsync(memColName, id: "info4", text: "I visited France and Italy five times since 2015");
        await memory.SaveInformationAsync(memColName, id: "info5", text: "My family is from New York");

        var questions = new[]
        {
            "what is my name?",
            "where do I live?",
            "where is my family from?",
            "where have I travelled?",
            "what do I do for work?",
        };

        foreach (var q in questions)
        {
            var response = await memory.SearchAsync(memColName, q, limit: 2, minRelevanceScore: 0.5).FirstOrDefaultAsync();
            Console.WriteLine(q + " " + response?.Metadata.Text);
        }

    }


}