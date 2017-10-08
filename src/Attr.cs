#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
//
// Portions Copyright (c) 2013 Ivan Nikulin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace ParseFive
{
    using System;
    using Microsoft.Extensions.Internal;

    struct Attr : IEquatable<Attr>
    {
        public string NamespaceUri { get; }
        public string Prefix       { get; }
        public string Name         { get; }
        public string Value        { get; }

        public Attr(string name, string value) :
            this(null, null, name, value) {}

        public Attr(string namespaceUri, string prefix, string name, string value)
        {
            NamespaceUri = namespaceUri;
            Prefix       = prefix;
            Name         = name;
            Value        = value;
        }

        public Attr WithName(string name) =>
            new Attr(NamespaceUri, Prefix, name, Value);

        public Attr WithValue(string value) =>
            new Attr(NamespaceUri, Prefix, Name, value);

        public bool Equals(Attr other) =>
            string.Equals(NamespaceUri, other.NamespaceUri)
            && string.Equals(Prefix, other.Prefix, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is Attr attribute && Equals(attribute);

        public override int GetHashCode()
        {
            var hashCode = HashCodeCombiner.Start();
            hashCode.Add(NamespaceUri);
            hashCode.Add(Prefix, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Value);
            return hashCode;
        }
    }
}
