# Postit – an exercise in Azure Static Web Apps

This personal hobby project is meant for exploring the capabilities of Azure Static Web Apps. The goal is to build a full stack web application using **free** Azure services and the .NET stack. Costs for running this solution should be zero.

### Database

**Azure Cosmos DB for NoSQL** is used as the database technology for data persistence. The free tier of Cosmos DB provides a decent amount of storage and provisioned throughput. There is no monthly quota, so the service can keep running 24/7.

### Backend

Azure Static Web Apps automatically integrates with **Azure Functions** as an API backend. The API project (C# isolated worker model) is deployed as part of the static web app (Managed API) without managing a separate Azure Functions resource. This means that we don't have to provision an Azure storage account separately to store the Function App code, thus avoiding any nominal costs related to storage.

### Frontend

**ASP.NET Core Blazor WebAssembly** is used to create an interactive SPA frontend. This is convenient, as the API and client projects can share a library project for common classes, such as DTO objects.

### Authentication & authorization

Several authentication providers are pre-configured and supported with Azure Static Web Apps. This project utilizes Microsoft Entra ID and Google (preview). Any user can log in and authenticate, but additional custom user roles are assigned to invited users only. These custom roles are then used to authorize specific API endpoints and client pages. This way some features can be restricted to a list of predefined users.

### Datasets

The project uses three sample JSON datasets from <a href="https://jsonplaceholder.typicode.com/">JSONPlaceholder</a>:
- <a href="https://jsonplaceholder.typicode.com/users">Users</a>
- <a href="https://jsonplaceholder.typicode.com/posts">Posts</a>
- <a href="https://jsonplaceholder.typicode.com/comments">Comments</a>

Users write posts, which in turn have comments. These datasets are stored in a Cosmos database called `Postit` in containers named `Users`, `Posts` and `Comments` respectively. Data is fetched from Cosmos using the Functions API and displayed in the client Blazor WebAssembly app.