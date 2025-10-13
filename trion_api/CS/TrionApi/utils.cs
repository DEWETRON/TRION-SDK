namespace TrionApiUtils
{
    public class Utils
    {
        public static void CheckErrorCode(Trion.TrionError error_code, String user_message)
        {
            if ((int)error_code < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                System.Diagnostics.Debug.WriteLine($"TRION API Warning: {user_message} {error_code}");
                Console.ResetColor();
                return;
            }
            //if ((int)error_code == 0)
            //{
             //   Console.ForegroundColor = ConsoleColor.Green;
              //  System.Diagnostics.Debug.WriteLine($"TRION API Success: {user_message} {error_code}");
              //  Console.ResetColor();
               // return;
            //}
            if ((int)error_code > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Diagnostics.Debug.WriteLine($"TRION API Error: {user_message} {error_code}");
                Environment.Exit((int)error_code);
                Console.ResetColor();
                TrionApi.CloseBoards();
                TrionApi.Uninitialize();
                Environment.Exit((int)error_code);
            }
        }
    }
}
