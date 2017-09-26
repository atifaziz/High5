using System;
using System.Collections.Generic;
using ParseFive.Extensions;
using T = HTML.TAG_NAMES;
using NS = HTML.NAMESPACES;

namespace ParseFive.Parser
{
    sealed class OpenElementStack
    {
        sealed class TreeAdapter
        {
            public readonly Func<Element, string> GetNamespaceUri;
            public readonly Func<Element, string> GetTagName;
            public readonly Func<Element, Node> GetTemplateContent;

            public TreeAdapter(Func<Element, string> getNamespaceUri,
                               Func<Element, string> getTagName,
                               Func<Element, Node> getTemplateContent)
            {
                GetNamespaceUri = getNamespaceUri;
                GetTagName = getTagName;
                GetTemplateContent = getTemplateContent;
            }
        }

        readonly TreeAdapter treeAdapter;
        public Node CurrentTmplContent { get; private set; }
        public Node Current { get; private set; }
        List<Element> items;
        public Element this[int index] => items[index];
        public string CurrentTagName { get; private set; }
        public int TmplCount { get; private set; }
        public int StackTop { get; private set; }

        //Stack of open elements
        public OpenElementStack(Node document, ITreeAdapter treeAdapter)
        {
            this.StackTop = -1;
            this.items = new List<Element>();
            this.Current = document;
            this.CurrentTagName = null;
            this.CurrentTmplContent = null;
            this.TmplCount = 0;
            this.treeAdapter = new TreeAdapter(treeAdapter.GetNamespaceUri,
                                               treeAdapter.GetTagName,
                                               treeAdapter.GetTemplateContent);
        }

        static bool IsImpliedEndTagRequired(string tn)
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

        static bool IsScopingElement(string tn, string ns)
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
        int IndexOf(Element element)
        {
            var idx = -1;

            for (var i = this.StackTop; i >= 0; i--)
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
        bool IsInTemplate()
        {
            return this.CurrentTagName == T.TEMPLATE && this.treeAdapter.GetNamespaceUri((Element) this.Current) == NS.HTML;
        }

        void UpdateCurrentElement()
        {
            this.Current = this.items[this.StackTop];
            this.CurrentTagName = this.Current.IsTruthy() ? this.treeAdapter.GetTagName((Element) this.Current) : null;

            this.CurrentTmplContent = this.IsInTemplate() ? this.treeAdapter.GetTemplateContent((Element) this.Current) : null;
        }

        //Mutations
        public void Push(Element element)
        {
            this.items.Add(element);
            StackTop++;
            this.UpdateCurrentElement();

            if (this.IsInTemplate())
                this.TmplCount++;

        }

        public void Pop()
        {
            this.StackTop--;
            this.items.pop();

            if (this.TmplCount > 0 && this.IsInTemplate())
                this.TmplCount--;

            this.UpdateCurrentElement();
        }

        public void Replace(Element oldElement, Element newElement)
        {
            var idx = this.IndexOf(oldElement);

            this.items[idx] = newElement;

            if (idx == this.StackTop)
                this.UpdateCurrentElement();
        }

        public void InsertAfter(Element referenceElement, Element newElement)
        {
            var insertionIdx = this.IndexOf(referenceElement) + 1;

            this.items.splice(insertionIdx, 0, newElement);

            if (insertionIdx == ++this.StackTop)
                this.UpdateCurrentElement();
        }

        public void PopUntilTagNamePopped(string tagName)
        {
            while (this.StackTop > -1)
            {
                string tn = this.CurrentTagName;
                string ns = this.treeAdapter.GetNamespaceUri((Element) this.Current);

                this.Pop();

                if (tn == tagName && ns == NS.HTML)
                    break;
            }
        }

        public void PopUntilElementPopped(Element element)
        {
            while (this.StackTop > -1)
            {
                var poppedElement = this.Current;

                this.Pop();

                if (poppedElement == element)
                    break;
            }
        }

        public void PopUntilNumberedHeaderPopped()
        {
            while (this.StackTop > -1)
            {
                string tn = this.CurrentTagName,
                    ns = this.treeAdapter.GetNamespaceUri((Element) this.Current);

                this.Pop();

                if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6 && ns == NS.HTML)
                    break;
            }
        }

        public void PopUntilTableCellPopped()
        {
            while (this.StackTop > -1)
            {
                string tn = this.CurrentTagName,
                    ns = this.treeAdapter.GetNamespaceUri((Element) this.Current);

                this.Pop();

                if (tn == T.TD || tn == T.TH && ns == NS.HTML)
                    break;
            }
        }

