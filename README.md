# High5

[![Build Status][win-build-badge]][win-builds]
[![Build Status][nix-build-badge]][nix-builds]
[![NuGet][nuget-badge]][nuget-pkg]
[![MyGet][myget-badge]][edge-pkgs]

High5 is a [spec-compliant HTML][html] parser [.NET Standard][netstd] library.
It parses HTML the way the latest version of your browser does.

High5 was born by porting [parse5][parse5], which is in JavaScript, to C#.

High5's parser is generic. It can work with any tree model for an HTML
document. A default model implementation is supplied that builds a read-only
tree of HTML nodes.


## Examples

Parse an HTML document:

```c#
var html = await new HttpClient().GetStringAsync("http://www.example.com/");
var document = Parser.Parse(html);
```

Parse an HTML document fragment:

```c#
var html = @"
  <div>
    <h1>Example Domain</h1>
    <p>This domain is established to be used for illustrative examples
       in documents. You may use this domain in examples without prior
       coordination or asking for permission.</p>
    <p><a href='http://www.iana.org/domains/example'>More information...</a></p>
  </div>";

var fragment = Parser.ParseFragment(html, null);
```


[win-build-badge]: https://img.shields.io/appveyor/ci/raboof/high5.svg?label=windows
[win-builds]: https://ci.appveyor.com/project/raboof/high5
[nix-build-badge]: https://img.shields.io/travis/atifaziz/High5.svg?label=linux
[nix-builds]: https://travis-ci.org/atifaziz/High5
[myget-badge]: https://img.shields.io/myget/raboof/vpre/High5.svg?label=myget
[edge-pkgs]: https://www.myget.org/feed/raboof/package/nuget/High5
[nuget-badge]: https://img.shields.io/nuget/v/High5.svg
[nuget-pkg]: https://www.nuget.org/packages/High5
[html]: https://html.spec.whatwg.org
[netstd]: https://docs.microsoft.com/en-us/dotnet/standard/net-standard
[parse5]: https://github.com/inikulin/parse5
