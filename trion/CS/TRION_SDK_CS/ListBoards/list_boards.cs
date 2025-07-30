using System;
using Trion;

class ListBoards
{
    static void Main(string[] args)
    {
        // Initialize the TRION API and get the number of boards
        int nNoOfBoards = TrionApi.Initialize();
        if (nNoOfBoards == 0)
        {
            Console.WriteLine("No TRION board found");
            return;
        }
        if (nNoOfBoards < 0)
        {
            Console.WriteLine("Trion API is set to use simulated boards");
        }
        else
        {
            Console.WriteLine("Trion API is set to use real boards");
        }
        nNoOfBoards = Math.Abs(nNoOfBoards);

        // Print header
        Console.WriteLine("{0,-7} {1,-7} {2,-8} {3,-30} {4}", "SlotNo", "BoardID", "EncID", "Name", "SerialNo");
        Console.WriteLine("--------------------------------------------------------------------------");

        // Iterate all boards
        for (int nBoardID = 0; nBoardID < nNoOfBoards; ++nBoardID)
        {
            string boardTarget = $"BoardID{nBoardID}";
            string sysInfoTarget = $"{boardTarget}/boardproperties/SystemInfo";
            string boardInfoTarget = $"{boardTarget}/boardproperties/BoardInfo";
            string encInfoTarget = $"{boardTarget}/boardproperties/SystemInfo/EnclosureInfo";

            // Get SlotNo
            var (errSlot, slotNoStr) = TrionApi.DeWeGetParamXML_String(sysInfoTarget, "SlotNo");
            int slotNo = 0;
            int.TryParse(slotNoStr, out slotNo);

            // Get EncID
            var (errEnc, encIdStr) = TrionApi.DeWeGetParamXML_String(encInfoTarget, "EnclosureID");
            int encId = 0;
            int.TryParse(encIdStr, out encId);

            // Get Board Name
            var (errName, boardName) = TrionApi.DeWeGetParamStruct_String(boardTarget, "BoardName");

            // Get Serial Number
            var (errSerial, serialNo) = TrionApi.DeWeGetParamXML_String(boardInfoTarget, "SerialNumber");

            // Print Properties
            Console.WriteLine("{0,-7} {1,-7} {2,-8} {3,-30} {4}",
                slotNo, nBoardID, encId, boardName, serialNo);
        }

        // Uninitialize the API (implement if needed)
        TrionApi.Uninitialize();
    }
}