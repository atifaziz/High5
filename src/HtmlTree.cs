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

namespace High5
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Internal;
    using Collections;

    interface IHtmlTree
    {
        HtmlNode Node { get; }
        ListNode<HtmlNode> Ancestors { get; }
    }

    public struct HtmlTree<TNode> : IEquatable<HtmlTree<TNode>>, IHtmlTree
        where TNode : HtmlNode
    {
        readonly ListNode<HtmlNode> _ancestorStack;

        internal HtmlTree(TNode node, ListNode<HtmlNode> ancestors)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ancestors == null) throw new ArgumentNullException(nameof(ancestors));

            Node = node;

            // If the node has children then push/cache the node itself on the
            // ancestors stack. This helps to avoid allocating a new list node
            // in the ancestor chain when handing out new instances of this
            // class for the children; the ancestor chain gets shared.

            _ancestorStack = node.ChildNodes.Count > 0 && ancestors.Item != node
                       ? ancestors.Prepend(node)
                       : ancestors;
        }

        public bool HasParent => AncestorStack.Count > 0;

        public HtmlTree<HtmlNode>? Parent =>
            HasParent ? HtmlTree.Create(AncestorStack.Item, AncestorStack.Next)
                      : (HtmlTree<HtmlNode>?) null;

        public TNode Node { get; }

        HtmlNode IHtmlTree.Node => Node;

        ListNode<HtmlNode> IHtmlTree.Ancestors => _ancestorStack;

        ListNode<HtmlNode> AncestorStack =>
            Node == null
            ? ListNode<HtmlNode>.Empty
            : //
              // If this node had children then it is pushed on the ancestor
              // chain during construction so pop it off here to return the
              // true chain.
              //
              Node.ChildNodes.Count > 0
              ? _ancestorStack.Next
              : _ancestorStack;

        public bool IsEmpty => Node == null;

        internal HtmlTree<T> As<T>() where T : HtmlNode =>
            HtmlTree.Create((T) (HtmlNode) Node, _ancestorStack);

        public HtmlTree<HtmlNode> AsBaseNode() => As<HtmlNode>();

        public int ChildNodeCount => Node?.ChildNodes.Count ?? 0;
        public bool HasChildNodes => ChildNodeCount > 0;

        public IEnumerable<HtmlTree<HtmlNode>> ChildNodes
        {
            get
            {
                var node = Node;
                if (node == null)
                    return Enumerable.Empty<HtmlTree<HtmlNode>>();
                var ancestors = _ancestorStack;
                return from child in node.ChildNodes
                       select HtmlTree.Create(child, ancestors);
            }
        }

        public HtmlTree<HtmlNode>? FirstChild =>
            ChildNodeCount > 0
            ? HtmlTree.Create(Node.FirstChild, _ancestorStack)
            : (HtmlTree<HtmlNode>?) null;

        public HtmlTree<HtmlNode>? LastChild =>
            ChildNodeCount > 0
            ? HtmlTree.Create(Node.LastChild, _ancestorStack)
            : (HtmlTree<HtmlNode>?) null;

        public HtmlTree<HtmlNode>? PreviousSibling => GetSibling(-1, (i, _) => i >= 0);
        public HtmlTree<HtmlNode>? NextSibling => GetSibling(+1, (i, count) => i < count);

        HtmlTree<HtmlNode>? GetSibling(int offset, Func<int, int, bool> predicate)
        {
            if (!HasParent)
                return null;
            var siblings = Parent.Value.Node.ChildNodes;
            var i = siblings.IndexOf(Node);
            return predicate(i + offset, siblings.Count)
                 ? HtmlTree.Create(siblings[i + offset], AncestorStack)
                 : (HtmlTree<HtmlNode>?) null;
        }

        public bool Equals(HtmlTree<TNode> other) =>
            Equals(other.Node, other._ancestorStack);

        bool Equals(HtmlNode otherNode, ListNode<HtmlNode> otherAncestors) =>
            (ReferenceEquals(_ancestorStack, otherAncestors) || _ancestorStack == otherAncestors)
            && Node == otherNode;

        public override bool Equals(object obj) =>
            obj is IHtmlTree tree && Equals(tree.Node, tree.Ancestors);

        public override int GetHashCode()
        {
            if (IsEmpty)
                return 0;
            var hash = HashCodeCombiner.Start();
            hash.Add(_ancestorStack?.GetHashCode() ?? 0);
            hash.Add(Node);
            return hash.CombinedHash;
        }

        public static bool operator ==(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            left.Equals(right);

        public static bool operator !=(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            !left.Equals(right);

        public IEnumerable<HtmlTree<HtmlElement>> Descendants() =>
            DescendantNodes().Elements();

        public IEnumerable<HtmlTree<HtmlNode>> DescendantNodes()
        {
            if (HasChildNodes)
            {
                foreach (var child in ChildNodes)
                {
                    yield return child;
                    if (child.HasChildNodes)
                    {
                        foreach (var descendant in child.DescendantNodes())
                            yield return descendant;
                    }
                }
            }
        }

        internal HtmlTree<HtmlElement> AsElementOrDefault() =>
            Node is HtmlElement
            ? As<HtmlElement>()
            : default(HtmlTree<HtmlElement>);

        public IEnumerable<HtmlTree<HtmlNode>> DescendantNodesAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(DescendantNodes());

        public IEnumerable<HtmlTree<HtmlElement>> DescendantsAndSelf() =>
            DescendantNodesAndSelf().Elements();

        public IEnumerable<HtmlTree<HtmlElement>> Elements() =>
            from d in ChildNodes
            select d.AsElementOrDefault()
            into e
            where !e.IsEmpty
            select e;

        public IEnumerable<HtmlTree<HtmlNode>> NodesAfterSelf()
        {
            for (var node = NextSibling; node != null; node = node.Value.NextSibling)
                yield return node.Value;
        }

        public IEnumerable<HtmlTree<HtmlNode>> NodesBeforeSelf()
        {
            var thisNode = AsBaseNode();
            for (var node = Parent?.FirstChild;
                 node != null && node != thisNode;
                 node = node.Value.NextSibling)
            {
                yield return node.Value;
            }
        }

        public IEnumerable<HtmlTree<HtmlElement>> ElementsAfterSelf() =>
            NodesAfterSelf().Elements();

        /// <summary>
        /// Returns a sequence of the ancestor elements of this node, going
        /// from the nearest to the furthest ancestor.
        /// </summary>

        public IEnumerable<HtmlTree<HtmlElement>> Ancestors() =>
            AncestorNodes().Elements();

        /// <summary>
        /// Returns a sequence of the ancestors of this node, going from the
        /// nearest to the furthest ancestor.
        /// </summary>

        public IEnumerable<HtmlTree<HtmlNode>> AncestorNodes()
        {
            for (var parent = Parent; parent != null; parent = parent.Value.Parent)
                yield return parent.Value;
        }

        /// <summary>
        /// Returns a sequence of the ancestors of this node, going from this
        /// node then the nearest to the furthest ancestor.
        /// </summary>

        public IEnumerable<HtmlTree<HtmlNode>> AncestorNodesAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(AncestorNodes());
    }

    public static class HtmlTree
    {
        internal static HtmlTree<TNode> Create<TNode>(TNode node, ListNode<HtmlNode> ancestors)
            where TNode : HtmlNode =>
            new HtmlTree<TNode>(node, ancestors);

        public static HtmlTree<HtmlDocument> Create(HtmlDocument document) =>
            TreeFromNode(document);

        public static HtmlTree<HtmlDocumentFragment> Create(HtmlDocumentFragment documentFragment) =>
            TreeFromNode(documentFragment);

        public static HtmlTree<HtmlElement> Create(HtmlElement element) =>
            TreeFromNode(element);

        static HtmlTree<T> TreeFromNode<T>(T root) where T : HtmlNode =>
            Create(root, ListNode<HtmlNode>.Empty);

        public static HtmlTree<HtmlElement> AsElement(this HtmlTree<HtmlNode> node) =>
            node.As<HtmlElement>();

        public static IEnumerable<HtmlTree<HtmlElement>> AncestorsAndSelf(this HtmlTree<HtmlElement> element) =>
            Enumerable.Repeat(element, 1).Concat(element.Ancestors());

        public static IEnumerable<HtmlTree<HtmlElement>> AncestorsAndSelf(this HtmlTree<HtmlTemplateElement> element) =>
            element.AsBaseNode().AsElement().AncestorsAndSelf();

        public static IEnumerable<HtmlTree<HtmlElement>> Elements(this IEnumerable<HtmlTree<HtmlNode>> nodes) =>
            from n in nodes ?? throw new ArgumentNullException(nameof(nodes))
            select n.AsElementOrDefault()
            into e
            where !e.IsEmpty
            select e;
    }
}
