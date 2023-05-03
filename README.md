# GitHub Actions Gates Samples

This project implements two [GitHub Actions protection rules](https://docs.github.com/en/actions/deployment/protecting-deployments/creating-custom-deployment-protection-rules) gates that can be used to gate the execution of a deployment to an environment using GitHub Actions. The gate will approve or reject the deployment based on the rules configured for the gate.

Two gates are provided as samples:
- **Deploy Hours gate** Configure a set of hours (and optionally days) when deployments are allowed, if a deployment is triggered outside of the configured hours the gate will postpone the (automatic) approval until the next allowed time.
- **Issues Gate** The gate is define by a query and or a search condition on GitHub. If one the conditions return more than X results the gate will reject the deployment, otherwise it will allow it.

![](docs/issue-gates-protection-rule.png)

## Objectives

I've create this sample with a few objectives in mind:
- Demonstrate how to create a GitHub Actions Gate
- Create a _simple_ gate that can be used as learning example and yet implements a useful real world scenario
  - Make the code as simple as possible, have a very small set of external dependencies and minimize the number of patterns (use dependency injection since we need it for unit tests but avoid containers as much as possible)
  - This wasn't fully achieved, I may have get carried out and implemented a basic framework that can be used to create other gates without much effort.
- Be hosted on the _cheap_ (or free) with usage based costs and without fixed costs.
- Gates should be user configurable per repository and per environment. The configuration file is a YAML file stored on the repository itself.

And also because I wanted to test [GitHub Copilot X](https://github.com/features/preview/copilot-x) in a real world scenario.

## Hosting

I don't provide any hosting for this project so it needs to be self hosted in order to be used. 

The project is implemented as .Net Core 6 Azure Functions and can be hosted on Azure Functions or any other hosting provider that supports .Net Core 6 Azure Functions but it requires an [Azure Service Bus]([https://](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview)) instance.

Optionally it can use [Azure Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) for monitoring and logging.

The samples are provided with Bicep files (infrastructure as code) that provisions all the requirements and a setup script that can configure everything for you. The setup script is not required, you can manually configure everything if you prefer.

GitHub Actions that can deploy the gates to Azure are also provided.

## Installation

See the [installation guide](docs/Installation.md) for instructions on how to install and configure the provided gates.

## Configuring the gates

See the [configuration guide](docs/Configuration.md) for instructions on how to configure the gates.

## Contributing

If you'd like to contribute to this project, please fork the repository and submit a pull request. We welcome contributions of all kinds, including bug fixes, feature requests, and documentation improvements.

## Image Credits

In the [logos](logos) folder you'll find a few logos that may be used in your GitHub Apps. 

They have been generated using Open's [AI DALL-E](https://openai.com/blog/dall-e/) and cannot be used for commercial purposes.

## Copyright and License

This project is licensed under the [MIT License](LICENSE.txt) - Copyright (c) 2023 Tiago Pascoal

The code uses a very small portion of code derived from [Octokit.net.extensions](https://github.com/mirsaeedi/octokit.net.extensions) which is licensed under the MIT License - Copyright (c) 2018 Ehsan Mirsaeedi

Besides using [Octokit.Net](https://github.com/octokit/octokit.net) as a dependency, the project also copied some files as well (with some changes) to facilitate Unit testing. Octokit.Net is licensed under the MIT License - Copyright (c) 2023 GitHub, Inc.

The derivations are clearkly marked as such in the source code.
