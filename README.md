# About Simulysis
Simulysis a method and a tool that is based on the WAVE-CIA method for change impact analysis of Simulink projects. For an uploaded project, Simulysis extracts the information to build the corresponding call graph. Then, it calculates the change set from the two call graphs corresponding to two versions of the Simulink project. Finally, it computes the impact set of the project with the given change set. The tool provides an effective and efficient way to address the change impact analysis of Simulink projects.

# Installation
## Prerequirements
- Visual Studio 2022
  
- Microsoft .NET SDK 6 or higher
  
- MySQL Workbench 8.0

- IIS Web Server

## How To Run
- Clone the repo <br />

  ```
  git clone https://github.com/KaoSon2004/Simulysis-CIA.git
  ```
  
- Import Official_DB.sql to your MySql Workbench 8.0
  
- Open Visual Studio 2022
  
- Set Simulysis as Startup project
  
- Edit Simulysis/appsettings.json file
  
  - Add your database <br />
  
    ```
    "ConnectionStrings": {
      "Default": "Server=;Database=;Uid=;Pwd=;Connect Timeout=600"
    }
    ```

  - To enable git import function, you need a git personal git access token, please check the official [Personal Access Token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens) guides, then add <br />
  
    ```
    "pat":
    ```
    
- Run the application
