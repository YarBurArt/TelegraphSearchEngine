using System.Collections.Generic;
using System.Text;

namespace TelegraphSearchEngine.Services;

// mirrors telegra.ph transliteration: шифр -> shifr
public static class Translit
{
    private static readonly IReadOnlyDictionary<char, string> Map = new Dictionary<char, string>
    {
        ['а'] = "a",  ['б'] = "b",  ['в'] = "v",    ['г'] = "g",  ['д'] = "d",  ['е'] = "e",
        ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z",    ['и'] = "i",  ['й'] = "y",  ['к'] = "k",
        ['л'] = "l",  ['м'] = "m",  ['н'] = "n",    ['о'] = "o",  ['п'] = "p",  ['р'] = "r",
        ['с'] = "s",  ['т'] = "t",  ['у'] = "u",    ['ф'] = "f",  ['х'] = "h",  ['ц'] = "ts",
        ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch",  ['ъ'] = "'",  ['ы'] = "yi",
        ['ь'] = "",   ['э'] = "e",  ['ю'] = "yu",   ['я'] = "ya",
        ['А'] = "A",  ['Б'] = "B",  ['В'] = "V",    ['Г'] = "G",  ['Д'] = "D",  ['Е'] = "E",
        ['Ё'] = "Yo", ['Ж'] = "Zh", ['З'] = "Z",    ['И'] = "I",  ['Й'] = "Y",  ['К'] = "K",
        ['Л'] = "L",  ['М'] = "M",  ['Н'] = "N",    ['О'] = "O",  ['П'] = "P",  ['Р'] = "R",
        ['С'] = "S",  ['Т'] = "T",  ['У'] = "U",    ['Ф'] = "F",  ['Х'] = "H",  ['Ц'] = "Ts",
        ['Ч'] = "Ch", ['Ш'] = "Sh", ['Щ'] = "Sch",  ['Ъ'] = "'",  ['Ы'] = "Yi",
        ['Ь'] = "",   ['Э'] = "E",  ['Ю'] = "Yu",   ['Я'] = "Ya"
    };

    public static string Transliterate(string source)
    {
        var builder = new StringBuilder(source.Length);

        foreach (var symbol in source)
        {
            builder.Append(Map.TryGetValue(symbol, out var replacement) ? replacement : symbol);
        }

        return builder.ToString();
    }
}
