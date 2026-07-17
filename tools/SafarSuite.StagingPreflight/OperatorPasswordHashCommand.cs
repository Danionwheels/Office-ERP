namespace SafarSuite.StagingPreflight;

internal static class OperatorPasswordHashCommand
{
    public static int Run()
    {
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("[FAIL] PASSWORD_INPUT: An interactive console is required.");
            return 2;
        }

        var password = ReadSecret("Operator password: ");

        try
        {
            if (password.Length < 12)
            {
                Console.Error.WriteLine("[FAIL] PASSWORD_LENGTH: The operator password must contain at least 12 characters.");
                return 1;
            }

            var confirmation = ReadSecret("Confirm password: ");

            try
            {
                if (!password.AsSpan().SequenceEqual(confirmation))
                {
                    Console.Error.WriteLine("[FAIL] PASSWORD_CONFIRMATION: The password confirmation did not match.");
                    return 1;
                }

                Console.WriteLine(OperatorPasswordHasher.HashPassword(new string(password)));
                return 0;
            }
            finally
            {
                Array.Clear(confirmation);
            }
        }
        finally
        {
            Array.Clear(password);
        }
    }

    private static char[] ReadSecret(string prompt)
    {
        Console.Error.Write(prompt);
        var characters = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.Error.WriteLine();
                var result = characters.ToArray();

                for (var index = 0; index < characters.Count; index++)
                {
                    characters[index] = '\0';
                }

                characters.Clear();
                return result;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(characters.Count - 1);
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
            }
        }
    }
}
