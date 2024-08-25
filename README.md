# GenAI.Reference.Samples

## Overview
GenAI.Reference.Samples has 2 samples:
1. Wade, the AI Meteorologist - this sample demonstrates simple tool calling to fetch weather from an API and have the LLM provide a weather report in Wade's persona.
1. TaxImpactAnalyzer - demonstrates AgentGroupChat that leverages the Semantic Kernel to analyze and review the tax implications of documents, focusing on US tax law. The application uses two agents: `TaxImpactAnalyzerAgent` and `TaxImpactReviewerAgent`. The `TaxImpactAnalyzerAgent` analyzes the tax implications of an image, while the `TaxImpactReviewerAgent` reviews the analysis and provides a recommendation on whether to keep or discard the document.

## Features
1. Wade, the AI Meteorologist:
- Accepts a valid location
- Fetches weather data json from an API.
- Wade provides the weather report based on the weather data.

2. TaxImpactAnalyzer:
- Analyze tax implications of documents based on US tax law.
- Review the analysis and provide recommendations.
- Supports image uploads for analysis.
- Interactive chat interface using Spectre.Console.

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022/VS Code or later
- Azure OpenAI API Key and Endpoint
- WeatherAPI.com api key

## Setup

### 1. Clone the Repository

### 2. Configure User Secrets

Add your Azure OpenAI API Key and Endpoint to the user secrets:

```bash
dotnet user-secrets set "AI:EASub:AzureOpenAI:Endpoint" "<azure openai endpoint goes here>" 
dotnet user-secrets set "AI:EASub:AzureOpenAI:ApiKey" "<azure openai api key goes here>"
dotnet user-secrets set "AI:WeatherApi:ApiKey" "<weather api key goes here>"
```


### 3. Install Dependencies

Restore the required NuGet packages:

```bash
dotnet restore
```


## Running the Application

### Wade, the AI Meteorologist
Comment the line `await RunTaxImpactAnalyzer(kernel);` and uncomment the line `await RunWadeTheMeteorologist(weatherApiKey, kernel);` in Program.cs

### TaxImpactAnalyzer
Comment the line `await RunWadeTheMeteorologist(weatherApiKey, kernel);` and uncomment the line `await RunTaxImpactAnalyzer(kernel);` in Program.cs

Run the application using Visual Studio or the .NET CLI:

```bash
dotnet run
```

Enter the path for the document you want to analyze when prompted. The application will analyze the document and provide a recommendation based on the tax implications.
In the User Prompt type in 'data/donation01.jpg' as the path for the sample receipt in the data folder.

## Usage

1. When prompted, upload your receipt, letter, or document for tax impact analysis. There's a sample receipt in the data folder. 
2. The `TaxImpactAnalyzerAgent` will analyze the document and provide an analysis.
3. The `TaxImpactReviewerAgent` will review the analysis and ask a key question based on the user's input.
4. The application will provide a recommendation on whether to keep or discard the document.

## Project Structure

- `Program.cs`: Main entry point of the application.
- `GenAI.AgentGroupChat.Core`: Core logic and classes for the application.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes.

## File Descriptions

### Program.cs
The main entry point of the application. It sets up the configuration, initializes the agents, and handles the user interaction loop.

### RecoTerminationStrategy.cs
Defines a custom termination strategy for the chat. The conversation terminates when the final message contains the term "I recommend".

### TaxImpactAnalyzer.txt
Contains the SP (System Prompt) instructions for the `TaxImpactAnalyzerAgent`. This agent analyzes the tax implications of an image, focusing on US tax law.

### TaxImpactReviewer.txt
Contains the SP (System Prompt) instructions for the `TaxImpactReviewerAgent`. This agent reviews the Tax Impact Analysis provided by the `TaxImpactAnalyzerAgent` and asks the user key questions to provide a recommendation.

### WeatherService.cs
Provides weather data from weatherapi.com using Semantic Kernel's KernelFunction invokation