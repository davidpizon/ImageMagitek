﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using ImageMagitek.Colors;
using ImageMagitek.Project;
using ImageMagitek.Project.Serialization;
using Monaco.PathTree;

namespace ImageMagitek.Services
{
    public interface IProjectService
    {
        ISet<ProjectTree> Projects { get; }
        ISet<IProjectResource> GlobalResources { get; }

        MagitekResult LoadSchemaDefinition(string schemaFileName);
        void SetSchemaDefinition(XmlSchemaSet schemas);
        bool TryAddGlobalResource(IProjectResource resource);

        MagitekResult<ProjectTree> NewProject(string projectName);
        MagitekResults<ProjectTree> OpenProjectFile(string projectFileName);
        MagitekResult SaveProject(ProjectTree projectTree);
        MagitekResult SaveProjectAs(ProjectTree projectTree, string projectFileName);
        void CloseProject(ProjectTree projectTree);
        void CloseProjects();

        MagitekResult<ResourceNode> AddResource(ResourceNode parentNode, IProjectResource resource, bool saveProject);
        MagitekResult<ResourceNode> CreateNewFolder(ResourceNode parentNode, string name, bool saveProject);

        ProjectTree GetContainingProject(ResourceNode node);
        ProjectTree GetContainingProject(IProjectResource resource);
        bool AreResourcesInSameProject(IProjectResource a, IProjectResource b);
    }

    public class ProjectService : IProjectService
    {
        public ISet<ProjectTree> Projects { get; } = new HashSet<ProjectTree>();
        public ISet<IProjectResource> GlobalResources { get; } = new HashSet<IProjectResource>();

        private XmlSchemaSet _schemas = new XmlSchemaSet();
        private readonly ICodecService _codecService;
        private readonly IColorFactory _colorFactory;

        public ProjectService(ICodecService codecService, IColorFactory colorFactory)
        {
            _codecService = codecService;
            _colorFactory = colorFactory;
        }

        public ProjectService(ICodecService codecService, IColorFactory colorFactory, IEnumerable<IProjectResource> globalResources)
        {
            _codecService = codecService;
            _colorFactory = colorFactory;
            GlobalResources = globalResources.ToHashSet();
        }

        public bool TryAddGlobalResource(IProjectResource resource) => GlobalResources.Add(resource);

        public MagitekResult<ProjectTree> NewProject(string projectFileName)
        {
            if (Projects.Any(x => string.Equals(x.Name, projectFileName, StringComparison.OrdinalIgnoreCase)))
                return new MagitekResult<ProjectTree>.Failed($"{projectFileName} already exists in the solution");

            var projectName = Path.GetFileNameWithoutExtension(projectFileName);
            var projectNode = new ProjectNode(projectName, new ImageProject(projectName));
            var projectTree = new ProjectTree(new PathTree<IProjectResource>(projectNode));
            projectTree.FileLocation = projectFileName;
            Projects.Add(projectTree);
            return new MagitekResult<ProjectTree>.Success(projectTree);
        }

