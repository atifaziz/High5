using System;
using ParseFive.Extensions;
using ɑ = HTML.TAG_NAMES;
using NS = HTML.NAMESPACES;

namespace ParseFive.Parser
{
    class OpenElementStack
    {
        class TreeAdapter
        {
            public readonly Func<Element, string> getNamespaceURI;
            public readonly Func<Element, string> getTagName;
            public readonly Func<Element, Node> getTemplateContent;

            public TreeAdapter(Func<Element, string> getNamespaceUri,
                               Func<Element, string> getTagName,
                               Func<Element, Node> getTemplateContent)
            {
                getNamespaceURI = getNamespaceUri;
                this.getTagName = getTagName;
                this.getTemplateContent = getTemplateContent;
            }
        }


        readonly TreeAdapter treeAdapter;
        public Node currentTmplContent { get; set; }
        public Node current { get; set; }
        public List<Element> items;
        public string currentTagName { get; private set; }
        public int tmplCount { get; private set; }
        public int stackTop;

        //Stack of open elements
        public OpenElementStack(Node document, ParseFive.TreeAdapter treeAdapter)
        {
            this.stackTop = -1;
            this.items = new List<Element>();
            this.current = document;
            this.currentTagName = null;
            this.currentTmplContent = null;
            this.tmplCount = 0;
            this.treeAdapter = new TreeAdapter(treeAdapter.getNamespaceURI,
                                               treeAdapter.getTagName,
                                               treeAdapter.getTemplateContent);
        }

        public static bool isImpliedEndTagRequired(string tn)
        {
            switch (tn.Length)
            {
                case 1:
                    return tn == ɑ.P;

                case 2:
                    return tn == ɑ.RB || tn == ɑ.RP || tn == ɑ.RT || tn == ɑ.DD || tn == ɑ.DT || tn == ɑ.LI;

                case 3:
                    return tn == ɑ.RTC;

                case 6:
                    return tn == ɑ.OPTION;

                case 8:
                    return tn == ɑ.OPTGROUP || tn == ɑ.MENUITEM;
            }

            return false;
        }

        public static bool isScopingElement(string tn, string ns)
        {
            switch (tn.Length)
            {
                case 2:
                    if (tn == ɑ.TD || tn == ɑ.TH)
                        return ns == NS.HTML;

                    else if (tn == ɑ.MI || tn == ɑ.MO || tn == ɑ.MN || tn == ɑ.MS)
                        return ns == NS.MATHML;

                    break;

                case 4:
                    if (tn == ɑ.HTML)
                        return ns == NS.HTML;

                    else if (tn == ɑ.DESC)
                        return ns == NS.SVG;

                    break;

                case 5:
                    if (tn == ɑ.TABLE)
                        return ns == NS.HTML;

                    else if (tn == ɑ.MTEXT)
                        return ns == NS.MATHML;

                    else if (tn == ɑ.TITLE)
                        return ns == NS.SVG;

                    break;

                case 6:
                    return (tn == ɑ.APPLET || tn == ɑ.OBJECT) && ns == NS.HTML;

                case 7:
                    return (tn == ɑ.CAPTION || tn == ɑ.MARQUEE) && ns == NS.HTML;

                case 8:
                    return tn == ɑ.TEMPLATE && ns == NS.HTML;

                case 13:
                    return tn == ɑ.FOREIGN_OBJECT && ns == NS.SVG;

                case 14:
                    return tn == ɑ.ANNOTATION_XML && ns == NS.MATHML;
            }

            return false;
        }



        //Index of element
        public int _indexOf(Element element)
        {
            var idx = -1;

            for (var i = this.stackTop; i >= 0; i--)
            {
                if (this.items[i] == element)
                {
                    idx = i;
                    break;
                }
            }
            return idx;
        }

        //Update current element
        public bool _isInTemplate()
        {
            return this.currentTagName == ɑ.TEMPLATE && this.treeAdapter.getNamespaceURI((Element) this.current) == NS.HTML;
        }

        public void _updateCurrentElement()
        {
            this.current = this.items[this.stackTop];
            this.currentTagName = this.current.IsTruthy() ? this.treeAdapter.getTagName((Element) this.current) : null;

            this.currentTmplContent = this._isInTemplate() ? this.treeAdapter.getTemplateContent((Element) this.current) : null;
        }

        //Mutations
        public void push(Element element)
        {
            this.items.Add(element);
            stackTop++;
            this._updateCurrentElement();

            if (this._isInTemplate())
                this.tmplCount++;

        }

        public void pop()
        {
            this.stackTop--;
            this.items.pop();

            if (this.tmplCount > 0 && this._isInTemplate())
                this.tmplCount--;

            this._updateCurrentElement();
        }

        public void replace(Element oldElement, Element newElement)
        {
            var idx = this._indexOf(oldElement);

            this.items[idx] = newElement;

            if (idx == this.stackTop)
                this._updateCurrentElement();
        }

        public void insertAfter(Element referenceElement, Element newElement)
        {
            var insertionIdx = this._indexOf(referenceElement) + 1;

            this.items.splice(insertionIdx, 0, newElement);

            if (insertionIdx == ++this.stackTop)
                this._updateCurrentElement();
        }

        public void popUntilTagNamePopped(string tagName)
        {
            while (this.stackTop > -1)
            {
                string tn = this.currentTagName;
                string ns = this.treeAdapter.getNamespaceURI((Element) this.current);

                this.pop();

                if (tn == tagName && ns == NS.HTML)
                    break;
            }
        }

