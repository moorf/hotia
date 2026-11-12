// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using osu.Framework;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Ai
{
    public sealed class AiManagerStore
    {
        private const string library_pattern = @"hotia.AiMapper.dll";

        private const string manager_type_name = @"hotia.AiMapper.AiManager";

        public IAiManager? Manager { get; private set; }

        public AiManagerStore(Storage? storage = null)
        {
            string[] files = Directory.GetFiles(RuntimeInfo.StartupDirectory, library_pattern);

            if (files.Length > 0)
            {
                var manager = loadManagerFromFile(files.First());
                if (manager != null)
                {
                    createManager(manager);
                }
            }
        }

        private static Assembly? loadManagerFromFile(string path)
        {
            try
            {
                return Assembly.LoadFrom(path);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    ex,
                    $"Could not load AiMapper assembly: {path}");

                return null;
            }
        }

        private bool createManager(Assembly assembly)
        {
            try
            {
                var type = assembly.GetType(
                    manager_type_name,
                    throwOnError: false);

                if (type == null)
                    return false;

                Manager = (IAiManager)Activator.CreateInstance(type)!;

                Logger.Log($"Loaded AiManager from {assembly.GetName().Name}.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Could not create AiManager from {assembly.FullName}.");
                return false;
            }
        }
    }
}
