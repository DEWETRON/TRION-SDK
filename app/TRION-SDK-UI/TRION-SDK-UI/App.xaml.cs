using Trion;

namespace TRION_SDK_UI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();


            // Initialize the TRION API backend (choose TRION or TRIONET as needed)
            Trion.API.DeWeConfigure(Trion.API.Backend.TRION);

            // Call DeWeDriverInit on startup
            int nNoOfBoards;
            TrionError nErrorCode = Trion.API.DeWeDriverInit(out nNoOfBoards);

            // Optional: handle error or log result
            if (nErrorCode != TrionError.NONE)
            {
                // Handle error (e.g., log, show message, etc.)
                System.Diagnostics.Debug.WriteLine($"TRION API Init failed: {Trion.API.DeWeErrorConstantToString(nErrorCode)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"TRION API Init succeeded. Number of boards: {nNoOfBoards}");
            }

            MainPage = new AppShell();
        }
    }
}
