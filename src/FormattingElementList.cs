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
    using System.Collections.Generic;
    using Extensions;
    using static Entry;

    abstract class Entry
    {
        public const string MARKER_ENTRY = "MARKER_ENTRY";
        public const string ELEMENT_ENTRY = "ELEMENT_ENTRY";

        public string Type { get; }

        protected Entry(string type) => Type = type;
    }

    sealed class MarkerEntry : Entry
    {
        public static readonly MarkerEntry Instance = new MarkerEntry();
        MarkerEntry() : base(MARKER_ENTRY) {}
    }

    sealed class ElementEntry<TElement> : Entry
        where TElement : class
    {
        public TElement Element { get; set; }
        public Token Token { get; }

        public ElementEntry(TElement element, Token token) :
            base(ELEMENT_ENTRY)
        {
            Element = element;
            Token = token;
        }
    }

    sealed class FormattingElementList<TElement, TAttr>
        where TElement : class
    {
        sealed class TreeAdapter
        {
            public readonly Func<TElement, string> GetNamespaceUri;
            public readonly Func<TElement, string> GetTagName;
            public readonly Func<TElement, int> GetAttrListCount;
            public readonly Func<TElement, ArraySegment<TAttr>, int> GetAttrList;
            public readonly Func<TAttr, (string, string)> GetAttr;

            public TreeAdapter(Func<TElement, string> getNamespaceUri,
                               Func<TElement, string> getTagName,
                               Func<TElement, int> getAttrListCount,
                               Func<TElement, ArraySegment<TAttr>, int> getAttrList,
                               Func<TAttr, (string, string)> getAttr)
            {
                GetNamespaceUri = getNamespaceUri;
                GetTagName = getTagName;
                GetAttrListCount = getAttrListCount;
                GetAttrList = getAttrList;
                GetAttr = getAttr;
            }
        }

        // Const

        const int NoahArkCapacity = 3;

        readonly TreeAdapter treeAdapter;
        int length;
        readonly List<Entry> entries;

        public object Bookmark { get; set; }
        public int Length => this.length;
        public Entry this[int index] => this.entries[index];

        // Entry types

        public FormattingElementList(Func<TElement, string> getNamespaceUri,
                                     Func<TElement, string> getTagName,
                                     Func<TElement, int> getAttrListCount,
                                     Func<TElement, ArraySegment<TAttr>, int> getAttrList,
                                     Func<TAttr, string> getAttrName,
                                     Func<TAttr, string> getAttrValue)
        {
            this.treeAdapter = new TreeAdapter(getNamespaceUri,
                                               getTagName,
                                               getAttrListCount,
                                               getAttrList,
                                               a => (getAttrName(a), getAttrValue(a)));
            length = 0;
            entries = new List<Entry>();
        }

        // Noah Ark's condition
        // OPTIMIZATION: at first we try to find possible candidates for exclusion using
        // lightweight heuristics without thorough attributes check.

        List<(int idx, (string Name, string Value)[] attrs)> GetNoahArkConditionCandidates(TElement newElement)
        {
            var candidates = new List<(int idx, (string, string)[] attrs)>();

            if (length >= NoahArkCapacity)
            {
                var neAttrsLength = this.treeAdapter.GetAttrListCount(newElement);
                var neTagName = this.treeAdapter.GetTagName(newElement);
                var neNamespaceUri = this.treeAdapter.GetNamespaceUri(newElement);
                var elementAttrObjs = new Array<TAttr>();

                for (var i = this.length - 1; i >= 0; i--)
                {
                    var entry = this.entries[i];

                    if (entry.Type == MARKER_ENTRY)
                        break;

                    var element = ((ElementEntry<TElement>) entry).Element;
                    var attrsLength = this.treeAdapter.GetAttrListCount(element);
                    elementAttrObjs.Init(attrsLength);
                    this.treeAdapter.GetAttrList(element, elementAttrObjs.ToArraySegment());
                    var elementAttrs = new (string, string)[attrsLength];
                    for (var ai = 0; ai < attrsLength; ai++)
                        elementAttrs[ai] = this.treeAdapter.GetAttr(elementAttrObjs[ai]);
                    var isCandidate = this.treeAdapter.GetTagName(element) == neTagName &&
                                      this.treeAdapter.GetNamespaceUri(element) == neNamespaceUri &&
                                      attrsLength == neAttrsLength;

                    if (isCandidate)
                        candidates.Push((idx: i, attrs: elementAttrs));
                }
            }

            return candidates.Count < NoahArkCapacity ? new List<(int, (string, string)[])>() : candidates;
        }

        void EnsureNoahArkCondition(TElement newElement)
        {
            var candidates = this.GetNoahArkConditionCandidates(newElement);
            var cLength = candidates.Count;

            if (cLength > 0)
            {
                var neAttrsLength = this.treeAdapter.GetAttrListCount(newElement);
                var neAttrObjs = new Array<TAttr>();
                neAttrObjs.Init(neAttrsLength);
                this.treeAdapter.GetAttrList(newElement, neAttrObjs.ToArraySegment());
                var neAttrs = new Array<(string Name, string Value)>(neAttrsLength);
                for (var i = 0; i < neAttrsLength; i++)
                    neAttrs.Add(this.treeAdapter.GetAttr(neAttrObjs[i]));

                var neAttrsMap = new Dictionary<string, string>();

                // NOTE: build attrs map for the new element so we can perform fast lookups

                for (var i = 0; i < neAttrsLength; i++)
                {
                    var neAttr = neAttrs[i];

                    neAttrsMap.Add(neAttr.Name, neAttr.Value);
                    // neAttrsMap[neAttr.name] = neAttr.value;
                }

                for (var i = 0; i < neAttrsLength; i++)
                {
                    for (var j = 0; j < cLength; j++)
                    {
                        var cAttr = candidates[j].attrs[i];

                        if ((neAttrsMap.TryGetValue(cAttr.Name, out var v) ? v : null) != cAttr.Value)
                        {
                            candidates.Splice(j, 1);
                            cLength--;
                        }

                        if (candidates.Count < NoahArkCapacity)
                            return;
                    }
                }

                // NOTE: remove bottommost candidates until Noah's Ark condition will not be met

                for (var i = cLength - 1; i >= NoahArkCapacity - 1; i--)
                {
                    this.entries.Splice(candidates[i].idx, 1);
                    this.length--;
                }
            }
        }

        // Mutations

        public void InsertMarker()
        {
            entries.Push(MarkerEntry.Instance);
            length++;
        }

        public void PushElement(TElement element, Token token)
        {
            this.EnsureNoahArkCondition(element);

            this.entries.Push(new ElementEntry<TElement>(element, token));
            length++;
        }

        internal void InsertElementAfterBookmark(TElement element, Token token)
        {
            var bookmarkIdx = this.length - 1;

            for (; bookmarkIdx >= 0; bookmarkIdx--)
            {
                if (this.entries[bookmarkIdx] == this.Bookmark)
                    break;
            }

            this.entries.Splice(bookmarkIdx + 1, 0, new ElementEntry<TElement>(element, token));

            this.length++;
        }

        public void RemoveEntry(Entry entry)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                if (this.entries[i] == entry)
                {
                    this.entries.Splice(i, 1);
                    this.length--;
                    break;
                }
            }
        }

        public void ClearToLastMarker()
        {
            while (this.length > 0)
            {
                var entry = this.entries.Pop();

                this.length--;

                if (entry.Type == MARKER_ENTRY)
                    break;
            }
        }

        // Search

        public ElementEntry<TElement> GetElementEntryInScopeWithTagName(string tagName)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                var entry = this.entries[i];

                if (entry.Type == MARKER_ENTRY)
                    return null;

                var elementEntry = (ElementEntry<TElement>) entry;
                if (this.treeAdapter.GetTagName(elementEntry.Element) == tagName)
                    return elementEntry;
            }

            return null;
        }

        public ElementEntry<TElement> GetElementEntry(TElement element)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                if (this.entries[i] is ElementEntry<TElement> entry && entry.Element == element)
                    return entry;
            }

            return null;
        }
    }
}
