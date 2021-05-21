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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            ImmutableArray<IFieldSymbol> fields,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            HashSet<string> namespaces
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

            namespaces.Add(genericWriterInterface.GetNamespace().ToDisplayString());
            namespaces.Add("System.IO"); // For SeekOrigin

            const string indent = "            ";

            source.AppendLine($"{indent}var savePosition = ((ISerializable)this).SavePosition;");
            source.AppendLine(@$"{indent}if (savePosition > -1)
{indent}{{
{indent}    writer.Seek(savePosition, SeekOrigin.Begin);
{indent}    return;
{indent}}}");

            // Version
            source.AppendLine($"{indent}writer.WriteEncodedInt(_version);");

            foreach (var field in fields)
            {
                namespaces.Add(field.Type.GetNamespace().ToDisplayString());
                if (field.Type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
                {
                    foreach (var genericArgument in namedTypeSymbol.TypeArguments)
                    {
                        namespaces.Add(genericArgument.GetNamespace().ToDisplayString());
                    }
                }

                var attributes = field.GetAttributes();
                source.SerializeField(
                    $"{indent}    ",
                    field.Name,
                    field.Type,
                    compilation,
                    attributes,
                    serializableTypes
                );
            }

            source.GenerateMethodEnd();
        }

        public static void SerializeField(
            this StringBuilder source,
            string indent,
            string fieldName,
            ITypeSymbol fieldType,
            Compilation compilation,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            if (fieldType.IsEnum())
            {
                source.AppendLine($"{indent}writer.WriteEnum({fieldName})");
                return;
            }

            var namedFieldType = fieldType as INamedTypeSymbol;

            // Uses `writer.Write(obj);`
            if (namedFieldType?.IsPrimitiveSerialization(compilation, serializableTypes) == true)
            {
                source.AppendLine(
                    attributes.Any(a => a.IsDeltaDateTime(compilation))
                        ? $"{indent}writer.WriteDeltaTime({fieldName});"
                        : $"{indent}writer.Write({fieldName});"
                );
                return;
            }

            // Custom type, but it has a `Serialize` method we can use
            if (fieldType.HasPublicSerializeMethod(compilation, serializableTypes))
            {
                source.AppendLine($"{indent}{fieldName}.Serialize(writer);");
                return;
            }

            // Check if we have an array or collection, then recurse
            ITypeSymbol collectionElementType = null;
            string lengthProperty = null;

            if (fieldType.SpecialType == SpecialType.System_Array)
            {
                collectionElementType = ((IArrayTypeSymbol)fieldType).ElementType;
                lengthProperty = "Length";
            }
            else if (namedFieldType?.ContainsInterface(compilation.GetTypeByMetadataName(ICOLLECTION_INTERFACE)) == true)
            {
                collectionElementType = namedFieldType.TypeArguments[0];
                lengthProperty = "Count";
            }

            if (collectionElementType != null)
            {
                source.AppendLine($"{indent}for (var i = 0; i < {fieldName}.{lengthProperty}; i++)");
                source.AppendLine($"{indent}{{");
                source.SerializeField(
                    $"{indent}    ",
                    $"{fieldName}[i]",
                    collectionElementType,
                    compilation,
                    ImmutableArray<AttributeData>.Empty,
                    serializableTypes
                );
                source.AppendLine($"{indent}}}");
                return;
            }

            throw new Exception($"No serialization Write method for type {fieldType}");
        }

        private static bool IsPrimitiveSerialization(
            this INamedTypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var isSpecialType = symbol.SpecialType switch
            {
                SpecialType.System_Boolean   => true,
                SpecialType.System_SByte     => true,
                SpecialType.System_Int16     => true,
                SpecialType.System_Int32     => true,
                SpecialType.System_Int64     => true,
                SpecialType.System_Byte      => true,
                SpecialType.System_UInt16    => true,
                SpecialType.System_UInt32    => true,
                SpecialType.System_UInt64    => true,
                SpecialType.System_Single    => true,
                SpecialType.System_Double    => true,
                SpecialType.System_String    => true,
                SpecialType.System_Decimal   => true,
                SpecialType.System_DateTime  => true,
                SpecialType.System_ValueType => true,
                _                            => false
            };

            return isSpecialType ||
                   symbol.IsPoint2D(compilation) ||
                   symbol.IsPoint3D(compilation) ||
                   symbol.IsRectangle2D(compilation) ||
                   symbol.IsRectangle3D(compilation) ||
                   symbol.IsIpAddress(compilation) ||
                   symbol.IsRace(compilation) ||
                   symbol.IsMap(compilation) ||
                   // Already inherits `ISerializable`
                   symbol.HasSerializableInterface(compilation, serializableTypes) ||
                   symbol.IsListOfSerializable(compilation, serializableTypes) ||
                   symbol.IsHashSetOfSerializable(compilation, serializableTypes);
        }
    }
}
