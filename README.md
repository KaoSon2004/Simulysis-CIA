# About This Project
UET. SQA320 - The project to create a tool for analyze the projects / models created in Simulink
# Installation
## 
## Prerequirements
- Visual Studio 2022
- Microsoft .NET SDK 6 or higher
- MySQL Workbench 8.0

## How To Run
- Clone the repo
  `git clone https://github.com/KaoSon2004/Simulysis-CIA.git`
- Import Official_DB to your MySql Workbench 8.0
- Open Visual Studio 2022
- Set Simulysis as Startup project
- Edit appsettings.json file
  - Add your database
    `  "ConnectionStrings": {
      "Default": "Server=;Database=;Uid=;Pwd=;Connect Timeout=600"
    },`
  - To enable git import function, you need a git personal git access token, please check the official [Personal Access Token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-  your-personal-access-tokens) guides, then add
    `"pat": `
- Run the application
