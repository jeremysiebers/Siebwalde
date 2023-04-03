using System.IO;
using System.Threading.Tasks;

namespace SiebwaldeApp
{
    /// <summary>
    /// Handles reading writing and querying the file system
    /// </summary>
    public class FileManager : IFileManager
    {
        /// <summary>
        /// Writes the text to the specified file
        /// </summary>
        /// <param name="text">Text to write</param>
        /// <param name="path">The path of the file to write</param>
        /// <param name="append">If true, writes the text to the end of the file, otherwise overrides any existing file</param>
        /// <returns></returns>
        public async Task WriteAllTextToFileAsync(string text, string path, bool append = false)
        {
            // TODO: Add exection catching

            // TODO: Normalize and resolve path

            // Lock the task
            await AsyncAwaiter.AwaitAsync(nameof(FileManager) + path, async () =>
            {
                // TODO: Add IoC.Task>run that logs to logger on failure

                // Run the synchronous file access as a new task
                await Task.Run(() =>
                {
                    // Write the log messgae to file 
                    using (var filestream = (TextWriter)new StreamWriter(File.Open(path, append ? FileMode.Append : FileMode.Create)))
                        filestream.Write(text);
                });
            });
        }
    }
}
