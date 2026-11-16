using System.IO;

namespace DevDogs.WebDevices.Core.Config;

/// <summary>
/// Carries the settings file over from the plugin's previous class name. SimHub names a plugin's settings file
/// after the plugin class (<c>&lt;class&gt;.&lt;key&gt;.json</c>), so without this, renaming the class to
/// <c>DDWebDevicesPlugin</c> would start every user with no devices (docs/adr/ADR-0008).
/// </summary>
public static class SettingsCarryOver
{
    /// <summary>
    /// Copies <paramref name="previousFile"/> to <paramref name="currentFile"/> when only the previous file
    /// exists. An existing current file always wins and is never overwritten; the previous file is left in place.
    /// </summary>
    /// <param name="previousFile">The settings file under the previous class name.</param>
    /// <param name="currentFile">The settings file under the current class name.</param>
    /// <returns><see langword="true"/> when the file was copied.</returns>
    /// <exception cref="IOException">The copy failed, for example because the current file appeared meanwhile.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Either file cannot be accessed.</exception>
    public static bool CopyIfMissing(string previousFile, string currentFile)
    {
        if (File.Exists(currentFile) || !File.Exists(previousFile))
        {
            return false;
        }

        File.Copy(previousFile, currentFile, overwrite: false);
        return true;
    }
}
