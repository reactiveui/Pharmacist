// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pharmacist.Core.Groups
{
    /// <summary>
    /// Contains details about a folder within a NuGet package.
    /// </summary>
    public class FilesGroup
    {
        private readonly DirectoryNode _rootNode = new DirectoryNode(string.Empty);

        /// <summary>
        /// Gets for a file name, the nearest matching full name in the shallowest of the hierarchy.
        /// </summary>
        /// <param name="fileName">The file name to grab.</param>
        /// <returns>The full path if available, null otherwise.</returns>
        public string GetFullFilePath(string fileName)
        {
            var processing = new Queue<DirectoryNode>(new[] { _rootNode });

            while (processing.Count != 0)
            {
                var current = processing.Dequeue();

                if (current.TryGetFile(fileName, out var fullPath))
                {
                    return fullPath;
                }

                foreach (var child in current.ChildNodes)
                {
                    processing.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all the files contained within the files group.
        /// </summary>
        /// <returns>The files.</returns>
        public IEnumerable<string> GetAllFileNames()
        {
            var processing = new Queue<DirectoryNode>(new[] { _rootNode });

            while (processing.Count != 0)
            {
                var current = processing.Dequeue();
                foreach (var file in current.Files)
                {
                    yield return file.FullPath;
                }

                foreach (var child in current.ChildNodes)
                {
                    processing.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Adds files if they don't already exist to our collection.
        /// </summary>
        /// <param name="files">The files to add.</param>
        public void AddFiles(IEnumerable<string> files)
        {
            if (files is null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            foreach (var file in files)
            {
                var directoryPath = Path.GetDirectoryName(file);

                var splitDirectory = directoryPath?.Split(new[] { Path.PathSeparator, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                var directoryNode = _rootNode;

                foreach (var currentPath in splitDirectory)
                {
                    directoryNode = directoryNode.AddChildNode(currentPath);
                }

                directoryNode?.AddFile(file);
            }
        }

        private class DirectoryNode : IEqualityComparer<DirectoryNode>, IComparable<DirectoryNode>
        {
            private readonly Dictionary<string, DirectoryNode> _childNodesDict = new Dictionary<string, DirectoryNode>();
            private readonly List<DirectoryNode> _childNodes = new List<DirectoryNode>();
            private readonly List<FileNode> _files = new List<FileNode>();
            private readonly Dictionary<string, FileNode> _filesDict = new Dictionary<string, FileNode>();

            public DirectoryNode(DirectoryNode parent, string name)
            {
                Name = name;
                Parent = parent;
            }

            public DirectoryNode(string name)
                : this(null, name)
            {
            }

            public string Name { get; }

            public string FullPath
            {
                get
                {
                    if (Parent == null || string.IsNullOrWhiteSpace(Parent.FullPath))
                    {
                        return Name;
                    }

                    return Parent.FullPath + Path.DirectorySeparatorChar + Name;
                }
            }

            public DirectoryNode Parent { get; }

            public IEnumerable<FileNode> Files => _files;

            public IEnumerable<DirectoryNode> ChildNodes => _childNodes;

            public bool TryGetChildNode(string path, out DirectoryNode outValue)
            {
                return _childNodesDict.TryGetValue(path, out outValue);
            }

            public bool TryGetFile(string name, out string outValue)
            {
                if (_filesDict.TryGetValue(name, out var node))
                {
                    outValue = node.FullPath;
                    return true;
                }

                outValue = null;
                return false;
            }

            public DirectoryNode AddChildNode(string name)
            {
                if (!_childNodesDict.TryGetValue(name, out var node))
                {
                    node = new DirectoryNode(this, name);
                    _childNodesDict.Add(name, node);
                    _childNodes.Add(node);
                }

                return node;
            }

            public FileNode AddFile(string fullPath)
            {
                var name = Path.GetFileName(fullPath);

                if (!_filesDict.TryGetValue(name, out var node))
                {
                    node = new FileNode(name, fullPath);
                    _filesDict.Add(name, node);
                    var index = _files.BinarySearch(node);
                    if (index < 0)
                    {
                        _files.Insert(~index, node);
                    }
                }

                return node;
            }

            /// <inheritdoc />
            public bool Equals(DirectoryNode x, DirectoryNode y)
            {
                return StringComparer.InvariantCultureIgnoreCase.Equals(x?.Name, y?.Name);
            }

            /// <inheritdoc />
            public int GetHashCode(DirectoryNode obj)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.Name);
            }

            /// <inheritdoc />
            public int CompareTo(DirectoryNode other)
            {
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                if (ReferenceEquals(null, other))
                {
                    return 1;
                }

                return StringComparer.InvariantCultureIgnoreCase.Compare(this, other);
            }
        }

        private class FileNode : IEqualityComparer<FileNode>, IComparable<FileNode>
        {
            public FileNode(string fileName, string fullPath)
            {
                FullPath = fullPath;
                FileName = fileName;
            }

            public string FullPath { get; }

            public string FileName { get; }

            /// <inheritdoc />
            public bool Equals(FileNode x, FileNode y)
            {
                return StringComparer.InvariantCultureIgnoreCase.Equals(x?.FullPath, y?.FullPath);
            }

            /// <inheritdoc />
            public int GetHashCode(FileNode obj)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.FullPath);
            }

            /// <inheritdoc />
            public int CompareTo(FileNode other)
            {
                return StringComparer.InvariantCultureIgnoreCase.Compare(FullPath, other?.FullPath);
            }
        }
    }
}
