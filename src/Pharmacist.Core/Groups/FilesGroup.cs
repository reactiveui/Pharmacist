// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
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
        private readonly DirectoryNode _rootNode = new(string.Empty);

        /// <summary>
        /// Gets for a file name, the nearest matching full name in the shallowest of the hierarchy.
        /// </summary>
        /// <param name="fileName">The file name to grab.</param>
        /// <returns>The full path if available, null otherwise.</returns>
        public string? GetFullFilePath(string fileName)
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

                var directoryNode = splitDirectory.Aggregate(_rootNode, (current, currentPath) => current.AddChildNode(currentPath));

                directoryNode.AddFile(file);
            }
        }

        private class DirectoryNode : IEqualityComparer<DirectoryNode>, IComparable<DirectoryNode>
        {
            private readonly Dictionary<string, DirectoryNode> _childNodesDict = new();
            private readonly List<DirectoryNode> _childNodes = new();
            private readonly List<FileNode> _files = new();
            private readonly Dictionary<string, FileNode> _filesDict = new();

            public DirectoryNode(string name)
            {
                Name = name;
            }

            public IEnumerable<FileNode> Files => _files;

            public IEnumerable<DirectoryNode> ChildNodes => _childNodes;

            private string Name { get; }

            public bool TryGetFile(string name, out string? outValue)
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
                if (_childNodesDict.TryGetValue(name, out var node))
                {
                    return node;
                }

                node = new DirectoryNode(name);
                _childNodesDict.Add(name, node);
                _childNodes.Add(node);

                return node;
            }

            public void AddFile(string fullPath)
            {
                var name = Path.GetFileName(fullPath);

                if (_filesDict.TryGetValue(name, out var node))
                {
                    return;
                }

                node = new FileNode(fullPath);
                _filesDict.Add(name, node);
                var index = _files.BinarySearch(node);
                if (index < 0)
                {
                    _files.Insert(~index, node);
                }
            }

            /// <inheritdoc />
            public bool Equals(DirectoryNode? x, DirectoryNode? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return ReferenceEquals(x, y) || StringComparer.InvariantCultureIgnoreCase.Equals(x.Name, y.Name);
            }

            /// <inheritdoc />
            public int GetHashCode(DirectoryNode obj)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.Name);
            }

            /// <inheritdoc />
            public int CompareTo(DirectoryNode? other)
            {
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                return ReferenceEquals(null, other) ? 1 : StringComparer.InvariantCultureIgnoreCase.Compare(this, other);
            }
        }

        private class FileNode : IEqualityComparer<FileNode>, IComparable<FileNode>
        {
            public FileNode(string fullPath)
            {
                FullPath = fullPath;
            }

            public string FullPath { get; }

            /// <inheritdoc />
            public bool Equals(FileNode? x, FileNode? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return ReferenceEquals(x, y) || StringComparer.InvariantCultureIgnoreCase.Equals(x.FullPath, y.FullPath);
            }

            /// <inheritdoc />
            public int GetHashCode(FileNode obj)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.FullPath);
            }

            /// <inheritdoc />
            public int CompareTo(FileNode? other)
            {
                return StringComparer.InvariantCultureIgnoreCase.Compare(FullPath, other?.FullPath);
            }
        }
    }
}
