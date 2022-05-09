/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LocalizationEntry.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Server.Buffers;
using Server.Collections;

namespace Server;

public class LocalizationEntry
{
    private static readonly Regex _textRegex = new(
        @"~(\d+)[_\w]+~",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant
    );

    public string Language { get; }
    public int Number { get; }
    public string Text { get; }
    public string[] TextSlices { get; }
    public string StringFormatter { get; }

    public LocalizationEntry(string lang, int number, string text)
    {
        Language = lang;
        Number = number;
        Text = text;

        ParseText(text, out var textSlices, out var stringFormatter);
        TextSlices = textSlices;
        StringFormatter = stringFormatter;
    }

    private static void ParseText(string text, out string[] textSlices, out string stringFormatter)
    {
        bool hasMatch = false;
        var prevIndex = 0;
        var builder = new ValueStringBuilder(stackalloc char[256]);
        using var queue = PooledRefQueue<string>.Create();
        foreach (Match match in _textRegex.Matches(text))
        {
            if (prevIndex < match.Index)
            {
                var substr = text[prevIndex..match.Index];
                builder.Append(substr);

                queue.Enqueue(substr);
            }

            queue.Enqueue(null);
            hasMatch = true;
            builder.Append($"{{{int.Parse(match.Groups[1].Value) - 1}}}");
            prevIndex = match.Index + match.Length;
        }

        if (prevIndex < text.Length - 1)
        {
            var substr = prevIndex == 0 ? text : text[prevIndex..];
            builder.Append(substr);
            queue.Enqueue(substr);
        }

        textSlices = queue.ToArray();
        stringFormatter = hasMatch ? builder.ToString() : null;

        builder.Dispose();
    }

    public string Format(params object[] args) => string.Format(StringFormatter, args);

    /// <summary>
    /// Creates a formatted string of the localization entry.
    /// Uses string interpolation under the hood. This method is preferably relative to the object array method signature.
    /// Example:
    /// Format($"{totalItems}{maxItems}{totalWeight}");
    /// </summary>
    /// <param name="handler">interpolated string handler used by the compiler as a string builder during compilation</param>
    /// <returns>A copy of the localization text where the placeholder arguments have been replaced with string representations of the provided interpolation arguments</returns>
    public PooledArraySpanFormattable Format(
        [InterpolatedStringHandlerArgument("")]
        ref LocalizationInterpolationHandler handler
    )
    {
        var chars = handler.ToPooledArray(out var length);
        handler = default; // Defensive clear
        return new PooledArraySpanFormattable(chars, length);
    }
}
