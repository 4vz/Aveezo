using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class Terminal
    {
        private static CancellationTokenSource terminalCancel = null;

        private static bool? isAvailable = null;

        public static bool IsAvailable
        {
            get
            {
                if (isAvailable == null)
                {
                    if (Environment.UserInteractive)
                    {
                        isAvailable = true;
                        try { var t = Console.WindowHeight; }
                        catch { isAvailable = false; }
                    }
                    else
                        isAvailable = false;
                }
                return isAvailable.Value;
            }
        }

        public static void Cancel()
        {
            if (terminalCancel != null)
            {
                terminalCancel.Cancel();
                ClearCurrentLine();
            }
        }

        public static char Read()
        {
            return ReadKey().KeyChar;
        }

        /// <summary>
        /// ReadKey until returns a value, even cancelled.
        /// </summary>
        /// <returns></returns>
        public static ConsoleKeyInfo ReadKey()
        {
            ConsoleKeyInfo keyInfo;

            while (true)
            {
                terminalCancel = new CancellationTokenSource();

                keyInfo = ReadKey(terminalCancel.Token, out bool cancelled);

                terminalCancel.Dispose();

                if (cancelled == false)
                    break;
            }

            terminalCancel = null;

            return keyInfo;
        }

        public static ConsoleKeyInfo ReadKey(CancellationToken cancellationToken, out bool cancelled)
        {
            var keyInfo = new ConsoleKeyInfo();

            cancelled = false;

            var cancel = false;

            Task.Run(() =>
            {
                try
                {
                    while (!Console.KeyAvailable)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(50);
                    }
                    keyInfo = Console.ReadKey(true);
                }
                catch
                {
                    cancel = true;
                }
            }).Wait();

            if (cancel)
                cancelled = true;

            return keyInfo;
        }

        /// <summary>
        /// ReadLine until returns a value, even cancelled.
        /// </summary>
        /// <returns></returns>
        public static string ReadLine()
        {
            string value = null;

            while (true)
            {
                terminalCancel = new CancellationTokenSource();

                value = ReadLine(terminalCancel.Token, value, out bool cancelled);

                terminalCancel.Dispose();

                if (cancelled == false)
                    break;
            }

            terminalCancel = null;

            return value;
        }

        public static string ReadLine(CancellationToken cancellationToken)
        {
            return ReadLine(cancellationToken, null, out _);
        }

        private static string ReadLine(CancellationToken cancellationToken, string startingString, out bool cancelled)
        {
            var stringBuilder = new StringBuilder(startingString);

            cancelled = false;

            var cancel = false;

            Task.Run(() =>
            {
                try
                {
                    ConsoleKeyInfo keyInfo;
                    var startingLeft = Console.CursorLeft;
                    var startingTop = Console.CursorTop;
                    var currentIndex = stringBuilder.Length;

                    Console.Write($"{startingString}");

                    do
                    {
                        var previousLeft = Console.CursorLeft;
                        var previousTop = Console.CursorTop;
                        while (!Console.KeyAvailable)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            Thread.Sleep(50);
                        }
                        keyInfo = Console.ReadKey();
                        switch (keyInfo.Key)
                        {
                            case ConsoleKey.D0:
                            case ConsoleKey.D1:
                            case ConsoleKey.D2:
                            case ConsoleKey.D3:
                            case ConsoleKey.D4:
                            case ConsoleKey.D5:
                            case ConsoleKey.D6:
                            case ConsoleKey.D7:
                            case ConsoleKey.D8:
                            case ConsoleKey.D9:
                            case ConsoleKey.A:
                            case ConsoleKey.B:
                            case ConsoleKey.C:
                            case ConsoleKey.D:
                            case ConsoleKey.E:
                            case ConsoleKey.F:
                            case ConsoleKey.G:
                            case ConsoleKey.H:
                            case ConsoleKey.I:
                            case ConsoleKey.J:
                            case ConsoleKey.K:
                            case ConsoleKey.L:
                            case ConsoleKey.M:
                            case ConsoleKey.N:
                            case ConsoleKey.O:
                            case ConsoleKey.P:
                            case ConsoleKey.Q:
                            case ConsoleKey.R:
                            case ConsoleKey.S:
                            case ConsoleKey.T:
                            case ConsoleKey.U:
                            case ConsoleKey.V:
                            case ConsoleKey.W:
                            case ConsoleKey.X:
                            case ConsoleKey.Y:
                            case ConsoleKey.Z:
                            case ConsoleKey.Spacebar:
                            case ConsoleKey.Decimal:
                            case ConsoleKey.Add:
                            case ConsoleKey.Subtract:
                            case ConsoleKey.Multiply:
                            case ConsoleKey.Divide:
                            case ConsoleKey.NumPad0:
                            case ConsoleKey.NumPad1:
                            case ConsoleKey.NumPad2:
                            case ConsoleKey.NumPad3:
                            case ConsoleKey.NumPad4:
                            case ConsoleKey.NumPad5:
                            case ConsoleKey.NumPad6:
                            case ConsoleKey.NumPad7:
                            case ConsoleKey.NumPad8:
                            case ConsoleKey.NumPad9:
                            case ConsoleKey.Oem1:
                            case ConsoleKey.Oem102:
                            case ConsoleKey.Oem2:
                            case ConsoleKey.Oem3:
                            case ConsoleKey.Oem4:
                            case ConsoleKey.Oem5:
                            case ConsoleKey.Oem6:
                            case ConsoleKey.Oem7:
                            case ConsoleKey.Oem8:
                            case ConsoleKey.OemComma:
                            case ConsoleKey.OemMinus:
                            case ConsoleKey.OemPeriod:
                            case ConsoleKey.OemPlus:
                                stringBuilder.Insert(currentIndex, keyInfo.KeyChar);
                                currentIndex++;
                                if (currentIndex < stringBuilder.Length)
                                {
                                    var left = Console.CursorLeft;
                                    var top = Console.CursorTop;
                                    Console.Write(stringBuilder.ToString().Substring(currentIndex));
                                    Console.SetCursorPosition(left, top);
                                }
                                break;
                            case ConsoleKey.Backspace:
                                if (currentIndex > 0)
                                {
                                    currentIndex--;
                                    stringBuilder.Remove(currentIndex, 1);
                                    var left = Console.CursorLeft;
                                    var top = Console.CursorTop;
                                    if (left == previousLeft)
                                    {
                                        left = Console.BufferWidth - 1;
                                        top--;
                                        Console.SetCursorPosition(left, top);
                                    }
                                    Console.Write(stringBuilder.ToString().Substring(currentIndex) + " ");
                                    Console.SetCursorPosition(left, top);
                                }
                                else
                                {
                                    //Console.SetCursorPosition(startingLeft, startingTop);
                                }
                                break;
                            case ConsoleKey.Delete:
                                if (stringBuilder.Length > currentIndex)
                                {
                                    stringBuilder.Remove(currentIndex, 1);
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(stringBuilder.ToString().Substring(currentIndex) + " ");
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                }
                                else
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                break;
                            case ConsoleKey.LeftArrow:
                                if (currentIndex > 0)
                                {
                                    currentIndex--;
                                    var left = Console.CursorLeft - 2;
                                    var top = Console.CursorTop;
                                    if (left < 0)
                                    {
                                        left = Console.BufferWidth + left;
                                        top--;
                                    }
                                    Console.SetCursorPosition(left, top);
                                    if (currentIndex < stringBuilder.Length - 1)
                                    {
                                        Console.Write(stringBuilder[currentIndex].ToString() + stringBuilder[currentIndex + 1]);
                                        Console.SetCursorPosition(left, top);
                                    }
                                }
                                else
                                {
                                    Console.SetCursorPosition(startingLeft, startingTop);
                                    if (stringBuilder.Length > 0)
                                        Console.Write(stringBuilder[0]);
                                    Console.SetCursorPosition(startingLeft, startingTop);
                                }
                                break;
                            case ConsoleKey.RightArrow:
                                if (currentIndex < stringBuilder.Length)
                                {
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(stringBuilder[currentIndex]);
                                    currentIndex++;
                                }
                                else
                                {
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                }
                                break;
                            case ConsoleKey.Home:
                                if (stringBuilder.Length > 0 && currentIndex != stringBuilder.Length)
                                {
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(stringBuilder[currentIndex]);
                                }
                                Console.SetCursorPosition(startingLeft, startingTop);
                                currentIndex = 0;
                                break;
                            case ConsoleKey.End:
                                if (currentIndex < stringBuilder.Length)
                                {
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(stringBuilder[currentIndex]);
                                    var left = previousLeft + stringBuilder.Length - currentIndex;
                                    var top = previousTop;
                                    while (left > Console.BufferWidth)
                                    {
                                        left -= Console.BufferWidth;
                                        top++;
                                    }
                                    currentIndex = stringBuilder.Length;
                                    Console.SetCursorPosition(left, top);
                                }
                                else
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                break;
                            default:
                                Console.SetCursorPosition(previousLeft, previousTop);
                                break;
                        }
                    } while (keyInfo.Key != ConsoleKey.Enter);
                    Console.WriteLine();
                }
                catch
                {
                    cancel = true;
                }
            }).Wait();

            if (cancel)
                cancelled = true;

            return stringBuilder.ToString();
        }

        public static void ClearCurrentLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public static void Up()
        {
            int previousLineCursor = Console.CursorTop - 1;
            if (previousLineCursor < 0) previousLineCursor = 0;
            Console.SetCursorPosition(0, previousLineCursor);
        }

        public static void Write(ulong value)
        {
            Console.Write(value);
        }

        public static void Write(bool value)
        {
            Console.Write(value);
        }

        public static void Write(char value)
        {
            Console.Write(value);
        }

        public static void Write(char[] buffer)
        {
            Console.Write(buffer);
        }

        public static void Write(char[] buffer, int index, int count)
        {
            Console.Write(buffer, index, count);
        }

        public static void Write(double value)
        {
            Console.Write(value);
        }

        public static void Write(long value)
        {
            Console.Write(value);
        }

        public static void Write(object value)
        {
            Console.Write(value);
        }

        public static void Write(float value)
        {
            Console.Write(value);
        }

        public static void Write(uint value)
        {
            Console.Write(value);
        }

        public static void Write(decimal value)
        {
            Console.Write(value);
        }

        public static void Write(int value)
        {
            Console.Write(value);
        }

        public static void WriteLine(ulong value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine()
        {
            Cancel();
            Console.WriteLine();
        }

        public static void WriteLine(bool value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(char[] buffer)
        {
            Cancel();
            Console.WriteLine(buffer);
        }

        public static void WriteLine(decimal value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(double value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(uint value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(int value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(object value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(float value)
        {
            Cancel();
            Console.WriteLine(value);
        }


        public static void WriteLine(long value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(char value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(char[] buffer, int index, int count)
        {
            Cancel();
            Console.WriteLine(buffer, index, count);
        }

        #region String

        public static void WriteLine(string[] values)
        {
            Cancel();
            foreach (string value in values)
                Console.WriteLine(value);
        }

        public static void Write(string value)
        {
            Console.Write(value);
        }

        public static void Write(string format, object arg0)
        {
            Console.Write(format, arg0);
        }

        public static void Write(string format, object arg0, object arg1)
        {
            Console.Write(format, arg0, arg1);
        }

        public static void Write(string format, object arg0, object arg1, object arg2)
        {
            Console.Write(format, arg0, arg1, arg2);
        }

        public static void Write(string format, params object[] arg)
        {
            Console.Write(format, arg);
        }

        public static void WriteLine(string format, params object[] arg)
        {
            Cancel();
            Console.WriteLine(format, arg);
        }


        public static void WriteLine(string value)
        {
            Cancel();
            Console.WriteLine(value);
        }

        public static void WriteLine(string format, object arg0)
        {
            Cancel();
            Console.WriteLine(format, arg0);
        }

        public static void WriteLine(string format, object arg0, object arg1)
        {
            Cancel();
            Console.WriteLine(format, arg0, arg1);
        }

        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Cancel();
            Console.WriteLine(format, arg0, arg1, arg2);
        }


        private static void Output(string str)
        {
            var s = "[red|[sadas]]";

            //Console.BackgroundColor = ConsoleColor.

        }

        #endregion
    }

    public class ConsoleOutputPart
    {
        #region Fields

        public string Text { get; set; }

        public ConsoleColor Foreground { get; set; }

        #endregion

        #region Constructors

        public ConsoleOutputPart()
        {

        }

        #endregion

        #region Operators


        #endregion

        #region Methods

        #endregion

        #region Statics

        #endregion
    }
}
