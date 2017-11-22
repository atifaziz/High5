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
        readonly ListNode<HtmlNode> _ancestors;

        internal HtmlTree(TNode node, ListNode<HtmlNode> ancestors)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ancestors == null) throw new ArgumentNullException(nameof(ancestors));
            Node = node;
            _ancestors = node.ChildNodes.Count > 0 ? ancestors.Prepend(node) : ancestors;
        }

        public bool HasParent => Ancestors.Count > 0;

        public HtmlTree<HtmlNode>? Parent =>
            HasParent ? HtmlTree.Create(Ancestors.Item, Ancestors.Next)
                      : (HtmlTree<HtmlNode>?) null;

        public TNode Node { get; }

        HtmlNode IHtmlTree.Node => Node;

        ListNode<HtmlNode> IHtmlTree.Ancestors => _ancestors;

        ListNode<HtmlNode> Ancestors =>
            Node == null
            ? ListNode<HtmlNode>.Empty
            : Node.ChildNodes.Count > 0 ? _ancestors.Next : _ancestors;

        public bool IsEmpty => Node == null;

        public HtmlTree<HtmlNode> AsBaseNode() => HtmlTree.Create((HtmlNode) Node, Ancestors);

        public int ChildNodeCount => Node?.ChildNodes.Count ?? 0;
        public bool HasChildNodes => ChildNodeCount > 0;

        public IEnumerable<HtmlTree<HtmlNode>> ChildNodes
        {
            get
            {
                var node = Node;
                if (node == null)
                    return Enumerable.Empty<HtmlTree<HtmlNode>>();
                var ancestors = _ancestors;
                return from child in node.ChildNodes
                       select HtmlTree.Create(child, ancestors);
            }
        }

        public HtmlTree<HtmlNode>? FirstChild =>
            ChildNodeCount > 0
            ? HtmlTree.Create(Node.FirstChild, _ancestors)
            : (HtmlTree<HtmlNode>?) null;

        public HtmlTree<HtmlNode>? LastChild =>
            ChildNodeCount > 0
            ? HtmlTree.Create(Node.LastChild, _ancestors)
            : (HtmlTree<HtmlNode>?) null;

        public bool Equals(HtmlTree<TNode> other) =>
            Equals(other.Node, other._ancestors);

        bool Equals(HtmlNode otherNode, ListNode<HtmlNode> otherAncestors) =>
            (ReferenceEquals(_ancestors, otherAncestors) || _ancestors == otherAncestors)
            && Node == otherNode;

        public override bool Equals(object obj) =>
            obj is IHtmlTree tree && Equals(tree.Node, tree.Ancestors);

        public override int GetHashCode()
        {
            if (IsEmpty)
                return 0;
            var hash = HashCodeCombiner.Start();
            hash.Add(_ancestors?.GetHashCode());
            hash.Add(Node);
            return hash.CombinedHash;
        }

        public static bool operator ==(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            left.Equals(right);

        public static bool operator !=(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            !left.Equals(right);

        public IEnumerable<HtmlTree<HtmlNode>> Descendants()
        {
            if (HasChildNodes)
            {
                foreach (var child in ChildNodes)
                {
                    yield return child;
                    if (child.HasChildNodes)
                    {
                        foreach (var descendant in child.Descendants())
                            yield return descendant;
                    }
                }
            }
        }

        public IEnumerable<HtmlTree<HtmlNode>> DescendantsAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(Descendants());
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
    }
}
