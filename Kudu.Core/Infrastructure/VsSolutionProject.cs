﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kudu.Core.Infrastructure
{
    [DebuggerDisplay("{ProjectName}")]
    public class VsSolutionProject
    {
        private const string ProjectInSolutionTypeName = "Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";        

        private static readonly Type _projectInSolutionType;
        private static readonly PropertyInfo _projectNameProperty;
        private static readonly PropertyInfo _relativePathProperty;
        private static readonly PropertyInfo _projectTypeProperty;

        static VsSolutionProject()
        {
            _projectInSolutionType = Type.GetType(ProjectInSolutionTypeName, throwOnError: false, ignoreCase: false);

            if (_projectInSolutionType != null)
            {
                _projectNameProperty = ReflectionUtility.GetInternalProperty(_projectInSolutionType, "ProjectName");
                _relativePathProperty = ReflectionUtility.GetInternalProperty(_projectInSolutionType, "RelativePath");
                _projectTypeProperty = ReflectionUtility.GetInternalProperty(_projectInSolutionType, "ProjectType");
            }
        }

        private readonly string _solutionPath;
        private readonly object _projectInstance;

        private bool _isWap;
        private bool _isWebSite;
        private IEnumerable<Guid> _projectTypeGuids;
        private string _projectName;
        private string _absolutePath;

        private bool _initialized;

        public IEnumerable<Guid> ProjectTypeGuids
        {
            get
            {
                EnsureProperties();
                return _projectTypeGuids;
            }
        }
        
        public string ProjectName
        {
            get
            {
                EnsureProperties();
                return _projectName;
            }
        }

        public string AbsolutePath
        {
            get
            {
                EnsureProperties();
                return _absolutePath;
            }
        }

        public bool IsWebSite
        {
            get
            {
                EnsureProperties();
                return _isWebSite;
            }
        }

        public bool IsWap
        {
            get
            {
                EnsureProperties();
                return _isWap;
            }
        }

        public VsSolutionProject(string solutionPath, object project)
        {
            _solutionPath = solutionPath;
            _projectInstance = project;
        }

        private void EnsureProperties()
        {
            if (_initialized)
            {
                return;
            }

            _projectName = _projectNameProperty.GetValue<string>(_projectInstance);
            var relativePath = _relativePathProperty.GetValue<string>(_projectInstance);
            var projectType = _projectTypeProperty.GetValue<SolutionProjectType>(_projectInstance);

            _absolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_solutionPath), relativePath));
            _isWebSite = projectType == SolutionProjectType.WebProject;

            if (projectType == SolutionProjectType.KnownToBeMSBuildFormat && File.Exists(_absolutePath))
            {
                // If the project is an msbuild project then extra the project type guids
                _projectTypeGuids = VsHelper.GetProjectTypeGuids(_absolutePath);

                // Check if it's a wap
                _isWap = VsHelper.IsWap(_projectTypeGuids);
            }
            else
            {
                _projectTypeGuids = Enumerable.Empty<Guid>();
            }

            _initialized = true;
        }

        // Microsoft.Build.Construction.SolutionProjectType
        private enum SolutionProjectType
        {
            Unknown,
            KnownToBeMSBuildFormat,
            SolutionFolder,
            WebProject,
            WebDeploymentProject,
            EtpSubProject,
        }
    }
}
