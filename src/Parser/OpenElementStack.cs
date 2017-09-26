using System;
using System.Collections.Generic;
using ParseFive.Extensions;
using T = HTML.TAG_NAMES;
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
                    return tn == T.P;

                case 2:
                    return tn == T.RB || tn == T.RP || tn == T.RT || tn == T.DD || tn == T.DT || tn == T.LI;

                case 3:
                    return tn == T.RTC;

                case 6:
                    return tn == T.OPTION;

                case 8:
                    return tn == T.OPTGROUP || tn == T.MENUITEM;
            }

            return false;
        }

        public static bool isScopingElement(string tn, string ns)
        {
            switch (tn.Length)
            {
                case 2:
                    if (tn == T.TD || tn == T.TH)
                        return ns == NS.HTML;

                    else if (tn == T.MI || tn == T.MO || tn == T.MN || tn == T.MS)
                        return ns == NS.MATHML;

                    break;

                case 4:
                    if (tn == T.HTML)
                        return ns == NS.HTML;

                    else if (tn == T.DESC)
                        return ns == NS.SVG;

                    break;

                case 5:
                    if (tn == T.TABLE)
                        return ns == NS.HTML;

                    else if (tn == T.MTEXT)
                        return ns == NS.MATHML;

                    else if (tn == T.TITLE)
                        return ns == NS.SVG;

                    break;

                case 6:
                    return (tn == T.APPLET || tn == T.OBJECT) && ns == NS.HTML;

                case 7:
                    return (tn == T.CAPTION || tn == T.MARQUEE) && ns == NS.HTML;

                case 8:
                    return tn == T.TEMPLATE && ns == NS.HTML;

                case 13:
                    return tn == T.FOREIGN_OBJECT && ns == NS.SVG;

                case 14:
                    return tn == T.ANNOTATION_XML && ns == NS.MATHML;
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
            return this.currentTagName == T.TEMPLATE && this.treeAdapter.getNamespaceURI((Element) this.current) == NS.HTML;
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

                if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6 && ns == NS.HTML)
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

                if (tn == T.TD || tn == T.TH && ns == NS.HTML)
                    break;
            }
        }

        public void popAllUpToHtmlElement()
        {
            //NOTE: here we assume that root <html> element is always first in the open element stack, so
            //we perform this fast stack clean up.
            this.stackTop = 0;
            this.items = new List<Element> { this.items[0] };
            this._updateCurrentElement();
        }

        public void clearBackToTableContext()
        {
            while (this.currentTagName != T.TABLE &&
                   this.currentTagName != T.TEMPLATE &&
                   this.currentTagName != T.HTML ||
                   this.treeAdapter.getNamespaceURI((Element) this.current) != NS.HTML)
                this.pop();
        }

        public void clearBackToTableBodyContext()
        {
            while (this.currentTagName != T.TBODY &&
                   this.currentTagName != T.TFOOT &&
                   this.currentTagName != T.THEAD &&
                   this.currentTagName != T.TEMPLATE &&
                   this.currentTagName != T.HTML ||
                   this.treeAdapter.getNamespaceURI((Element) this.current) != NS.HTML)
                this.pop();
        }

        public void clearBackToTableRowContext()
        {
            while (this.currentTagName != T.TR &&
                   this.currentTagName != T.TEMPLATE &&
                   this.currentTagName != T.HTML ||
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
            var element = this.items.Count > 1 ? this.items[1] : null;

            return element.IsTruthy() && this.treeAdapter.getTagName(element) == T.BODY ? element : null;
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
            return this.stackTop == 0 && this.currentTagName == T.HTML;
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

                if ((tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6) && ns == NS.HTML)
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

                if ((tn == T.UL || tn == T.OL) && ns == NS.HTML || isScopingElement(tn, ns))
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

                if (tn == T.BUTTON && ns == NS.HTML || isScopingElement(tn, ns))
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

                if (tn == T.TABLE || tn == T.TEMPLATE || tn == T.HTML)
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

                if (tn == T.TBODY || tn == T.THEAD || tn == T.TFOOT)
                    return true;

                if (tn == T.TABLE || tn == T.HTML)
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

                if (tn != T.OPTION && tn != T.OPTGROUP)
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