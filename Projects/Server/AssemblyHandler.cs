/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AssemblyHandler.cs                                              *
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Server
{
    public static class AssemblyHandler
    {
        private static readonly Dictionary<Assembly, TypeCache> m_TypeCaches = new();
        private static TypeCache m_NullCache;
        public static Assembly[] Assemblies { get; set; }

        public static void LoadScripts(string[] files)
        {
            var assemblies = new Assembly[files.Length];

            for (var i = 0; i < files.Length; i++)
            {
                assemblies[i] = AssemblyLoadContext.Default.LoadFromAssemblyPath(files[i]);
            }

            Assemblies = assemblies;
        }

        public static void Invoke(string method)
        {
            var invoke = new List<MethodInfo>();

            Core.Assembly.AddMethods(method, invoke);

            for (var i = 0; i < Assemblies.Length; i++)
            {
                Assemblies[i].AddMethods(method, invoke);
            }

            invoke.Sort(new CallPriorityComparer());

            for (var i = 0; i < invoke.Count; ++i)
            {
                invoke[i].Invoke(null, null);
            }
        }

        private static void AddMethods(this Assembly assembly, string method, List<MethodInfo> list)
        {
            var types = assembly.GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                var m = types[i].GetMethod(method, BindingFlags.Static | BindingFlags.Public);
                if (m != null)
                {
                    list.Add(m);
                }
            }
        }


        public static TypeCache GetTypeCache(Assembly asm)
        {
            if (asm == null)
            {
                return m_NullCache ??= new TypeCache(null);
            }

            if (m_TypeCaches.TryGetValue(asm, out var c))
            {
                return c;
            }

            return m_TypeCaches[asm] = new TypeCache(asm);
        }

        public static Type FindTypeByFullName(string name, bool ignoreCase = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (ignoreCase)
            {
                name = name.ToLower();
            }

            for (var i = 0; i < Assemblies.Length; i++)
            {
                foreach (var type in GetTypeCache(Assemblies[i]).GetEnumerator(name, ignoreCase))
                {
                    if (type.FullName.EqualsOrdinal(name))
                    {
                        return type;
                    }
                }
            }

            foreach(var type in GetTypeCache(Core.Assembly).GetEnumerator(name, ignoreCase))
            {
                if (type.FullName.EqualsOrdinal(name))
                {
                    return type;
                }
            }

            return null;
        }

        public static Type FindTypeByName(string name, bool ignoreCase = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (ignoreCase)
            {
                name = name.ToLower();
            }

            for (var i = 0; i < Assemblies.Length; i++)
            {
                foreach (var type in GetTypeCache(Assemblies[i]).GetEnumerator(name, ignoreCase))
                {
                    return type;
                }
            }

            foreach(var type in GetTypeCache(Core.Assembly).GetEnumerator(name, ignoreCase))
            {
                return type;
            }

            return null;
        }

        // TODO: Change to IEnumerable using another custom enumerator
        public static List<Type> FindTypesByFullName(string name, bool ignoreCase = true)
        {
            var types = new List<Type>();

            if (ignoreCase)
            {
                name = name.ToLower();
            }

            for (var i = 0; i < Assemblies.Length; i++)
            {
                foreach (var type in GetTypeCache(Assemblies[i]).GetEnumerator(name, ignoreCase))
                {
                    if (type.FullName.EqualsOrdinal(name))
                    {
                        types.Add(type);
                    }
                }
            }

            foreach(var type in GetTypeCache(Core.Assembly).GetEnumerator(name, ignoreCase))
            {
                if (type.FullName.EqualsOrdinal(name))
                {
                    types.Add(type);
                }
            }

            return types;
        }

        // TODO: Change to IEnumerable using another custom enumerator
        public static List<Type> FindTypesByName(string name, bool ignoreCase = true)
        {
            var types = new List<Type>();

            if (ignoreCase)
            {
                name = name.ToLower();
            }

            for (var i = 0; i < Assemblies.Length; i++)
            {
                foreach (var type in GetTypeCache(Assemblies[i]).GetEnumerator(name, ignoreCase))
                {
                    types.Add(type);
                }
            }

            foreach(var type in GetTypeCache(Core.Assembly).GetEnumerator(name, ignoreCase))
            {
                types.Add(type);
            }

            return types;
        }

        public static string EnsureDirectory(string dir)
        {
            var path = Path.Combine(Core.BaseDirectory, dir);
            Directory.CreateDirectory(path);

            return path;
        }
    }

    public class TypeCache
    {
        private readonly Dictionary<string, int[]> _nameMap = new();
        private readonly Dictionary<string, int[]> _nameMapInsensitive = new();

        public TypeCache(Assembly asm)
        {
            Types = asm?.GetTypes() ?? Type.EmptyTypes;

            var nameMap = new Dictionary<string, HashSet<int>>();
            var nameMapInsensitive = new Dictionary<string, HashSet<int>>();

            void addToRefs(int index, string key, Dictionary<string, HashSet<int>> map)
            {
                if (key == null)
                {
                    return;
                }

                if (map.TryGetValue(key, out var refs))
                {
                    refs.Add(index);
                }
                else
                {
                    refs = new HashSet<int> { index };
                    map.Add(key, refs);
                }
            }

            var aliasType = typeof(TypeAliasAttribute);
            for (var i = 0; i < Types.Length; i++)
            {
                var current = Types[i];
                addToRefs(i, current.Name, nameMap);
                addToRefs(i, current.Name.ToLower(), nameMapInsensitive);
                addToRefs(i, current.FullName, nameMap);
                addToRefs(i, current.FullName?.ToLower(), nameMapInsensitive);
                if (current.GetCustomAttribute(aliasType, false) is TypeAliasAttribute alias)
                {
                    for (var j = 0; j < alias.Aliases.Length; j++)
                    {
                        addToRefs(i, alias.Aliases[j], nameMap);
                        addToRefs(i, alias.Aliases[j], nameMapInsensitive);
                    }
                }
            }

            foreach (var (key, value) in nameMap)
            {
                _nameMap[key] = value.ToArray();
            }

            foreach (var (key, value) in nameMapInsensitive)
            {
                _nameMapInsensitive[key] = value.ToArray();
            }
        }

        public Enumerator GetEnumerator(string name, bool ignoreCase) => new(name, this, ignoreCase);

        public Type[] Types { get; }

        public struct Enumerator : IEnumerable<Type>, IEnumerator<Type>
        {
            private readonly TypeCache _cache;
            private readonly int[] _values;
            private int _index;
            private Type _current;

            internal Enumerator(string name, TypeCache cache, bool ignoreCase)
            {
                _cache = cache;

                var map = ignoreCase ? _cache._nameMapInsensitive : _cache._nameMap;
                _values = map.TryGetValue(name, out var values) ? values : Array.Empty<int>();
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                int[] localList = _values;

                while ((uint)_index < (uint)localList.Length)
                {
                    _current = _cache.Types[_values[_index++]];

                    if (_current != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            public Type Current => _current!;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _values.Length + 1)
                    {
                        throw new InvalidOperationException(nameof(_index));
                    }

                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }

            public IEnumerator<Type> GetEnumerator() => this;

            IEnumerator IEnumerable.GetEnumerator() => this;
        }
    }
}
