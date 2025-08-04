using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrionApiUtils
{
    public class Utils
    {
        public static void CheckErrorCode(Trion.TrionError error_code, String user_message)
        {
            if ((int)error_code < 0)
            {
                Console.WriteLine($"TRION API Warning: {user_message} {error_code}");
                return;
            }
            if ((int)error_code == 0)
            {
                return;
            }
            if ((int)error_code > 0)
            {
                Console.WriteLine($"TRION API Error: {user_message} {error_code}");
                TrionApi.CloseBoards();
                TrionApi.Uninitialize();
                Environment.Exit((int)error_code);
            }
        }
    }
}
