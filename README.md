---
ArtifactType: executable
Documentation: N/A
Language: csharp
Platform: windows, linux
Stackoverflow: N/A
Tags: roslyn,analyzers,precompiled
---

# Peeker

Peeker is a tool that runs Roslyn code analyzers on precompiled .NET binaries. If source mappings are provided in the form of a PDB file, the inspection results are associated with the original source locations. Peeker outputs its results in the SARIF file format.

## Getting Started

Clone Peeker and build with `dotnet build`. In use, you will need to supply libraries that provide Roslyn analyzers (i.e. Microsoft.CodeQuality.Analyzers.dll) and their dependencies.

### Prerequisites

.NET 6.0

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Versioning and changelog

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the tags on this repository.

## Acknowledgments

* This project makes great use of [ILSpy](https://github.com/ICSharpCode/ILSpy), a third party .NET decompilation library.

See also [NOTICE.md](https://github.com/microsoft/peeker/blob/main/NOTICE.md).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.