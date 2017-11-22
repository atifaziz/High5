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

    public struct HtmlTree<TNode> : IEquatable<HtmlTree<TNode>>
        where TNode : HtmlNode
    {
        readonly ListNode<HtmlNode> _ancestors;

        internal HtmlTree(TNode node, ListNode<HtmlNode> ancestors)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            _ancestors = ancestors ?? throw new ArgumentNullException(nameof(ancestors));
        }

        public bool HasParent => _ancestors?.IsEmpty == false;

        public HtmlTree<HtmlNode>? Parent =>
            HasParent
            ? HtmlTree.Create(_ancestors.Item, _ancestors.Next)
            : (HtmlTree<HtmlNode>?) null;

        public TNode Node { get; }

        public HtmlTree<HtmlNode> AsBaseNode() =>
            Node != null
            ? HtmlTree.Create((HtmlNode) Node, _ancestors)
            : default(HtmlTree<HtmlNode>);

        public int ChildNodeCount => Node?.ChildNodes.Count ?? 0;
        public bool HasChildNodes => ChildNodeCount > 0;

        public IEnumerable<HtmlTree<HtmlNode>> ChildNodes
        {
            get
            {
                var node = Node;
                if (node == null)
                    return Enumerable.Empty<HtmlTree<HtmlNode>>();
                var ancestors = _ancestors.Prepend(node);
                return from child in node.ChildNodes
                       select HtmlTree.Create(child, ancestors);
            }
        }

        public HtmlTree<HtmlNode>? PreviousSibling => GetSibling(-1, (i, _) => i >= 0);
        public HtmlTree<HtmlNode>? NextSibling => GetSibling(+1, (i, count) => i < count);

        HtmlTree<HtmlNode>? GetSibling(int offset, Func<int, int, bool> predicate)
        {
            var result = GetIndexInSiblings();
            if (result == null)
                return null;
            var (i, siblings) = result.Value;
            return predicate(i + offset, siblings.Count)
                 ? HtmlTree.Create(siblings[i + offset], _ancestors)
                 : default(HtmlTree<HtmlNode>);
        }

        (int Index, ReadOnlyCollection<HtmlNode> Siblings)? GetIndexInSiblings()
        {
            var parent = Parent;
            if (parent == null)
                return null;
            var siblings = parent.Value.Node.ChildNodes;
            for (var i = 0; i < siblings.Count; i++)
            {
                var sibling = siblings[i];
                if (sibling == Node)
                    return (i, siblings);
            }
            throw new Exception("Implementation error.");
        }

        public bool Equals(HtmlTree<TNode> other) =>
            (ReferenceEquals(_ancestors, other._ancestors) || _ancestors == other._ancestors)
            && Node == other.Node;

        public override bool Equals(object obj) =>
            obj is HtmlTree<TNode> node && Equals(node);

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(_ancestors?.GetHashCode());
            hash.Add(Node);
            return hash.CombinedHash;
        }

        public static bool operator ==(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            left.Equals(right);

        public static bool operator !=(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            !left.Equals(right);

        IEnumerable<HtmlTree<HtmlNode>> Loop(Func<HtmlTree<HtmlNode>, HtmlTree<HtmlNode>?> step)
        {
            if (Node != null)
            {
                for (var node = step(AsBaseNode()); node != null; node = step(node.Value))
                    yield return node.Value;
            }
        }

        public IEnumerable<HtmlTree<HtmlNode>> NodesBeforeSelf1()
        {
            var result = GetIndexInSiblings();
            if (result == null)
                return Enumerable.Empty<HtmlTree<HtmlNode>>();
            var (i, siblings) = result.Value;
            var ancestors = _ancestors;
            return from s in siblings.Take(i - 1)
                   select HtmlTree.Create(s, ancestors);
        }

        public IEnumerable<HtmlTree<HtmlNode>> NodesBeforeSelf() =>
            Loop(n => n.PreviousSibling);

        public IEnumerable<HtmlTree<HtmlNode>> NodesAfterSelf() =>
            Loop(n => n.NextSibling);

        public IEnumerable<HtmlTree<HtmlNode>> Descendants()
        {
            foreach (var child in ChildNodes)
            {
                yield return child;
                foreach (var descendant in child.Descendants())
                    yield return descendant;
            }
        }

        public IEnumerable<HtmlTree<HtmlNode>> DescendantsAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(Descendants());

        public IEnumerable<HtmlTree<HtmlNode>> Ancestors() =>
            Loop(n => n.Parent);

        public IEnumerable<HtmlTree<HtmlNode>> AncestorsAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(Ancestors());

        public IEnumerable<HtmlTree<T>> ChildNodesOfType<T>()
            where T : HtmlNode =>
            from child in ChildNodes
            let node = child.Node as T
            where node != null
            select HtmlTree.Create(node, child._ancestors);

        public IEnumerable<HtmlTree<HtmlElement>> Elements() =>
            ChildNodesOfType<HtmlElement>();
    }

    public static class HtmlTree
    {
        public static HtmlTree<TNode> Create<TNode>(TNode node, ListNode<HtmlNode> ancestors)
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
