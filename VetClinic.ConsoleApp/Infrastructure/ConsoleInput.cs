using System.Runtime.InteropServices;
using System.Text;

namespace VetClinic.ConsoleApp.Infrastructure;

internal static class ConsoleInput
{
    private const int StdInputHandle = -10;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ReadConsoleW(
        IntPtr hConsoleInput,
        [Out] char[] lpBuffer,
        uint nNumberOfCharsToRead,
        out uint lpNumberOfCharsRead,
        IntPtr pInputControl);

    /// <summary>Повертає введений рядок без символів кінця рядка або null, якщо вхідний потік закрито.</summary>
    public static string? ReadLine()
    {
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected)
        {
            return Console.ReadLine();
        }

        var handle = GetStdHandle(StdInputHandle);
        var text = new StringBuilder();
        var buffer = new char[256];

        while (true)
        {
            if (!ReadConsoleW(handle, buffer, (uint)buffer.Length, out var read, IntPtr.Zero))
            {
                // Нестандартний хост консолі — повертаємося до звичайного способу.
                return text.Length == 0 ? Console.ReadLine() : text.ToString();
            }

            if (read == 0)
            {
                return text.Length == 0 ? null : text.ToString();
            }

            text.Append(buffer, 0, (int)read);

            if (buffer[read - 1] == '\n')
            {
                break;
            }
        }

        var line = text.ToString().TrimEnd('\r', '\n');

        // Ctrl+Z (кінець потоку) у консолі Windows
        return line.Length > 0 && line[0] == '\u001a' ? null : line;
    }
}
