using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace AIMInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define the path where the AIM app will be extracted
            string aimZipPath = "path/to/your/aim.zip"; // Replace with actual path to AIM ZIP
            string extractPath = Path.Combine(Environment.CurrentDirectory, "AIM");

            // Extract the AIM app
            ZipFile.ExtractToDirectory(aimZipPath, extractPath);

            // Run the Deploy-AIM.ps1 script
            Process process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File '{Path.Combine(extractPath, "Deploy-AIM.ps1")}'";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            // Start the process and wait for it to complete
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Display output
            Console.WriteLine(output);
        }
    }
}