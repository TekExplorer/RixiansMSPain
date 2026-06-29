using Godot;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

class AlternateArtsCredits
{
    public static string? CreditFor<T>() where T : CardModel => CreditFor(ModelDb.Card<T>());

    public static string? CreditFor(CardModel card) => Credits.GetValueOrDefault(KeyFor(card));

    public static string KeyFor(CardModel card)
    {
        var pool = card.Pool.Title.ToLowerInvariant();
        var name = card.Id.Entry.ToLowerInvariant();
        var key = $"{pool}.{name}"; // "silent.predator"
        return key;
    }

    public static Dictionary<string, string> Credits { get; set; } = [];

    public static void LoadCreditsFromFile()
    {
        // json file
        const string filePath = "res://HideDetailsMod/credits.json"; // Use 'user://' for save files

        if (!Godot.FileAccess.FileExists(filePath)) return;
        // 1. Open the file
        using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);

        // 2. Read the raw text content
        string jsonText = file.GetAsText();

        // 3. Create a JSON parser instance and parse the text
        Json jsonParser = new();
        Error error = jsonParser.Parse(jsonText);
        if (error == Error.Ok)
        {
            // 4. Convert the parsed Variant data into a Godot Dictionary
            Credits = jsonParser.Data.AsGodotDictionary()
                .ToDictionary(kvp => kvp.Key.AsString(), kvp => kvp.Value.AsString());
            MainFile.Logger.Debug($"Credits file loaded: {Credits}");
        }
        else
        {
            MainFile.Logger.Error(
                $"LoadCreditsFromFile: JSON Parsing Error: {jsonParser.GetErrorMessage()} on line {jsonParser.GetErrorLine()}");
        }
    }
}