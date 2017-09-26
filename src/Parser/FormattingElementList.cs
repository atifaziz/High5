namespace ParseFive.Parser
{
    using Extensions;
    using NeAttrsMap = System.Collections.Generic.Dictionary<string, string>;
    using Tokenizer;

    abstract class Entry
    {
        public string Type { get; }
        public Element Element { get; set; }
        public Token Token { get; }

        protected Entry(string type, Element element = null, Token token = null)
        {
            Type = type;
            Element = element;
            Token = token;
        }
    }

    sealed class MarkerEntry : Entry
    {
        public MarkerEntry(string type) :
            base(type) {}
    }

    sealed class ElementEntry : Entry
    {
        public ElementEntry(string type, Element element, Token token) :
            base(type, element, token) {}
    }

    sealed class FormattingElementList
    {
        // Const

        const int NoahArkCapacity = 3;

        readonly TreeAdapter treeAdapter;
        int length;
        readonly List<Entry> entries;

        public object Bookmark { get; set; }
        public int Length => this.length;
        public Entry this[int index] => this.entries[index];

        // Entry types

        public const string MARKER_ENTRY = "MARKER_ENTRY";
        public const string ELEMENT_ENTRY = "ELEMENT_ENTRY";

        public FormattingElementList(TreeAdapter treeAdapter)
        {
            this.treeAdapter = treeAdapter;
            length = 0;
            entries = new List<Entry>();
        }

        // Noah Ark's condition
        // OPTIMIZATION: at first we try to find possible candidates for exclusion using
        // lightweight heuristics without thorough attributes check.

        List<(int idx, List<Attr> attrs)> GetNoahArkConditionCandidates(Element newElement)
        {
            var candidates = new List<(int idx, List<Attr> attrs)>();

            if (length >= NoahArkCapacity)
            {
                var neAttrsLength = this.treeAdapter.getAttrList(newElement).length;
                var neTagName = this.treeAdapter.getTagName(newElement);
                var neNamespaceUri = this.treeAdapter.getNamespaceURI(newElement);

                for (var i = this.length - 1; i >= 0; i--)
                {
                    var entry = this.entries[i];

                    if (entry.Type == MARKER_ENTRY)
                        break;

                    var element = entry.Element;
                    var elementAttrs = this.treeAdapter.getAttrList(element);
                    var isCandidate = this.treeAdapter.getTagName(element) == neTagName &&
                                      this.treeAdapter.getNamespaceURI(element) == neNamespaceUri &&
                                      elementAttrs.length == neAttrsLength;

                    if (isCandidate)
                        candidates.push((idx: i, attrs: elementAttrs));
                }
            }

            return candidates.Count < NoahArkCapacity ? new List<(int, List<Attr>)>() : candidates;
        }

        void EnsureNoahArkCondition(Element newElement)
        {
            var candidates = this.GetNoahArkConditionCandidates(newElement);
            var cLength = candidates.length;

            if (cLength.IsTruthy())
            {
                var neAttrs = this.treeAdapter.getAttrList(newElement);
                var neAttrsLength = neAttrs.length;
                var neAttrsMap = new NeAttrsMap();

                // NOTE: build attrs map for the new element so we can perform fast lookups

                for (var i = 0; i < neAttrsLength; i++)
                {
                    var neAttr = neAttrs[i];

                    neAttrsMap.Add(neAttr.name, neAttr.value);
                    // neAttrsMap[neAttr.name] = neAttr.value;
                }

                for (var i = 0; i < neAttrsLength; i++)
                {
                    for (var j = 0; j < cLength; j++)
                    {
                        var cAttr = candidates[j].attrs[i];

                        if ((neAttrsMap.TryGetValue(cAttr.name, out var v) ? v : null) != cAttr.value)
                        {
                            candidates.splice(j, 1);
                            cLength--;
                        }

                        if (candidates.length < NoahArkCapacity)
                            return;
                    }
                }

                // NOTE: remove bottommost candidates until Noah's Ark condition will not be met

                for (var i = cLength - 1; i >= NoahArkCapacity - 1; i--)
                {
                    this.entries.splice(candidates[i].idx, 1);
                    this.length--;
                }
            }
        }

        // Mutations

        public void InsertMarker()
        {
            entries.push(new MarkerEntry(MARKER_ENTRY));
            length++;
        }

        public void PushElement(Element element, Token token)
        {
            this.EnsureNoahArkCondition(element);

            this.entries.push(new ElementEntry(ELEMENT_ENTRY, element, token));
            length++;
        }

        internal void InsertElementAfterBookmark(Element element, Token token)
        {
            var bookmarkIdx = this.length - 1;

            for (; bookmarkIdx >= 0; bookmarkIdx--)
            {
                if (this.entries[bookmarkIdx] == this.Bookmark)
                    break;
            }

            this.entries.splice(bookmarkIdx + 1, 0, new ElementEntry(ELEMENT_ENTRY, element, token));

            this.length++;
        }

        public void RemoveEntry(Entry entry)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                if (this.entries[i] == entry)
                {
                    this.entries.splice(i, 1);
                    this.length--;
                    break;
                }
            }
        }

        public void ClearToLastMarker()
        {
            while (this.length.IsTruthy())
            {
                var entry = this.entries.pop();

                this.length--;

                if (entry.Type == MARKER_ENTRY)
                    break;
            }
        }

        // Search

        public Entry GetElementEntryInScopeWithTagName(string tagName)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                var entry = this.entries[i];

                if (entry.Type == MARKER_ENTRY)
                    return null;

                if (this.treeAdapter.getTagName(entry.Element) == tagName)
                    return entry;
            }

            return null;
        }

        public Entry GetElementEntry(Element element)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                var entry = this.entries[i];

                if (entry.Type == ELEMENT_ENTRY && entry.Element == element)
                    return entry;
            }

            return null;
        }
    }
}
