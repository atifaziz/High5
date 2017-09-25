using System;
using ParseFive.Extensions;
using System.Text;
using NeAttrsMap = System.Collections.Generic.Dictionary<string, string>;
using ParseFive.Tokenizer;
//using Attrs = ParseFive.Extensions.List<Attr>;


public abstract class IEntry
{
    public string type { get; set; }
    public Element element { get; set; }
    public Token token { get; set; }
}

public class MarkerEntry : IEntry
{
    public MarkerEntry(string type)
    {
        this.type = type;
    }
}

public class ElementEntry : IEntry
{
    public ElementEntry(string type, Element element, Token token)
    {
        this.type = type;
        this.element = element;
        this.token = token;
    }
}

public class Node
{
    public Node ParentNode { get; internal set; }
    public System.Collections.Generic.List<Node> ChildNodes { get; } = new System.Collections.Generic.List<Node>();
}
public class Document : Node
{
    public string Mode { get; internal set; }
}
public class DocumentFragment : Node {}
public class Element : Node {}
public class Comment : Node {}
public class Text : Node {}
public class DocumentType : Node {}

namespace ParseFive.Parser
{
    class FormattingElementList
    {
        const int NOAH_ARK_CAPACITY = 3;

        public int length;
        public List<IEntry> entries;
        public TreeAdapter treeAdapter;
        public object bookmark;

        public const string MARKER_ENTRY = "MARKER_ENTRY";
        public const string ELEMENT_ENTRY = "ELEMENT_ENTRY";

        public FormattingElementList(TreeAdapter treeAdapter)
        {
            this.treeAdapter = treeAdapter;
            length = 0;
            entries = new List<IEntry>();
        }

        private List<(int idx, List<Attr> attrs)> getNoahArkConditionCandidates(Element newElement)
        {
            var candidates = new List<(int idx, List<Attr> attrs)>();

            if (length >= NOAH_ARK_CAPACITY)
            {
                var neAttrsLength = this.treeAdapter.getAttrList(newElement).length;
                var neTagName = this.treeAdapter.getTagName(newElement);
                var neNamespaceURI = this.treeAdapter.getNamespaceURI(newElement);

                for (var i = this.length - 1; i >= 0; i--)
                {
                    var entry = this.entries[i];

                    if (entry.type == MARKER_ENTRY)
                        break;

                    var element = entry.element;
                    var elementAttrs = this.treeAdapter.getAttrList(element);
                    var isCandidate = this.treeAdapter.getTagName(element) == neTagName &&
                                      this.treeAdapter.getNamespaceURI(element) == neNamespaceURI &&
                                      elementAttrs.length == neAttrsLength;

                    if (isCandidate)
                        candidates.push((idx: i, attrs: elementAttrs));
                }
            }

            return candidates.Count < NOAH_ARK_CAPACITY ? new List<(int, List<Attr>)>() : candidates;
        }

        private void ensureNoahArkCondition(Element newElement)
        {
            var candidates = this.getNoahArkConditionCandidates(newElement);
            var cLength = candidates.length;

            if (cLength)
            {
                var neAttrs = this.treeAdapter.getAttrList(newElement);
                var neAttrsLength = neAttrs.length;
                var neAttrsMap = new NeAttrsMap();

                for (var i = 0; i < neAttrsLength; i++)
                {
                    var neAttr = neAttrs[i];

                    neAttrsMap.Add(neAttr.name, neAttr.value);
                    //neAttrsMap[neAttr.name] = neAttr.value;
                }

                for (var i = 0; i < neAttrsLength; i++)
                {
                    for (var j = 0; j < cLength; j++)
                    {
                        var cAttr = candidates[j].attrs[i];

                        if (neAttrsMap[cAttr.name] != cAttr.value)
                        {
                            candidates.splice(j, 1);
                            cLength--;
                        }

                        if (candidates.length < NOAH_ARK_CAPACITY)
                            return;
                    }
                }

                //NOTE: remove bottommost candidates until Noah's Ark condition will not be met
                for (var i = cLength - 1; i >= NOAH_ARK_CAPACITY - 1; i--)
                {
                    this.entries.splice(candidates[i].idx, 1);
                    this.length--;
                }
            }
        }

        public void insertMarker()
        {
            entries.push(new MarkerEntry(MARKER_ENTRY));
            length++;
        }

        public void pushElement(Element element, Token token)
        {
            this.ensureNoahArkCondition(element);

            this.entries.push(new ElementEntry(ELEMENT_ENTRY, element, token));
            length++;
        }

        public void removeEntry(IEntry entry)
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

        public void clearToLastMarker()
        {
            while (this.length.IsTruthy())
            {
                var entry = this.entries.pop();

                this.length--;

                if (entry.type == FormattingElementList.MARKER_ENTRY)
                    break;
            }
        }

        //Search
        public IEntry getElementEntryInScopeWithTagName(string tagName)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                var entry = this.entries[i];

                if (entry.type == FormattingElementList.MARKER_ENTRY)
                    return null;

                if (this.treeAdapter.getTagName(entry.element) == tagName)
                    return entry;
            }

            return null;
        }

        public IEntry getElementEntry(Element element)
        {
            for (var i = this.length - 1; i >= 0; i--)
            {
                var entry = this.entries[i];

                if (entry.type == ELEMENT_ENTRY && entry.element == element)
                    return entry;
            }

            return null;
        }

        internal void insertElementAfterBookmark(Element newElement, Token token)
        {
            throw new NotImplementedException();
        }
    }
}
