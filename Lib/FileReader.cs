using Assets.Scripts.Objects;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FileReader : MonoBehaviour
{
    [SerializeField] public static Dictionary<string, Record> result = new();

    string fileName = "cards_final";
    TextAsset textFile;

    static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        FloatParseHandling = FloatParseHandling.Double
    };

    void Start()
    {
        textFile = Resources.Load<TextAsset>(fileName);
        if (result.Count == 0) 
        {
            StartCoroutine (ReadFileV2(textFile.text));
        }
    }

    public static IEnumerator ReadFileV2(string json)
    {
        var jsonCards = JsonConvert.DeserializeObject<List<Record>>(json, settings);
        var resultt = new Dictionary<string, Record>();

        foreach (var cardx in jsonCards)
        {
            if (cardx.Id < 199)
                resultt.TryAdd(cardx.CardNumber, cardx);
            if (cardx.Id > 198)
            {
                bool add = true;
                if (cardx.CardType == "ホロメン" || cardx.CardType == "Buzzホロメン")
                    foreach (var art in cardx.Arts)
                    {
                        if (!string.IsNullOrEmpty(JsonArt.Convert(art).Effect))
                            add = false;
                    }
                if (add)
                    resultt.TryAdd(cardx.CardNumber, cardx);
            }
        }
        result = resultt;
        yield break;
    }
}

