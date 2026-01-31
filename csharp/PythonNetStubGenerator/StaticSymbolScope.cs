using System;
using System.Collections.Generic;

namespace PythonNetStubGenerator
{
    public class StaticSymbolScope : IDisposable
    {

        public readonly List<string> ReservedSymbols;
        public static readonly List<StaticSymbolScope> Scopes = new List<StaticSymbolScope>();
        public readonly string Namespace;

        public StaticSymbolScope(IEnumerable<string> reservedSymbols, string nameSpace)
        {
            Namespace = nameSpace;
            ReservedSymbols = new List<string>(reservedSymbols);
            Scopes.Add(this);
        }

        public void Dispose()
        {
            Scopes.Remove(this);
        }

        public bool HasConflict(string cleanName, string typeNamespace) =>
            typeNamespace != Namespace && ReservedSymbols.Contains(cleanName);
    }
}