        public MagitekResult LoadSchemaDefinition(string schemaFileName)
        {
            if (!File.Exists(schemaFileName))
                return new MagitekResult.Failed($"File '{schemaFileName}' does not exist");

            try
            {
                using var schemaStream = File.OpenRead(schemaFileName);
                _schemas = new XmlSchemaSet();
                _schemas.Add("", XmlReader.Create(schemaStream));
                return MagitekResult.SuccessResult;
            }
            catch (Exception ex)
            {
                return new MagitekResult.Failed($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SetSchemaDefinition(XmlSchemaSet schemas)
        {
            _schemas = schemas;
        }

        public MagitekResults<ProjectTree> OpenProjectFile(string projectFileName)
        {
            if (string.IsNullOrWhiteSpace(projectFileName))
                throw new ArgumentException($"{nameof(OpenProjectFile)} cannot have a null or empty value for '{nameof(projectFileName)}'");

            if (!File.Exists(projectFileName))
                return new MagitekResults<ProjectTree>.Failed($"File '{projectFileName}' does not exist");

            try
            {
                var deserializer = new XmlGameDescriptorReader(_schemas, _codecService.CodecFactory, _colorFactory, GlobalResources);
                var result = deserializer.ReadProject(projectFileName);

                return result.Match(
                    success =>
                    {
                        Projects.Add(success.Result);
                        return result;
                    },
                    fail => result
                );
            }
            catch (Exception ex)
            {
                return new MagitekResults<ProjectTree>.Failed($"Failed to open project '{projectFileName}': {ex.Message}");
            }
        }

        public MagitekResult SaveProject(ProjectTree projectTree)
        {
            if (projectTree is null)
                throw new InvalidOperationException($"{nameof(SaveProject)} parameter '{nameof(projectTree)}' was null");

            if (string.IsNullOrWhiteSpace(projectTree.FileLocation))
                throw new InvalidOperationException($"{nameof(SaveProject)} cannot have a null or empty value for the project's file location");

            try
            {
                var serializer = new XmlGameDescriptorWriter(GlobalResources);
                return serializer.WriteProject(projectTree, projectTree.FileLocation);
            }
            catch (Exception ex)
            {
                return new MagitekResult.Failed($"Failed to save project: {ex.Message}");
            }
        }

        public MagitekResult SaveProjectAs(ProjectTree projectTree, string projectFileName)
        {
            if (projectTree is null)
                throw new InvalidOperationException($"{nameof(SaveProjectAs)} parameter '{nameof(projectTree)}' was null");

            if (string.IsNullOrWhiteSpace(projectFileName))
                throw new ArgumentException($"{nameof(SaveProjectAs)} cannot have a null or empty value for '{nameof(projectFileName)}'");

            try
            {
                var serializer = new XmlGameDescriptorWriter(GlobalResources);
                var result = serializer.WriteProject(projectTree, projectFileName);
                if (result.Value is MagitekResult.Success)
                    projectTree.FileLocation = projectFileName;

                return result;
            }
            catch (Exception ex)
            {
                return new MagitekResult.Failed($"Failed to save project: {ex.Message}");
            }
        }

        public void CloseProject(ProjectTree projectTree)
        {
            if (projectTree is null)
                throw new InvalidOperationException($"{nameof(CloseProject)} parameter '{nameof(projectTree)}' was null");

            if (Projects.Contains(projectTree))
            {
                foreach (var file in projectTree.Tree.EnumerateBreadthFirst().Select(x => x.Value).OfType<DataFile>())
                    file.Close();

                Projects.Remove(projectTree);
            }
        }

        public void CloseProjects()
        {
            foreach (var projectTree in Projects)
            {
                foreach (var file in projectTree.Tree.EnumerateDepthFirst().Select(x => x.Value).OfType<DataFile>())
                    file.Close();
            }
            Projects.Clear();
        }

        public MagitekResult<ResourceNode> AddResource(ResourceNode parentNode, IProjectResource resource, bool saveProject)
        {
            var projectTree = Projects.FirstOrDefault(x => x.ContainsNode(parentNode));

            if (projectTree is null)
                return new MagitekResult<ResourceNode>.Failed($"{parentNode.Value.Name} is not contained within any loaded project");


            var addResult = projectTree.AddResource(parentNode, resource);

            return addResult.Match(
                addSuccess =>
                {
                    if (saveProject)
                    {
                        var saveResult = SaveProject(projectTree);
                        return saveResult.Match(
                            saveSuccess => addResult,
                            saveFailed => new MagitekResult<ResourceNode>.Failed(saveFailed.Reason));
                    }
                    else
                        return addResult;

                },
                addFailed => addResult);
        }

        public MagitekResult<ResourceNode> CreateNewFolder(ResourceNode parentNode, string name, bool saveProject)
        {
            var projectTree = Projects.FirstOrDefault(x => x.ContainsNode(parentNode));

            if (projectTree is null)
                return new MagitekResult<ResourceNode>.Failed($"{parentNode.Value.Name} is not contained within any loaded project");


            var addResult = projectTree.CreateNewFolder(parentNode, name);

            return addResult.Match(
                addSuccess =>
                {
                    if (saveProject)
                    {
                        var saveResult = SaveProject(projectTree);
                        return saveResult.Match(
                            saveSuccess => addResult,
                            saveFailed => new MagitekResult<ResourceNode>.Failed(saveFailed.Reason));
                    }
                    else
                        return addResult;

                },
                addFailed => addResult);
        }

        public ProjectTree GetContainingProject(ResourceNode node)
        {
            return Projects.FirstOrDefault(x => x.ContainsNode(node)) ??
                throw new ArgumentException($"{nameof(GetContainingProject)} could not locate the node '{node.PathKey}'");
        }

        public ProjectTree GetContainingProject(IProjectResource resource)
        {
            return Projects.FirstOrDefault(x => x.ContainsResource(resource)) ??
                throw new ArgumentException($"{nameof(GetContainingProject)} could not locate the resource '{resource.Name}'");
        }

        public bool AreResourcesInSameProject(IProjectResource a, IProjectResource b)
        {
            var projectA = Projects.FirstOrDefault(x => x.ContainsResource(a));
            var projectB = Projects.FirstOrDefault(x => x.ContainsResource(b));

            return ReferenceEquals(projectA, projectB);
        }
    }
}
