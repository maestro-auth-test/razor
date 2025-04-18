﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// An item in a <see cref="RazorProjectFileSystem"/>.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerToString) + "()}")]
public abstract class RazorProjectItem
{
    /// <summary>
    /// Path specified in <see cref="RazorProject.EnumerateItems(string)"/>.
    /// </summary>
    public abstract string BasePath { get; }

    /// <summary>
    /// File path relative to <see cref="BasePath"/>. This property uses the project path syntax,
    /// using <c>/</c> as a path separator and does not follow the operating system's file system
    /// conventions.
    /// </summary>
    public abstract string FilePath { get; }

    /// <summary>
    /// The absolute physical (file system) path to the file, including the file name.
    /// </summary>
    public abstract string PhysicalPath { get; }

    /// <summary>
    /// The relative physical (file system) path to the file, including the file name. Relative to the
    /// physical path of the <see cref="BasePath"/>.
    /// </summary>
    public virtual string RelativePhysicalPath => null;

    /// <summary>
    /// A scope identifier that will be used on elements in the generated class, or null.
    /// </summary>
    public virtual string CssScope { get; }

    /// <summary>
    /// Gets the document kind that should be used for the generated document. If possible this will be inferred from the file path. May be null.
    /// </summary>
    public virtual RazorFileKind FileKind
        => FilePath == null
            ? RazorFileKind.None
            : FileKinds.GetFileKindFromPath(FilePath);

    /// <summary>
    /// Gets the file contents as readonly <see cref="Stream"/>.
    /// </summary>
    /// <returns>The <see cref="Stream"/>.</returns>
    public abstract Stream Read();

    /// <summary>
    /// Gets a value that determines if the file exists.
    /// </summary>
    public abstract bool Exists { get; }

    /// <summary>
    /// The root relative path of the item.
    /// </summary>
    public string CombinedPath
        => BasePath == RazorProjectFileSystem.DefaultBasePath
            ? FilePath
            : BasePath + FilePath;

    /// <summary>
    /// The extension of the file.
    /// </summary>
    public string Extension
    {
        get
        {
            var index = FileName.LastIndexOf('.');
            if (index == -1)
            {
                return null;
            }
            else
            {
                return FileName.Substring(index);
            }
        }
    }

    /// <summary>
    /// The name of the file including the extension.
    /// </summary>
    public string FileName
    {
        get
        {
            var index = FilePath.LastIndexOf('/');
            return FilePath.Substring(index + 1);
        }
    }

    /// <summary>
    /// File path relative to <see cref="BasePath"/> without the extension.
    /// </summary>
    public string FilePathWithoutExtension
    {
        get
        {
            var index = FilePath.LastIndexOf('.');
            if (index == -1)
            {
                return FilePath;
            }
            else
            {
                return FilePath.Substring(0, index);
            }
        }
    }

    internal virtual RazorSourceDocument GetSource()
        => RazorSourceDocument.ReadFrom(this);

    protected virtual string DebuggerToString()
        => CombinedPath;
}
