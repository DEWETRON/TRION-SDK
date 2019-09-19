/**
 * Short example to describe how to control the IDLed on a board
 * that supports this funtionality
  *
 * This example should be used with a TRION-DIO-6400
 * 
 * Describes following:
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

//Escape - Key
static const char KEY_ESC = 27;

//helper-funciton, to obtain ledcolor in string representation
const char* IDLedToString(int ledcol);

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-MULTI",
                                    "TRION-DIO-6400",
                                    "TRION-DI64-HD-S1",
                                    NULL };


int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256]  = {0};
    char sTarget[256]   = {0};
    char sErrorText[256]= {0};
    int nNoOfBoards = 0;
    int nErrorCode  = 0;
    int ledcol = IDLED_COL_OFF;
    char hitkey = 0;

    // Load pxi_api.dll
    if ( 0 != LoadTrionApi() )
    {
        return 1;
    }

    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);
    nNoOfBoards = abs(nNoOfBoards);

    // Check if TRION cards are in the system
    if (nNoOfBoards == 0)
    {
        return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWE2 Explorer.\n");
    }
    
    // Build BoardId -> Either comming from command line (arg 1) or default "0"
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }
    
    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d", nBoardId);
      
    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    // Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardId, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    printf("\n\nPress corresponding key to toggle to specific ID LED state:\n");
    printf("    <esc> Exit application\n\n");
    printf("    [a]   Off\n");
    printf("    [s]   Red\n");
    printf("    [d]   Green\n");
    printf("    [f]   Orange\n");
    printf( "\n\n");

    printf("\rID LED set to color: %s          ", IDLedToString(ledcol));

    while( KEY_ESC != hitkey )
    {
        if (kbhit())
        {
            hitkey = getch();

            switch (hitkey)
            {
            case 'a':
            case 'A':
                ledcol = IDLED_COL_OFF;
                break;
            case 's':
            case 'S':
                ledcol = IDLED_COL_RED;
                break;
            case 'd':
            case 'D':
                ledcol = IDLED_COL_GREEN;
                break;
            case 'f':
            case 'F':
                ledcol = IDLED_COL_ORANGE;
                break;
            default:
                //dont care, just do nothing
                break;
            }

            printf("\rID LED set to color: %s          ", IDLedToString(ledcol));
#if 1
            //this does work, even with IDLED_COL_OFF
            nErrorCode = DeWeSetParam_i32(nBoardId, CMD_IDLED_BOARD_ON, ledcol);
#else
            //alternative way to handle the off state
            if ( IDLED_COL_OFF == ledcol )
            {
                nErrorCode = DeWeSetParam_i32(nBoardId, CMD_IDLED_BOARD_OFF, 0);
            } 
            else 
            {
                nErrorCode = DeWeSetParam_i32(nBoardId, CMD_IDLED_BOARD_ON, ledcol);
            }
#endif
            CheckError(nErrorCode);
        } 
        else
        {
            hitkey = 0;
        }
    }

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}


const char* IDLedToString(int ledcol)
{
    typedef struct tagmap{
        int         key;
        const char* str;
    } STRMAP;

    static const STRMAP colmap[] = {
        IDLED_COL_OFF,      "OFF",
        IDLED_COL_RED,      "RED",
        IDLED_COL_GREEN,    "GREEN",
        IDLED_COL_ORANGE,   "ORANGE",
        -1,                 ""
    };

    static const char* sunmappable = "Unamppable Color";

    int i = 0;

    for ( i = 0; colmap[i].key >= 0; ++i )
    {
        if (ledcol == colmap[i].key)
        {
            return colmap[i].str;
        }
    }

    return sunmappable;
}


