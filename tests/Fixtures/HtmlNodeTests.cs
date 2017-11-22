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

    public class HtmlNodeTests
    {
        [Fact]
        public void TextReturnsTextNodeInitialzed()
        {
            const string value = "foobar";
            var text = Text(value);
            Assert.NotNull(text);
            Assert.Equal(value, text.Value);
            Assert.Equal(0, text.ChildNodes.Count);
        }

        [Fact]
        public void CommentReturnsCommentNodeInitialzed()
        {
            const string data = "some comment";
            var comment = Comment(data);
            Assert.NotNull(comment);
            Assert.Equal(data, comment.Data);
            Assert.Equal(0, comment.ChildNodes.Count);
        }

        static void AssertAttributeEqual(string expectedName,
                                         string expectedValue,
                                         HtmlAttribute actualAttribute)
        {
            Assert.NotNull(actualAttribute);
            Assert.Equal(expectedName, actualAttribute.Name);
            Assert.Equal(expectedValue, actualAttribute.Value);
            Assert.Equal(null, actualAttribute.NamespaceUri);
            Assert.Equal(null, actualAttribute.Prefix);
        }

        [Fact]
        public void AttributeReturnsAttributeInitialized()
        {
            const string name = "name";
            const string value = "value";

            AssertAttributeEqual(name, value, Attribute(name, value));
        }

        [Fact]
        public void AttributesReturnsSequenceOfInitializedAttributes()
        {
            const string name1  = "name1";
            const string value1 = "value2";
            const string name2  = "name1";
            const string value2 = "value2";

            using (var e = Attributes((name1, value1), (name2, value2)).GetEnumerator())
            {
                Assert.True(e.MoveNext());
                AssertAttributeEqual(name1, value1, e.Current);

                Assert.True(e.MoveNext());
                AssertAttributeEqual(name2, value2, e.Current);

                Assert.False(e.MoveNext());
            }
        }

        [Fact]
        public void AttributeDeconstruction()
        {
            const string name = "name";
            const string value = "value";

            var attribute = Attribute(name, value);
            var (n, v) = attribute;
            Assert.Equal(n, name);
            Assert.Equal(v, value);
        }

        [Fact]
        public void AttributeDeconstructionWithNamespace()
        {
            const string name = "name";
            const string value = "value";

            var attribute = Attribute(name, value);
            var (ns, n, v) = attribute;
            Assert.Equal(ns, null);
            Assert.Equal(n, name);
            Assert.Equal(v, value);
        }

        [Fact]
        public void AttributeDeconstructionWithNamespaceAndPrefix()
        {
            const string name = "name";
            const string value = "value";

            var attribute = Attribute(name, value);
            var (ns, prefix, n, v) = attribute;
            Assert.Equal(ns, null);
            Assert.Equal(prefix, null);
            Assert.Equal(n, name);
            Assert.Equal(v, value);
        }

        const string HtmlNs = "http://www.w3.org/1999/xhtml";

        [Fact]
        public void ElementReturnsElementNodeInitialzed()
        {
            const string tagName = "element";
            var element = Element(tagName);

            Assert.NotNull(element);
            Assert.Equal(tagName, element.TagName);
            Assert.Equal(HtmlNs, element.NamespaceUri);
            Assert.Equal(0, element.Attributes.Count);
            Assert.Equal(0, element.ChildNodes.Count);
        }

        [Fact]
        public void ElementWithAttributesReturnsElementNodeInitialzedSuch()
        {
            const string tagName = "element";
            const string name1 = "name1"; const string value1 = "value1";
            const string name2 = "name2"; const string value2 = "value2";

            var element =
                Element(tagName,
                    Attribute(name1, value1),
                    Attribute(name2, value2));

            Assert.NotNull(element);
            Assert.Equal(tagName, element.TagName);
            Assert.Equal(HtmlNs, element.NamespaceUri);

            Assert.Equal(2, element.Attributes.Count);
            AssertAttributeEqual(name1, value1, element.Attributes[0]);
            AssertAttributeEqual(name2, value2, element.Attributes[1]);

            Assert.Equal(0, element.ChildNodes.Count);
        }

        [Fact]
        public void ElementWithAttributesAndChildrenReturnsElementNodeInitialzedSuch()
        {
            const string tagName = "element";
            const string name1  = "name1";
            const string value1 = "value2";
            const string name2  = "name1";
            const string value2 = "value2";

            var child1 = Text("text");
            var child2 = Comment("some comment");
            var child3 = Element("child");

            var element =
                Element(tagName,
                    Attributes((name1, value1), (name2, value2)),
                    child1,
                    child2,
                    child3);

            Assert.NotNull(element);
            Assert.Equal(tagName, element.TagName);
            Assert.Equal(HtmlNs, element.NamespaceUri);

            Assert.Equal(2, element.Attributes.Count);
            AssertAttributeEqual(name1, value1, element.Attributes[0]);
            AssertAttributeEqual(name2, value2, element.Attributes[1]);

            Assert.Equal(3, element.ChildNodes.Count);
            Assert.Same(child1, element.ChildNodes[0]);
            Assert.Same(child2, element.ChildNodes[1]);
            Assert.Same(child3, element.ChildNodes[2]);
        }

        [Fact]
        public void DocumentFragmentReturnsDocumentFragmentInitialized()
        {
            var fragment = DocumentFragment();
            Assert.NotNull(fragment);
            Assert.Equal(0, fragment.ChildNodes.Count);
        }

        [Fact]
        public void DocumentFragmentWithChildrenReturnsDocumentFragmentInitializedSuch()
        {
            var child1 = Text("text");
            var child2 = Comment("some comment");
            var child3 = Element("child");

            var fragment = DocumentFragment(child1, child2, child3);

            Assert.NotNull(fragment);

            Assert.Equal(3, fragment.ChildNodes.Count);
            Assert.Same(child1, fragment.ChildNodes[0]);
            Assert.Same(child2, fragment.ChildNodes[1]);
            Assert.Same(child3, fragment.ChildNodes[2]);
        }

        [Fact]
        public void DocumentFragmentWithSequenceOfChildrenReturnsDocumentFragmentInitializedSuch()
        {
            var child1 = Text("text");
            var child2 = Comment("some comment");
            var child3 = Element("child");

            var children = new HtmlNode[] { child1, child2, child3 };
            var fragment = DocumentFragment(children.AsEnumerable());

            Assert.NotNull(fragment);

            Assert.Equal(3, fragment.ChildNodes.Count);
            Assert.Same(child1, fragment.ChildNodes[0]);
            Assert.Same(child2, fragment.ChildNodes[1]);
            Assert.Same(child3, fragment.ChildNodes[2]);
        }

        [Fact]
        public void TemplateReturnsTemplateNodeInitialized()
        {
            var content =
                DocumentFragment(
                    Text("text"),
                    Comment("some comment"),
                    Element("child"));

            var template = Template(content);

            Assert.NotNull(template);
            Assert.Same(content, template.Content);
        }

        [Fact]
        public void TemplateWithAttributesReturnsTemplateNodeInitializedSuch()
        {
            const string name1  = "name1";
            const string value1 = "value2";
            const string name2  = "name1";
            const string value2 = "value2";

            var content =
                DocumentFragment(
                    Text("text"),
                    Comment("some comment"),
                    Element("child"));

            var template = Template(Attributes((name1, value1), (name2, value2)), content);

            Assert.NotNull(template);

            Assert.Equal(2, template.Attributes.Count);
            AssertAttributeEqual(name1, value1, template.Attributes[0]);
            AssertAttributeEqual(name2, value2, template.Attributes[1]);

            Assert.Same(content, template.Content);
        }

        [Fact]
        public void DocumentReturnsDocumentNodeInitialized()
        {
            var document = Document();

            Assert.NotNull(document);
            Assert.Equal(0, document.ChildNodes.Count);
        }

        [Fact]
        public void DocumentWithChildrenReturnsDocumentNodeInitializedSuch()
        {
            var head =
                Element("head",
                    Element("title", Text("The Document")));
            var body =
                Element("body",
                    Element("p", Text("This is a paragraph.")));

            var document = Document(head, body);

            Assert.NotNull(document);
            Assert.Equal(2, document.ChildNodes.Count);
            Assert.Same(head, document.ChildNodes[0]);
            Assert.Same(body, document.ChildNodes[1]);
        }
    }
}