        public void PopAllUpToHtmlElement()
        {
            //NOTE: here we assume that root <html> element is always first in the open element stack, so
            //we perform this fast stack clean up.
            this.StackTop = 0;
            this.items = new List<Element> { this.items[0] };
            this.UpdateCurrentElement();
        }

        public void ClearBackToTableContext()
        {
            while (this.CurrentTagName != T.TABLE &&
                   this.CurrentTagName != T.TEMPLATE &&
                   this.CurrentTagName != T.HTML ||
                   this.treeAdapter.GetNamespaceUri((Element) this.Current) != NS.HTML)
                this.Pop();
        }

        public void ClearBackToTableBodyContext()
        {
            while (this.CurrentTagName != T.TBODY &&
                   this.CurrentTagName != T.TFOOT &&
                   this.CurrentTagName != T.THEAD &&
                   this.CurrentTagName != T.TEMPLATE &&
                   this.CurrentTagName != T.HTML ||
                   this.treeAdapter.GetNamespaceUri((Element) this.Current) != NS.HTML)
                this.Pop();
        }

        public void ClearBackToTableRowContext()
        {
            while (this.CurrentTagName != T.TR &&
                   this.CurrentTagName != T.TEMPLATE &&
                   this.CurrentTagName != T.HTML ||
                   this.treeAdapter.GetNamespaceUri((Element) this.Current) != NS.HTML)
                this.Pop();
        }

        public void Remove(Element element)
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                if (this.items[i] == element)
                {
                    this.items.splice(i, 1);
                    this.StackTop--;
                    this.UpdateCurrentElement();
                    break;
                }
            }
        }

        //Search
        public Element TryPeekProperlyNestedBodyElement()
        {
            //Properly nested <body> element (should be second element in stack).
            var element = this.items.Count > 1 ? this.items[1] : null;

            return element.IsTruthy() && this.treeAdapter.GetTagName(element) == T.BODY ? element : null;
        }

        public bool Contains(Element element)
        {
            return this.IndexOf(element) > -1;
        }

        public Element GetCommonAncestor(Element element)
        {
            var elementIdx = this.IndexOf(element);

            return --elementIdx >= 0 ? this.items[elementIdx] : null;
        }

        public bool IsRootHtmlElementCurrent()
        {
            return this.StackTop == 0 && this.CurrentTagName == T.HTML;
        }

        //Element in scope
        public bool HasInScope(string tagName)
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

                if (tn == tagName && ns == NS.HTML)
                    return true;

                if (IsScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool HasNumberedHeaderInScope()
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

                if ((tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6) && ns == NS.HTML)
                    return true;

                if (IsScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool HasInListItemScope(string tagName)
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

                if (tn == tagName && ns == NS.HTML)
                    return true;

                if ((tn == T.UL || tn == T.OL) && ns == NS.HTML || IsScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool HasInButtonScope(string tagName)
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

                if (tn == tagName && ns == NS.HTML)
                    return true;

                if (tn == T.BUTTON && ns == NS.HTML || IsScopingElement(tn, ns))
                    return false;
            }

            return true;
        }

        public bool HasInTableScope(string tagName)
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

                if (ns != NS.HTML)
                    continue;

                if (tn == tagName)
                    return true;

                if (tn == T.TABLE || tn == T.TEMPLATE || tn == T.HTML)
                    return false;
            }

            return true;
        }

        public bool HasTableBodyContextInTableScope()
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

                if (ns != NS.HTML)
                    continue;

                if (tn == T.TBODY || tn == T.THEAD || tn == T.TFOOT)
                    return true;

                if (tn == T.TABLE || tn == T.HTML)
                    return false;
            }

            return true;
        }

        public bool HasInSelectScope(string tagName)
        {
            for (var i = this.StackTop; i >= 0; i--)
            {
                string tn = this.treeAdapter.GetTagName(this.items[i]);
                string ns = this.treeAdapter.GetNamespaceUri(this.items[i]);

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
        public void GenerateImpliedEndTags()
        {
            while (IsImpliedEndTagRequired(this.CurrentTagName))
                this.Pop();
        }

        public void GenerateImpliedEndTagsWithExclusion(string exclusionTagName)
        {
            while (IsImpliedEndTagRequired(this.CurrentTagName) && this.CurrentTagName != exclusionTagName)
                this.Pop();
        }

    }
}