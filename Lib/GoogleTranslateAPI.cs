using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Google.Apis.Translate.v2.Data;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GoogleTranslateAPI : MonoBehaviour
{
    static GameSettings settings;
    private static string apiKey = "AIzaSyBxOg-1y1tAKWEcbG8NOKvpIMVkqBnAvl8";

    public static void TranslateText(MonoBehaviour context, string text, System.Action<string> onComplete)
    {
        context.StartCoroutine(TranslateTextCoroutine(text, onComplete));
    }

    private static IEnumerator TranslateTextCoroutine(string text, System.Action<string> onComplete)
    {
        Task<string> translateTask = TranslateTextHandle(text);

        while (!translateTask.IsCompleted)
        {
            yield return null;
        }

        if (translateTask.IsCompletedSuccessfully)
        {
            onComplete?.Invoke(translateTask.Result);
        }
        else
        {
            Debug.LogError("Translation task failed.");
            onComplete?.Invoke(null);
        }
    }

    public static async Task<string> TranslateTextHandle(string text)
    {
        if (GameController.INSTANCE.Language.Equals("JP"))
            return text;

        text.Replace("cheer", "XXXXXXX");
        text.Replace("cheers", "XXXX01X");
        text.Replace("&lt", "|");

        if (string.IsNullOrEmpty(text))
            return null;
        
        try
        {
            if (settings == null)
                settings = GameObject.Find("GameSettings").GetComponent<GameSettings>();

            var translateService = new TranslateService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "Google Translate API"
            });

            var request = translateService.Translations.List(text, settings.Language);
            TranslationsListResponse response = await request.ExecuteAsync();

            return response.Translations[0].TranslatedText.Replace("XXXXXXX", "cheer").Replace("XXXX01X", "cheers");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to translate text: " + ex.Message);
            return text;
        }
    }
}
