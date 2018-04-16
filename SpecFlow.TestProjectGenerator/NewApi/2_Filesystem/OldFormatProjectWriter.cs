﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using SpecFlow.TestProjectGenerator.NewApi._1_Memory;
using SpecFlow.TestProjectGenerator.NewApi._1_Memory.Extensions;

namespace SpecFlow.TestProjectGenerator.NewApi._2_Filesystem
{
    public class OldFormatProjectWriter : BaseProjectWriter
    {
        public override void WriteProject(Project project, string path)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            string targetFramework;
            try
            {
                targetFramework = project.TargetFrameworks.ToOldNetVersion();
            }
            catch (InvalidOperationException exc)
            {
                throw new ProjectCreationNotPossibleException("Multiple target frameworks don't work with the old csproj format", exc);
            }

            string outputType = project.ProjectType == ProjectType.Exe ? "WinExe" : "Library";
            
            Directory.CreateDirectory(path);
            string projFileName = $"{project.Name}.{project.ProgrammingLanguage.ToProjectFileExtension()}";
            string projFilePath = Path.Combine(path, projFileName);

            const string msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            using (var xw = new XmlTextWriter(
                File.Open(projFilePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8))
            {
                xw.WriteStartDocument();

                // project tag
                xw.WriteStartElement(string.Empty, "Project", msbuildNamespace);
                xw.WriteAttributeString("ToolsVersion", "14.0");
                xw.WriteAttributeString("DefaultTargets", "Build");

                // main property group
                xw.WriteStartElement("PropertyGroup");
                xw.WriteElementString("OutputType", outputType);
                xw.WriteElementString("RootNamespace", project.Name);
                xw.WriteElementString("AssemblyName", project.Name);
                xw.WriteElementString("FileAlignment", "512");
                xw.WriteElementString("TargetFrameworkVersion", targetFramework);

                // close main property group
                xw.WriteEndElement();

                if (project.References.Count > 0)
                {
                    // open item group for library & GAC references
                    xw.WriteStartElement("ItemGroup");

                    // write GAC references
                    foreach (var reference in project.References)
                    {
                        xw.WriteStartElement("Reference");
                        xw.WriteAttributeString("Include", reference.Name);
                        xw.WriteEndElement();
                    }

                    // close item group for library & GAC references
                    xw.WriteEndElement();
                }

                if (project.NuGetPackages.Count > 0)
                {
                    // TODO: add NuGet reference to packages.config
                }

                if (project.Files.Count > 0)
                {
                    // open item group for files
                    xw.WriteStartElement("ItemGroup");

                    // write project files
                    var fileWriter = new ProjectFileWriter();
                    foreach (var file in project.Files)
                    {
                        xw.WriteStartElement("Compile");
                        xw.WriteAttributeString("Include", file.Path);
                        xw.WriteEndElement();
                        fileWriter.Write(file, path);
                    }

                    // close item group for files
                    xw.WriteEndElement();
                }

                // close project tag
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            if (project.References.Count > 0)
            {
                using (var pkgConf = new XmlTextWriter(
                    File.Open(Path.Combine(path, "packages.config"), FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8))
                {
                    // TODO: write packages.config

                }

                // TODO: call nuget.exe to add packages
            }
        }
    }
}