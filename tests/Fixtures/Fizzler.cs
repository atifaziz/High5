namespace High5.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Fizzler;
    using Selector = Fizzler.Selector<HtmlTree<HtmlElement>>;

    /// <summary>
    /// An <see cref="IElementOps{TElement}"/> implementation for <see cref="HtmlNode"/>
    /// from <a href="http://www.codeplex.com/htmlagilitypack">HtmlAgilityPack</a>.
    /// </summary>
    public class HtmlNodeOps : IElementOps<HtmlTree<HtmlElement>>
    {
        static readonly Selector EmptySetSelector = _ => Enumerable.Empty<HtmlTree<HtmlElement>>();

        static class Separator
        {
            public static readonly char[] Space = { '\x20' };
            public static readonly char[] Dash = { '-' };
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#type-selectors">type selector</a>,
        /// which represents an instance of the element type in the document tree.
        /// </summary>
        public virtual Selector Type(NamespacePrefix prefix, string type)
        {
            return prefix.IsSpecific
                 ? EmptySetSelector
                 : elements => elements.Where(e => e.Node.TagName == type);
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#universal-selector">universal selector</a>,
        /// any single element in the document tree in any namespace
        /// (including those without a namespace) if no default namespace
        /// has been specified for selectors.
        /// </summary>
        public virtual Selector Universal(NamespacePrefix prefix) =>
            prefix.IsSpecific
            ? EmptySetSelector
            : elements => elements;

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#Id-selectors">ID selector</a>,
        /// which represents an element instance that has an identifier that
        /// matches the identifier in the ID selector.
        /// </summary>
        public virtual Selector Id(string id)
        {
            return elements => elements.Where(e => e.TryMapAttribute("id", a => a.Value == id));
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#class-html">class selector</a>,
        /// which is an alternative <see cref="IElementOps{TElement}.AttributeIncludes"/> when
        /// representing the <c>class</c> attribute.
        /// </summary>
        public virtual Selector Class(string clazz)
        {
            return elements => elements.Where(e => e.TryMapAttribute("class", a => a.Value.Split(Separator.Space).Contains(clazz)));
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the given attribute <paramref name="name"/>
        /// whatever the values of the attribute.
        /// </summary>
        public virtual Selector AttributeExists(NamespacePrefix prefix, string name)
        {
            return prefix.IsSpecific
                 ? EmptySetSelector
                 : elements => elements.Where(e => e.TryMapAttribute(name, _ => true));
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the given attribute <paramref name="name"/>
        /// and whose value is exactly <paramref name="value"/>.
        /// </summary>
        public virtual Selector AttributeExact(NamespacePrefix prefix, string name, string value)
        {
            return prefix.IsSpecific
                 ? EmptySetSelector
                 : elements => from e in elements
                               where e.TryMapAttribute(name, a => a.Value == value)
                               select e;
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the given attribute <paramref name="name"/>
        /// and whose value is a whitespace-separated list of words, one of
        /// which is exactly <paramref name="value"/>.
        /// </summary>
        public virtual Selector AttributeIncludes(NamespacePrefix prefix, string name, string value)
        {
            return prefix.IsSpecific
                 ? EmptySetSelector
                 : elements => from e in elements
                               where e.TryMapAttribute(name, a => a.Value.Split(Separator.Space).Contains(value))
                               select e;
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the given attribute <paramref name="name"/>,
        /// its value either being exactly <paramref name="value"/> or beginning
        /// with <paramref name="value"/> immediately followed by "-" (U+002D).
        /// </summary>
        public virtual Selector AttributeDashMatch(NamespacePrefix prefix, string name, string value)
        {
            return prefix.IsSpecific || string.IsNullOrEmpty(value)
                 ? EmptySetSelector
                 : elements => from e in elements
                               where e.TryMapAttribute(name, a => a.Value.Split(Separator.Dash).Contains(value))
                               select e;
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the attribute <paramref name="name"/>
        /// whose value begins with the prefix <paramref name="value"/>.
        /// </summary>
        public Selector AttributePrefixMatch(NamespacePrefix prefix, string name, string value)
        {
            return prefix.IsSpecific || string.IsNullOrEmpty(value)
                 ? EmptySetSelector
                 : elements => from e in elements
                               where e.TryMapAttribute(name, a => a.Value.StartsWith(value, StringComparison.Ordinal))
                               select e;
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the attribute <paramref name="name"/>
        /// whose value ends with the suffix <paramref name="value"/>.
        /// </summary>
        public Selector AttributeSuffixMatch(NamespacePrefix prefix, string name, string value)
        {
            return prefix.IsSpecific || string.IsNullOrEmpty(value)
                 ? EmptySetSelector
                 : elements => from e in elements
                               where e.TryMapAttribute(name, a => a.Value.EndsWith(value, StringComparison.Ordinal))
                               select e;
        }

        /// <summary>
        /// Generates an <a href="http://www.w3.org/TR/css3-selectors/#attribute-selectors">attribute selector</a>
        /// that represents an element with the attribute <paramref name="name"/>
        /// whose value contains at least one instance of the substring <paramref name="value"/>.
        /// </summary>
        public Selector AttributeSubstring(NamespacePrefix prefix, string name, string value)
        {
            return prefix.IsSpecific || string.IsNullOrEmpty(value)
                 ? EmptySetSelector
                 : elements => from e in elements
                               where e.TryMapAttribute(name, a => a.Value.Contains(value))
                               select e;
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#pseudo-classes">pseudo-class selector</a>,
        /// which represents an element that is the first child of some other element.
        /// </summary>
        public virtual Selector FirstChild()
        {
            return elements => elements.Where(e => !e.ElementsBeforeSelf().Any());
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#pseudo-classes">pseudo-class selector</a>,
        /// which represents an element that is the last child of some other element.
        /// </summary>
        public virtual Selector LastChild()
        {
            return elements => elements.Where(e => !e.ElementsAfterSelf().Any());
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#pseudo-classes">pseudo-class selector</a>,
        /// which represents an element that is the N-th child of some other element.
        /// </summary>
        public virtual Selector NthChild(int a, int b)
        {
            if (a != 1)
                throw new NotSupportedException("The nth-child(an+b) selector where a is not 1 is not supported.");

            return elements => from e in elements
                               let siblingElements = e.Parent?.Elements().Take(b).ToArray()
                               where siblingElements != null
                                  && siblingElements.Length == b
                                  && siblingElements.Last().Equals(e)
                               select e;
        }

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#pseudo-classes">pseudo-class selector</a>,
        /// which represents an element that has a parent element and whose parent
        /// element has no other element children.
        /// </summary>
        public virtual Selector OnlyChild() =>
            elements => elements.Where(e => !e.ElementsAfterSelf().Concat(e.ElementsBeforeSelf()).Any());

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#pseudo-classes">pseudo-class selector</a>,
        /// which represents an element that has no children at all.
        /// </summary>
        public virtual Selector Empty() =>
            elements => elements.Where(e => e.ChildNodeCount == 0
                                         || e.Node.ChildNodes.All(cn => !(cn is HtmlComment)));

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#combinators">combinator</a>,
        /// which represents a childhood relationship between two elements.
        /// </summary>
        public virtual Selector Child() =>
            elements => elements.SelectMany(e => e.Elements());

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#combinators">combinator</a>,
        /// which represents a relationship between two elements where one element is an
        /// arbitrary descendant of some ancestor element.
        /// </summary>
        public virtual Selector Descendant() =>
            elements => elements.SelectMany(e => e.Descendants());

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#combinators">combinator</a>,
        /// which represents elements that share the same parent in the document tree and
        /// where the first element immediately precedes the second element.
        /// </summary>
        public virtual Selector Adjacent() =>
            elements => elements.SelectMany(e => e.ElementsAfterSelf().Take(1));

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#combinators">combinator</a>,
        /// which separates two sequences of simple selectors. The elements represented
        /// by the two sequences share the same parent in the document tree and the
        /// element represented by the first sequence precedes (not necessarily
        /// immediately) the element represented by the second one.
        /// </summary>
        public virtual Selector GeneralSibling() =>
            elements => elements.SelectMany(e => e.ElementsAfterSelf());

        /// <summary>
        /// Generates a <a href="http://www.w3.org/TR/css3-selectors/#pseudo-classes">pseudo-class selector</a>,
        /// which represents an element that is the N-th child from bottom up of some other element.
        /// </summary>
        public Selector NthLastChild(int a, int b)
        {
            if (a != 1)
                throw new NotSupportedException("The nth-last-child(an+b) selector where a is not 1 is not supported.");

            return elements => from e in elements
                               let siblings = e.Parent?.Elements().Skip(Math.Max(0, (e.Parent?.Elements().Count() ?? 0) - b)).Take(b).ToArray()
                               where siblings != null
                                  && siblings.Length == b
                                  && siblings.First().Equals(e)
                               select e;
        }
    }

    public static class HtmlSelection
    {
        static readonly HtmlNodeOps Ops = new HtmlNodeOps();

        public static HtmlElement QuerySelector(this HtmlDocument document, string selector) =>
            document.QuerySelectorAll(selector).FirstOrDefault();

        public static IEnumerable<HtmlElement> QuerySelectorAll(this HtmlDocument document, string selector) =>
            ((HtmlNode) document).QuerySelectorAll(selector);

        public static HtmlElement QuerySelector(this HtmlDocumentFragment fragment, string selector) =>
            fragment.QuerySelectorAll(selector).FirstOrDefault();

        public static IEnumerable<HtmlElement> QuerySelectorAll(this HtmlDocumentFragment fragment, string selector) =>
            ((HtmlNode) fragment).QuerySelectorAll(selector);

        static IEnumerable<HtmlElement> QuerySelectorAll(this HtmlNode document, string selector) =>
            HtmlNodeFactory.Element("<fake>", document.ChildNodes).QuerySelectorAll(selector);

        /// <summary>
        /// Similar to <see cref="QuerySelectorAll(HtmlElement,string)" />
        /// except it returns only the first element matching the supplied
        /// selector strings.
        /// </summary>
        public static HtmlElement QuerySelector(this HtmlElement element, string selector) =>
            element.QuerySelectorAll(selector).FirstOrDefault();

        /// <summary>
        /// Retrieves all element nodes from descendants of the starting
        /// element node that match any selector within the supplied
        /// selector strings.
        /// </summary>
        public static IEnumerable<HtmlElement> QuerySelectorAll(this HtmlElement element, string selector) =>
            QuerySelectorAll(element, selector, null);

        /// <summary>
        /// Retrieves all element nodes from descendants of the starting
        /// element node that match any selector within the supplied
        /// selector strings. An additional parameter specifies a
        /// particular compiler to use for parsing and compiling the
        /// selector.
        /// </summary>
        /// <remarks>
        /// The <paramref name="compiler"/> can be <c>null</c>, in which
        /// case a default compiler is used. If the selector is to be used
        /// often, it is recommended to use a caching compiler such as the
        /// one supplied by <see cref="CreateCachingCompiler()"/>.
        /// </remarks>
        public static IEnumerable<HtmlElement> QuerySelectorAll(this HtmlElement element, string selector, Func<string, Func<HtmlElement, IEnumerable<HtmlElement>>> compiler) =>
            (compiler ?? Compile)(selector)(element);

        /// <summary>
        /// Parses and compiles CSS selector text into run-time function.
        /// </summary>
        /// <remarks>
        /// Use this method to compile and reuse frequently used CSS selectors
        /// without parsing them each time.
        /// </remarks>
        public static Func<HtmlElement, IEnumerable<HtmlElement>> Compile(string selector)
        {
            var compiled = Parser.Parse(selector, new SelectorGenerator<HtmlTree<HtmlElement>>(Ops)).Selector;
            return root => from e in compiled(Enumerable.Repeat(HtmlTree.Create(root), 1))
                           select e.Node;
        }

        //
        // Caching
        //

        [ThreadStatic]
        static Func<string, Func<HtmlElement, IEnumerable<HtmlElement>>> _defaultCachingCompiler;

        static Func<string, Func<HtmlElement, IEnumerable<HtmlElement>>> DefaultCachingCompiler =>
            _defaultCachingCompiler ?? (_defaultCachingCompiler = CreateCachingCompiler());

        /// <summary>
        /// Compiles a selector. If the selector has been previously
        /// compiled then this method returns it rather than parsing
        /// and compiling the selector on each invocation.
        /// </summary>
        /// <remarks>
        /// The cache is per-thread and therefore thread-safe without
        /// lock contention.
        /// </remarks>
        public static Func<HtmlElement, IEnumerable<HtmlElement>> CachableCompile(string selector) =>
            DefaultCachingCompiler(selector);

        /// <summary>
        /// Creates a caching selector compiler that uses a default
        /// cache strategy when the selector text is regarded as being
        /// orginally case-insensitive.
        /// </summary>
        public static Func<string, Func<HtmlElement, IEnumerable<HtmlElement>>> CreateCachingCompiler() =>
            CreateCachingCompiler(null);

        /// <summary>
        /// Creates a caching selector compiler where the compiled selectors
        /// are maintained in a user-supplied <see cref="IDictionary{TKey,TValue}"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// If <paramref name="cache"/> is <c>null</c> then this method uses a
        /// the <see cref="Dictionary{TKey,TValue}"/> implementation with an
        /// ordinally case-insensitive selectors text comparer.
        /// </remarks>
        public static Func<string, Func<HtmlElement, IEnumerable<HtmlElement>>> CreateCachingCompiler(IDictionary<string, Func<HtmlElement, IEnumerable<HtmlElement>>> cache) =>
            SelectorsCachingCompiler.Create(Compile, cache);
    }

    static class Extensions
    {
        public static T TryMapAttribute<T>(this HtmlTree<HtmlElement> element, string attributeName, Func<HtmlAttribute, T> resultSelector) =>
            TryMapAttribute(element, attributeName, default(T), resultSelector);

        public static T TryMapAttribute<T>(this HtmlTree<HtmlElement> element, string attributeName, T none, Func<HtmlAttribute, T> resultSelector) =>
            element.Node.Attributes.FirstOrDefault(a => string.Equals(a.Name, attributeName, StringComparison.OrdinalIgnoreCase))
            is HtmlAttribute attr && !string.IsNullOrEmpty(attr.Name)
            ? resultSelector(attr)
            : none;
    }
}
