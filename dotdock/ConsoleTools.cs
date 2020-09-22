using System;
using System.Collections.Generic;
using System.Linq;

namespace dotdock
{
    internal static class ConsoleTools
    {
        public static T SelectOne<T>(IEnumerable<T> options, Func<T, string> stringer, string prompt = "Select option")
        {
            Console.WriteLine();
            var arr = options.OrderBy(stringer).ToArray();
            if (arr.Length == 1)
            {
                var onlyOne = arr.First();
                Console.WriteLine($"Using {stringer(onlyOne)}");
                return onlyOne;
            }

            for (var i = 0; i < arr.Length; i++) Console.WriteLine($"{i + 1}) {stringer(arr[i])}");

            Console.WriteLine();
            while (true)
            {
                Console.Write(prompt + "> ");
                var res = Console.ReadLine();
                if (int.TryParse(res!.Trim(), out var index) && index >= 1 && index <= arr.Length)
                    return arr[index - 1];

                Console.WriteLine("Invalid input!");
            }
        }

        public static bool AskYesNo(string prompt)
        {
            Console.Write($"{prompt} [Y/n]");
            while (true)
            {
                var res = Console.ReadKey();
                switch (res.Key)
                {
                    case ConsoleKey.Y:
                    case ConsoleKey.Enter:
                        return true;
                    case ConsoleKey.N:
                        return false;
                }
            }
        }
    }
}