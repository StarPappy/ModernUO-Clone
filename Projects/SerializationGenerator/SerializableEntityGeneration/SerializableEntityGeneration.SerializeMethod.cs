/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.SerializeMethod.cs                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateSerializeMethod(
            this StringBuilder source,
            Compilation compilation,
            bool isOverride,
            bool encodedVersion,
            List<SerializableProperty> properties
        )
        {
            var genericWriterInterface = compilation.GetTypeByMetadataName(GENERIC_WRITER_INTERFACE);

            source.GenerateMethodStart(
                "Serialize",
                AccessModifier.Public,
                isOverride,
                "void",
                ImmutableArray.Create<(ITypeSymbol, string)>((genericWriterInterface, "writer"))
            );

            const string indent = "            ";

            source.AppendLine($"{indent}var savePosition = ((Server.ISerializable)this).SavePosition;");
            source.AppendLine(@$"{indent}if (savePosition > -1)
{indent}{{
{indent}    writer.Seek(savePosition, System.IO.SeekOrigin.Begin);
{indent}    return;
{indent}}}");

            // Version
            source.AppendLine();
            source.AppendLine($"{indent}writer.{(encodedVersion ? "WriteEncodedInt" : "Write")}(_version);");

            foreach (var property in properties)
            {
                source.AppendLine();
                SerializableMigrationRulesEngine.Rules[property.Rule].GenerateSerializationMethod(
                    source,
                    indent,
                    property
                );
            }

            source.GenerateMethodEnd();
        }
    }
}
