#region Copyright (c) 2017 Atif Aziz
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

namespace High5.Tests
{
    using System.Linq;
    using Xunit;
    using static HtmlNodeFactory;

    public class HtmlTreeTests
    {
        static HtmlTree<HtmlNode> DefaultHtmlTree => default(HtmlTree<HtmlNode>);

        [Fact]
        public void DefaultNodeIsNull() =>
            Assert.Null(default(HtmlTree<HtmlNode>).Node);

        [Fact]
        public void DefaultIsEmpty() =>
            Assert.True(DefaultHtmlTree.IsEmpty);

        [Fact]
        public void DefaultHasChildNodesIsFalse() =>
            Assert.False(DefaultHtmlTree.HasChildNodes);

        [Fact]
        public void DefaultChildNodeCountIsZero() =>
            Assert.Equal(0, DefaultHtmlTree.ChildNodeCount);

        [Fact]
        public void DefaultChildNodesIsEmpty() =>
            Assert.Empty(DefaultHtmlTree.ChildNodes);

        [Fact]
        public void DefaultFirstChildIsNull() =>
            Assert.Null(DefaultHtmlTree.FirstChild);

        [Fact]
        public void DefaultLastChildIsNull() =>
            Assert.Null(DefaultHtmlTree.LastChild);

        [Fact]
        public void DefaultHasParentIsFalse() =>
            Assert.False(DefaultHtmlTree.HasParent);

        [Fact]
        public void DefaultParentIsNull() =>
            Assert.Null(DefaultHtmlTree.Parent);

        [Fact]
        public void DefaultHashCodeIsZero() =>
            Assert.Equal(0, DefaultHtmlTree.GetHashCode());

        [Fact]
        public void DefaultEqualsSelf()
        {
            Assert.True(DefaultHtmlTree.Equals(DefaultHtmlTree));
            Assert.True(DefaultHtmlTree.Equals((object) DefaultHtmlTree));
            Assert.True(DefaultHtmlTree == default(HtmlTree<HtmlNode>));
            Assert.False(DefaultHtmlTree != default(HtmlTree<HtmlNode>));
        }

        [Fact]
        public void DefaultNotEqualsNull() =>
            Assert.False(DefaultHtmlTree.Equals(null));

        [Fact]
        public void DefaultNotEqualsOther() =>
            Assert.False(DefaultHtmlTree.Equals(new object()));

        [Fact]
        public void DefaultDescendantsIsEmpty() =>
            Assert.Empty(DefaultHtmlTree.DescendantNodes());

        [Fact]
        public void AllRelationsAreReflectedCorrectlyThroughTree()
        {
            HtmlElement html, head, body, p;
            var doc =
                Document(
                    html = Element("html",
                        head = Element("head",
                            Element("title", Text("Example"))),
                        body = Element("body",
                            Element("h1", Attributes(("class", "main")),
                                Text("Heading")),
                            Comment("content start"),
                            p = Element("p",
                                Text("Lorem ipsum dolor sit amet, consectetur adipiscing elit.")),
                            Element("p",
                                Text("The quick brown fox jumps over the lazy dog.")),
                            Comment("content end"))));

            var tree = HtmlTree.Create(doc);
            Assert.Same(doc, tree.Node);
            Assert.False(tree.HasParent);
            Assert.True(tree.HasChildNodes);
            Assert.Equal(1, tree.ChildNodeCount);
            Assert.True(tree.FirstChild.Equals(tree.LastChild));

            var treeHtml = tree.ChildNodes.First();
            Assert.Same(html, treeHtml.Node);
            Assert.True(tree.Equals(treeHtml.Parent));
            Assert.Equal(2, treeHtml.ChildNodeCount);
            Assert.False(treeHtml.FirstChild.Equals(treeHtml.LastChild));

            var treeHead = treeHtml.ChildNodes.First();
            Assert.True(treeHead.Equals(treeHtml.FirstChild));
            Assert.Same(head, treeHead.Node);
            Assert.True(treeHead.Parent == treeHtml);
            Assert.Equal(1, treeHead.ChildNodeCount);

            var treeBody = treeHtml.ChildNodes.Last();
            Assert.True(treeBody.Equals(treeHtml.LastChild));
            Assert.Same(body, treeBody.Node);
            Assert.True(treeBody.Parent == treeHtml);
            Assert.Equal(5, treeBody.ChildNodeCount);

            Assert.True(treeHead.NextSibling == treeBody);
            Assert.Null(treeHead.PreviousSibling);

            Assert.True(treeBody.PreviousSibling == treeHead);
            Assert.Null(treeBody.NextSibling);

            Assert.Equal(doc.DescendantNodes(),
                         from d in tree.DescendantNodes()
                         select d.Node);

            Assert.Equal(doc.DescendantNodesAndSelf(),
                         from d in tree.DescendantNodesAndSelf()
                         select d.Node);

            Assert.Equal(html.Elements(),
                         from e in treeHtml.Elements()
                         select e.Node);

            Assert.Equal(html.Descendants(),
                         from e in treeHtml.Descendants()
                         select e.Node);

            Assert.Equal(treeBody.ChildNodes.Skip(1),
                         treeBody.FirstChild.Value.NodesAfterSelf());

            Assert.Equal(treeBody.ChildNodes.Skip(1).Elements(),
                         treeBody.FirstChild.Value.ElementsAfterSelf());

            Assert.Equal(treeBody.ChildNodes.Skip(2),
                         treeBody.FirstChild.Value.NextSibling.Value.NodesAfterSelf());

            Assert.Equal(treeBody.ChildNodes.Skip(2).Elements(),
                         treeBody.FirstChild.Value.NextSibling.Value.ElementsAfterSelf());

            Assert.Empty(treeBody.LastChild.Value.NodesAfterSelf());
            Assert.Empty(treeBody.Elements().Last().ElementsAfterSelf());

            Assert.Equal(treeBody.ChildNodes.Take(treeBody.ChildNodeCount - 1),
                         treeBody.LastChild.Value.NodesBeforeSelf());

            Assert.Equal(treeBody.ChildNodes.Take(treeBody.ChildNodeCount - 1).Elements(),
                         treeBody.LastChild.Value.ElementsBeforeSelf());

            Assert.Equal(treeBody.ChildNodes.Take(treeBody.ChildNodeCount - 2),
                         treeBody.LastChild.Value.PreviousSibling.Value.NodesBeforeSelf());

            Assert.Equal(treeBody.ChildNodes.Take(treeBody.ChildNodeCount - 2).Elements(),
                         treeBody.LastChild.Value.PreviousSibling.Value.ElementsBeforeSelf());

            Assert.Empty(treeBody.FirstChild.Value.NodesBeforeSelf());
            Assert.Empty(treeBody.Elements().First().ElementsBeforeSelf());

            Assert.Equal(new HtmlNode[] { body, html, doc },
                         tree.DescendantNodes()
                             .Elements()
                             .Single(e => e.Node == p)
                             .AncestorNodes()
                             .Select(a => a.Node));

            var treePara = HtmlTree.Create(p);
            Assert.True(treePara.Equals(treePara.ChildNodes.Single().Parent));
        }
    }
}
