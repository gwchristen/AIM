# Compilation Instructions

## Overview
This document provides step-by-step instructions for compiling the AIM-Installer.cs and the portable application in the AIM repository.

## Prerequisites
Make sure you have the following software installed:
- [.NET SDK](https://dotnet.microsoft.com/download/dotnet) (version specified in the project needs)
- A code editor or IDE (e.g., Visual Studio, Visual Studio Code)

## Steps for Compiling AIM-Installer.cs

1. **Clone the Repository**  
   If you haven't already cloned the AIM repository, do so by running:
   ```bash
   git clone https://github.com/gwchristen/AIM.git
   cd AIM
   ```

2. **Open the Project**  
   Open the AIM project folder in your preferred code editor or IDE.

3. **Restore Dependencies**  
   Before building the project, ensure all dependencies are restored:
   ```bash
   dotnet restore
   ```

4. **Compile the Installer**  
   Compile the AIM-Installer.cs file:
   ```bash
   dotnet build AIM-Installer.cs
   ```

5. **Locate the Output**  
   The compiled installer will typically be found in the `bin/Debug/net5.0/` or similar directory, depending on the .NET version.

## Steps for Compiling the Portable Application

1. **Navigate to the Portable Application Directory**  
   Navigate to the directory containing the portable application code:
   ```bash
   cd PortableApp
   ```

2. **Restore Dependencies**  
   Just like the installer, restore the dependencies:
   ```bash
   dotnet restore
   ```

3. **Compile the Portable Application**  
   Build the portable application:
   ```bash
   dotnet build
   ```

4. **Locate the Output**  
   The resulting portable application will be located in the `bin/Debug/net5.0/` or corresponding directory.

## Conclusion
After following these steps, you should have successfully compiled both the AIM installer and the portable application. If you encounter any issues, consult the project's README or seek support from the repository maintainers.
