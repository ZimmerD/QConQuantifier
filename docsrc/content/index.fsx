(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
QConQuantifier
======================

Setup
-------

1. The QConQuantifier library can be downloaded from <a href="https://github.com/ZimmerD/QConQuantifier">github</a>. 
2. To build the tool please make sure that you have installed:  
	- 2.1 the latest  <a href="https://dotnet.microsoft.com/download">.NET Core SDK</a> AND <a href="https://dotnet.microsoft.com/downloadr">.NET Framework Dev Pack</a>.   
	- 2.2 the fake cli via "dotnet tool install fake-cli -g", build the by calling "fake build". For details regarding projects based uppon the project scaffold please visit <a href="https://github.com/fsprojects/ProjectScaffold">ProjectScaffold</a> or <a href="https://github.com/CSBiology/CsbScaffold">CsbScaffold</a>. 
3. Once everything is set up you can download <a href="https://1drv.ms/u/s!Ak2uNQ51QZNO00S3QjgrSl6XDMVS">sample datasets</a> and get started! 

Example
-------

This example demonstrates running the console tool.

*)
#r "QConQuantifier.dll"
open QConQuantifier

printfn "hello = %i" <| Library.hello 0

(**
Some more info

Samples & documentation
-----------------------

The library comes with comprehensible documentation. 
It can include tutorials automatically generated from `*.fsx` files in [the content folder][content]. 
The API reference is automatically generated from Markdown comments in the library implementation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/QConQuantifier/tree/master/docs/content
  [gh]: https://github.com/fsprojects/QConQuantifier
  [issues]: https://github.com/fsprojects/QConQuantifier/issues
  [readme]: https://github.com/fsprojects/QConQuantifier/blob/master/README.md
  [license]: https://github.com/fsprojects/QConQuantifier/blob/master/LICENSE.txt
*)
