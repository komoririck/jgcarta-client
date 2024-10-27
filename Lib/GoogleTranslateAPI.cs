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

    // This method starts the coroutine
    public static void TranslateText(MonoBehaviour context, string text, System.Action<string> onComplete)
    {
        context.StartCoroutine(TranslateTextCoroutine(text, onComplete));
    }

    // Coroutine method to call the async method
    private static IEnumerator TranslateTextCoroutine(string text, System.Action<string> onComplete)
    {
        Task<string> translateTask = TranslateTextHandle(text);

        // Wait until the async task completes
        while (!translateTask.IsCompleted)
        {
            yield return null;
        }

        // Call the completion callback with the result
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

    // Async method to handle the actual translation
    public static async Task<string> TranslateTextHandle(string text)
    {
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

            return response.Translations[0].TranslatedText;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to translate text: " + ex.Message);
            return null;
        }
    }
}