        public void popUntilElementPopped(Element element)
        {
            while (this.stackTop > -1)
            {
                var poppedElement = this.current;

                this.pop();

                if (poppedElement == element)
                    break;
            }
        }

        public void popUntilNumberedHeaderPopped()
        {
            while (this.stackTop > -1)
            {
                string tn = this.currentTagName,
                    ns = this.treeAdapter.getNamespaceURI((Element) this.current);

                this.pop();

                if (tn == ɑ.H1 || tn == ɑ.H2 || tn == ɑ.H3 || tn == ɑ.H4 || tn == ɑ.H5 || tn == ɑ.H6 && ns == NS.HTML)
                    break;
            }
        }

        public void popUntilTableCellPopped()
        {
            while (this.stackTop > -1)
            {
                string tn = this.currentTagName,
                    ns = this.treeAdapter.getNamespaceURI((Element) this.current);

                this.pop();

                if (tn == ɑ.TD || tn == ɑ.TH && ns == NS.HTML)
                    break;
            }
        }

        public void popAllUpToHtmlElement()
        {
            //NOTE: here we assume that root <html> element is always first in the open element stack, so
            //we perform this fast stack clean up.
            this.stackTop = 0;
            this._updateCurrentElement();
        }

        public void clearBackToTableContext()
        {
            while (this.currentTagName != ɑ.TABLE &&
                   this.currentTagName != ɑ.TEMPLATE &&
                   this.currentTagName != ɑ.HTML ||
                   this.treeAdapter.getNamespaceURI((Element) this.current) != NS.HTML)
                this.pop();
        }

        public void clearBackToTableBodyContext()
        {
            while (this.currentTagName != ɑ.TBODY &&
                   this.currentTagName != ɑ.TFOOT &&
                   this.currentTagName != ɑ.THEAD &&
                   this.currentTagName != ɑ.TEMPLATE &&
                   this.currentTagName != ɑ.HTML ||
                   this.treeAdapter.getNamespaceURI((Element) this.current) != NS.HTML)
                this.pop();
        }

        public void clearBackToTableRowContext()
        {
            while (this.currentTagName != ɑ.TR &&
                   this.currentTagName != ɑ.TEMPLATE &&
                   this.currentTagName != ɑ.HTML ||
                   this.treeAdapter.getNamespaceURI((Element) this.current) != NS.HTML)
                this.pop();
        }

        public void remove(Element element)
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                if (this.items[i] == element)
                {
                    this.items.splice(i, 1);
                    this.stackTop--;
                    this._updateCurrentElement();
                    break;
                }
            }
        }

        //Search
        public Element tryPeekProperlyNestedBodyElement()
        {
            //Properly nested <body> element (should be second element in stack).
            var element = this.items[1];

            return element.IsTruthy() && this.treeAdapter.getTagName(element) == ɑ.BODY ? element : null;
        }

        public bool contains(Element element)
        {
            return this._indexOf(element) > -1;
        }

        public Element getCommonAncestor(Element element)
        {
            var elementIdx = this._indexOf(element);

            return --elementIdx >= 0 ? this.items[elementIdx] : null;
        }

        public bool isRootHtmlElementCurrent()
        {
            return this.stackTop == 0 && this.currentTagName == ɑ.HTML;
        }

        //Element in scope
        public bool hasInScope(string tagName)
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if (tn == tagName && ns == NS.HTML)
                    return true;

                if (isScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool hasNumberedHeaderInScope()
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if ((tn == ɑ.H1 || tn == ɑ.H2 || tn == ɑ.H3 || tn == ɑ.H4 || tn == ɑ.H5 || tn == ɑ.H6) && ns == NS.HTML)
                    return true;

                if (isScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool hasInListItemScope(string tagName)
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if (tn == tagName && ns == NS.HTML)
                    return true;

                if ((tn == ɑ.UL || tn == ɑ.OL) && ns == NS.HTML || isScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool hasInButtonScope(string tagName)
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if (tn == tagName && ns == NS.HTML)
                    return true;

                if (tn == ɑ.BUTTON && ns == NS.HTML || isScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool hasInTableScope(string tagName)
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if (ns != NS.HTML)
                    continue;

                if (tn == tagName)
                    return true;

                if (tn == ɑ.TABLE || tn == ɑ.TEMPLATE || tn == ɑ.HTML)
                    return false;
            }

            return true;
        }

        public bool hasTableBodyContextInTableScope()
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if (ns != NS.HTML)
                    continue;

                if (tn == ɑ.TBODY || tn == ɑ.THEAD || tn == ɑ.TFOOT)
                    return true;

                if (tn == ɑ.TABLE || tn == ɑ.HTML)
                    return false;
            }

            return true;
        }

        public bool hasInSelectScope(string tagName)
        {
            for (var i = this.stackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.getTagName(this.items[i]);
                string ns = this.treeAdapter.getNamespaceURI(this.items[i]);

                if (ns != NS.HTML)
                    continue;

                if (tn == tagName)
                    return true;

                if (tn != ɑ.OPTION && tn != ɑ.OPTGROUP)
                    return false;
            }

            return true;
        }

        //Implied end tags
        public void generateImpliedEndTags()
        {
            while (isImpliedEndTagRequired(this.currentTagName))
                this.pop();
        }

        public void generateImpliedEndTagsWithExclusion(string exclusionTagName)
        {
            while (isImpliedEndTagRequired(this.currentTagName) && this.currentTagName != exclusionTagName)
                this.pop();
        }

    }
}