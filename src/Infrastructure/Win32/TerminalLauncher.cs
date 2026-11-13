using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Glance
{
    /// <summary>
    /// Meluncurkan terminal interaktif (CMD/PowerShell) lalu mengetikkan perintah ke buffer input
    /// setelah prompt shell (seperti Clink / Oh My Posh / PowerShell Profile) selesai tampil.
    /// </summary>
    public static class TerminalLauncher
    {
        private static readonly object _consoleLock = new object();
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// Membuka terminal dan menjalankan perintah pada sesi interaktif.
        /// Untuk mode Administrator, fallback menggunakan argumen CLI karena pembatasan UIPI Windows.
        /// </summary>
        public static void LaunchAndExecute(
            string exePath,
            string workingDir,
            string commandText,
            string fallbackAdminArgs,
            bool asAdmin)
        {
            string cleanCmd = (commandText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir))
            {
                psi.WorkingDirectory = workingDir;
            }

            if (asAdmin)
            {
                psi.Verb = "runas";
                psi.Arguments = fallbackAdminArgs ?? "";
                Process.Start(psi);
                return;
            }

            // Mulai shell secara interaktif polos tanpa /K atau -Command agar AutoRun/Clink/Oh My Posh
            // selesai menggambar banner & prompt di bagian atas sebelum perintah diketikkan.
            Process process = Process.Start(psi);
            if (process == null || string.IsNullOrEmpty(cleanCmd))
            {
                process?.Dispose();
                return;
            }

            Task.Run(() => AttachWaitAndType(process, cleanCmd));
        }

        private static void AttachWaitAndType(Process process, string commandText)
        {
            lock (_consoleLock)
            {
                IntPtr hOut = INVALID_HANDLE_VALUE;
                IntPtr hIn = INVALID_HANDLE_VALUE;
                try
                {
                    if (!TryAttachConsole(process)) return;

                    hOut = NativeMethods.CreateFileW(
                        "CONOUT$",
                        NativeMethods.GENERIC_READ_WRITE,
                        NativeMethods.FILE_SHARE_READ_WRITE,
                        IntPtr.Zero,
                        NativeMethods.OPEN_EXISTING,
                        0,
                        IntPtr.Zero);

                    hIn = NativeMethods.CreateFileW(
                        "CONIN$",
                        NativeMethods.GENERIC_READ_WRITE,
                        NativeMethods.FILE_SHARE_READ_WRITE,
                        IntPtr.Zero,
                        NativeMethods.OPEN_EXISTING,
                        0,
                        IntPtr.Zero);

                    if (hIn == INVALID_HANDLE_VALUE) return;

                    if (hOut != INVALID_HANDLE_VALUE)
                    {
                        WaitForPromptReady(process, hOut);
                    }
                    else
                    {
                        Thread.Sleep(900);
                    }

                    if (!process.HasExited)
                    {
                        SendCommandToConsole(hIn, commandText);
                    }
                }
                catch { }
                finally
                {
                    if (hOut != INVALID_HANDLE_VALUE) NativeMethods.CloseHandle(hOut);
                    if (hIn != INVALID_HANDLE_VALUE) NativeMethods.CloseHandle(hIn);
                    NativeMethods.FreeConsole();
                    try { process.Dispose(); } catch { }
                }
            }
        }

        private static bool TryAttachConsole(Process process)
        {
            for (int i = 0; i < 60; i++)
            {
                if (process.HasExited) return false;
                NativeMethods.FreeConsole();
                if (NativeMethods.AttachConsole(process.Id)) return true;
                Thread.Sleep(50);
            }
            return false;
        }

        private static void WaitForPromptReady(Process process, IntPtr hOut)
        {
            int lastX = -1;
            int lastY = -1;
            string lastLine = "";
            int promptStableTicks = 0;
            int idleFallbackTicks = 0;

            for (int i = 0; i < 120; i++)
            {
                if (process.HasExited) return;

                if (NativeMethods.GetConsoleScreenBufferInfo(hOut, out var csbi))
                {
                    short curX = csbi.dwCursorPosition.X;
                    short curY = csbi.dwCursorPosition.Y;
                    string line = ReadConsoleRow(hOut, curY, csbi.dwSize.X);

                    bool sameState = curX == lastX && curY == lastY && line == lastLine;
                    bool hasPromptCursor = curY >= 1 && curX > 0 && !string.IsNullOrWhiteSpace(line);

                    if (sameState && hasPromptCursor)
                    {
                        promptStableTicks++;
                        if (promptStableTicks >= 6) return;
                    }
                    else
                    {
                        promptStableTicks = 0;
                    }

                    if (sameState && curY >= 1 && i >= 30)
                    {
                        idleFallbackTicks++;
                        if (idleFallbackTicks >= 10) return;
                    }
                    else
                    {
                        idleFallbackTicks = 0;
                    }

                    lastX = curX;
                    lastY = curY;
                    lastLine = line;
                }

                Thread.Sleep(50);
            }
        }

        private static string ReadConsoleRow(IntPtr hOut, short rowY, short bufferWidth)
        {
            int len = Math.Min(Math.Max((int)bufferWidth, 1), 160);
            char[] row = new char[len];
            var coord = new NativeMethods.COORD { X = 0, Y = rowY };
            if (!NativeMethods.ReadConsoleOutputCharacterW(hOut, row, (uint)len, coord, out uint charsRead))
            {
                return "";
            }
            return new string(row, 0, (int)charsRead).TrimEnd();
        }

        private static void SendCommandToConsole(IntPtr hIn, string commandText)
        {
            var records = new List<NativeMethods.INPUT_RECORD>((commandText.Length + 1) * 2);

            foreach (char ch in commandText)
            {
                AddKeyRecordPair(records, 0, ch);
            }
            AddKeyRecordPair(records, NativeMethods.VK_RETURN, '\r');

            const int chunkSize = 64;
            for (int offset = 0; offset < records.Count; offset += chunkSize)
            {
                int count = Math.Min(chunkSize, records.Count - offset);
                var batch = new NativeMethods.INPUT_RECORD[count];
                records.CopyTo(offset, batch, 0, count);
                NativeMethods.WriteConsoleInputW(hIn, batch, (uint)batch.Length, out _);
                if (offset + chunkSize < records.Count) Thread.Sleep(5);
            }
        }

        private static void AddKeyRecordPair(List<NativeMethods.INPUT_RECORD> records, ushort vk, char ch)
        {
            records.Add(new NativeMethods.INPUT_RECORD
            {
                EventType = NativeMethods.KEY_EVENT,
                KeyEvent = new NativeMethods.KEY_EVENT_RECORD
                {
                    bKeyDown = 1,
                    wRepeatCount = 1,
                    wVirtualKeyCode = vk,
                    UnicodeChar = ch
                }
            });
            records.Add(new NativeMethods.INPUT_RECORD
            {
                EventType = NativeMethods.KEY_EVENT,
                KeyEvent = new NativeMethods.KEY_EVENT_RECORD
                {
                    bKeyDown = 0,
                    wRepeatCount = 1,
                    wVirtualKeyCode = vk,
                    UnicodeChar = ch
                }
            });
        }
    }
}
