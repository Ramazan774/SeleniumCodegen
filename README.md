#SpecFlow Test Generator

A console-based tool that captures your browser interactions and automatically generates '.feature' and 'steps.cs' files for SpecFlow using Selenium in C#.
Planned to be a Visual Studio 2022 extension in near future.
Inspired by Playwright Codegen, but made for .NET ecosystem.

---

##Features

- Record browser actions using Selenium
- Automatically generate:
  - Gherkin-style '.feature' files
  - Corresponding 'steps.cs' bindings for SpecFlow
- Supports basic click, input, navigation actions
- Works with Chrome

---

##How it works
1. Launch the console app
   - dotnet clean
   - dotnet build
   - dotnet run
2. Interact with your browser
3. The tool records your actions
4. It generates:
   - 'MyFeature.feature'
   - 'MyFeatureSteps.cs'

---

##Prerequisities

- .NET SDK (8.0 or later)
- Chrome browser
- IDE
